using System.Diagnostics;
using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl.Analysis;
using SatSolver.Core.Solving.Cdcl.Minimization;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl;

internal sealed class CdclRun
{
    private readonly IDecisionHeuristic _heuristic;
    private readonly IConflictAwareDecisionHeuristic? _conflictHeuristic;
    private readonly ClauseDatabase _clauses;
    private readonly SolverState _state;
    private readonly SearchStatistics _statistics = new();
    private readonly IPropagationEngine _propagator;
    private readonly IConflictAnalyzer _analyzer;
    private readonly ILearnedClauseMinimizer _minimizer;
    private readonly IRestartPolicy _restart;
    private readonly ClauseDeletionMethod _clauseDeletion;
    private readonly double _deletionGrowth;
    private readonly int _keepLbd;
    private readonly double _deletionFraction;
    private int _deletionLimit;
    private int _conflicts;

    public CdclRun(
        CnfFormula formula,
        IDecisionHeuristic heuristic,
        IPropagationEngine propagator,
        IConflictAnalyzer analyzer,
        ILearnedClauseMinimizer minimizer,
        IRestartPolicy restart,
        ClauseDeletionMethod clauseDeletion,
        int deletionLimit,
        double deletionGrowth,
        int keepLbd,
        double deletionFraction)
    {
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentNullException.ThrowIfNull(heuristic);
        ArgumentNullException.ThrowIfNull(propagator);
        ArgumentNullException.ThrowIfNull(analyzer);
        ArgumentNullException.ThrowIfNull(minimizer);
        ArgumentNullException.ThrowIfNull(restart);

        _heuristic = heuristic;
        _conflictHeuristic = heuristic as IConflictAwareDecisionHeuristic;
        _clauses = new ClauseDatabase(formula);
        _state = new SolverState(formula);
        _propagator = propagator;
        _analyzer = analyzer;
        _minimizer = minimizer;
        _restart = restart;
        _clauseDeletion = clauseDeletion;
        _deletionLimit = deletionLimit;
        _deletionGrowth = deletionGrowth;
        _keepLbd = keepLbd;
        _deletionFraction = deletionFraction;
    }

    public SolverResult Solve()
    {
        _heuristic.Initialize(_state.Formula);
        _propagator.Initialize(_clauses);
        _restart.Reset();

        using var process = Process.GetCurrentProcess();
        var start = process.TotalProcessorTime;
        var isSat = Search();
        var cpuTime = process.TotalProcessorTime - start;

        return isSat
            ? new SolverResult(SolverStatus.SAT, _state.CreateModel(), _statistics.Create(cpuTime))
            : new SolverResult(SolverStatus.UNSAT, [], _statistics.Create(cpuTime));
    }

    private bool Search()
    {
        while (true)
        {
            var propagation = _propagator.Propagate(_state, _statistics);
            if (propagation.HasConflict)
            {
                if (!HandleConflict(propagation.ConflictClause!.Value))
                    return false;

                continue;
            }

            if (!MakeDecision())
                return true;
        }
    }

    private bool HandleConflict(ClauseReference conflict)
    {
        _statistics.RecordConflict();
        _conflicts++;

        if (_state.CurrentDecisionLevel == 0)
            return false;

        BumpConflictClauseActivity(conflict);
        LearnAndBackjump(conflict);
        _conflictHeuristic?.OnConflict();
        RestartIfNeeded();
        DeleteLearnedClauses();
        return true;
    }

    private void BumpConflictClauseActivity(ClauseReference conflict)
    {
        var clause = _clauses.Get(conflict);
        if (clause.IsLearned)
            clause.BumpActivity();
    }

    private void LearnAndBackjump(ClauseReference conflict)
    {
        var analysis = _analyzer.Analyze(conflict, _state, _clauses);
        var asserting = _minimizer.Minimize(
            analysis.AssertingClause,
            _state,
            _clauses);
        var level = GetAssertionLevel(asserting);
        var clauseRef = AddLearnedClause(asserting);

        AddAdditionalClauses(analysis.AdditionalClauses);

        _state.BacktrackTo(level);
        _statistics.RecordBackjump();
        EnqueueAssertingLiteral(asserting, clauseRef);
    }

    private void AddAdditionalClauses(IReadOnlyList<LearnedClause> moreClauses)
    {
        foreach (var clause in moreClauses)
        {
            var minimizedClause = _minimizer.Minimize(clause, _state, _clauses);
            AddLearnedClause(minimizedClause);
        }
    }

    private void EnqueueAssertingLiteral(LearnedClause clause, ClauseReference clauseRef)
    {
        if (!_state.Enqueue(clause.Literals[0], clauseRef))
            throw new InvalidOperationException("The asserting learned clause is not unit.");

        _clauses.Get(clauseRef).BumpActivity();
        _statistics.RecordUnitPropagations(1);
    }

    private void RestartIfNeeded()
    {
        if (!_restart.ShouldRestart(_conflicts))
            return;

        _state.BacktrackTo(0);
        _restart.OnRestart();
        _conflicts = 0;
        _statistics.RecordRestart();
    }

    private bool MakeDecision()
    {
        var decision = _heuristic.ChooseLiteral(_state);
        if (!decision.HasValue)
            return false;

        _state.BeginDecisionLevel();
        _state.Enqueue(decision.Value, reason: null);
        _statistics.RecordDecision();
        return true;
    }

    private ClauseReference AddLearnedClause(LearnedClause clause)
    {
        var clauseRef = _clauses.AddLearned(clause);
        _propagator.RegisterClause(clauseRef);
        _conflictHeuristic?.OnLearnedClause(clause.Literals);
        _statistics.RecordLearnedClause(clause.Literals.Count, clause.Lbd);
        return clauseRef;
    }

    private void DeleteLearnedClauses()
    {
        if (_clauseDeletion == ClauseDeletionMethod.Disabled ||
            _clauses.ActiveLearnedClauseCount <= _deletionLimit)
            return;

        var toDelete = SelectForDeletion(
            _clauses.LearnedClauses,
            _state.GetLockedClauses(),
            _clauseDeletion,
            _keepLbd,
            _deletionFraction);

        foreach (var clauseRef in toDelete)
        {
            _clauses.DeleteLearned(clauseRef);
            _statistics.RecordDeletedLearnedClause();
        }

        _deletionLimit = (int)Math.Min(int.MaxValue, Math.Ceiling(_deletionLimit * _deletionGrowth));
    }

    internal static IReadOnlyList<ClauseReference> SelectForDeletion(
        IEnumerable<SolverClause> clauses,
        IReadOnlySet<ClauseReference> lockedClauses,
        ClauseDeletionMethod method,
        int keepLbd,
        double deletionFraction)
    {
        if (method == ClauseDeletionMethod.Disabled)
            return [];

        // Keep binaries, low-LBD clauses and reason clauses.
        var eligible = clauses.Where(clause =>
            clause.IsLearned && !clause.IsDeleted &&
            clause.Literals.Count > 2 && clause.Lbd > keepLbd &&
            !lockedClauses.Contains(clause.Reference));

        Func<SolverClause, (double Primary, double Secondary)> priorityOf = method switch
        {
            ClauseDeletionMethod.Activity => clause => (clause.Activity, -clause.Lbd),
            ClauseDeletionMethod.Lbd => clause => (-clause.Lbd, 0),
            ClauseDeletionMethod.LbdActivity => clause => (-clause.Lbd, clause.Activity),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };

        // Bulk heapification costs O(m); extract only the k requested candidates.
        // The input index preserves the stable ordering of equal priorities.
        var candidates = new PriorityQueue<ClauseReference, (double, double, int)>(
            eligible.Select((clause, index) =>
            {
                var priority = priorityOf(clause);
                return (clause.Reference, (priority.Primary, priority.Secondary, index));
            }));
        var selected = new ClauseReference[(int)Math.Ceiling(candidates.Count * deletionFraction)];
        for (var index = 0; index < selected.Length; index++)
            selected[index] = candidates.Dequeue();
        return selected;
    }

    private int GetAssertionLevel(LearnedClause clause)
    {
        var level = 0;

        for (var idx = 1; idx < clause.Literals.Count; idx++)
            level = Math.Max(level, _state.GetDecisionLevel(clause.Literals[idx].Variable));

        return level;
    }
}
