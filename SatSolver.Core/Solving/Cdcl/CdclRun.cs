using System.Diagnostics;
using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl.Analysis;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Cdcl.Deletion;
using SatSolver.Core.Solving.Cdcl.Minimization;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl;

/// <summary>One CDCL search run.</summary>
internal sealed class CdclRun(
    IDecisionHeuristic decisionHeuristic,
    CdclSolverOptions options)
{
    private readonly IDecisionHeuristic _decisionHeuristic = decisionHeuristic;
    private readonly CdclSolverOptions _options = options;

    public SolverResult Solve(CnfFormula formula)
    {
        _decisionHeuristic.Initialize(formula);

        var clauses = new ClauseDatabase(formula);
        var propagator = PropagationEngineFactory.Create(_options.Propagation);
        var analyzer = ConflictAnalyzerFactory.Create(_options.ConflictAnalysis);
        var minimizer = LearnedClauseMinimizerFactory.Create(_options.Minimization);
        var restartPolicy = RestartPolicyFactory.Create(_options.Restart);
        var deletionPolicy = ClauseDeletionPolicyFactory.Create(_options.ClauseDeletion);
        var deletionSchedule = new LearnedClauseDeletionSchedule(_options.ClauseDeletion);
        var state = new SolverState(formula);
        var statistics = new SearchStatistics();

        propagator.Initialize(clauses);

        using var process = Process.GetCurrentProcess();
        var cpuTimeBefore = process.TotalProcessorTime;
        var isSatisfiable = Search(
            state,
            clauses,
            propagator,
            analyzer,
            minimizer,
            restartPolicy,
            deletionPolicy,
            deletionSchedule,
            statistics);
        var cpuTime = process.TotalProcessorTime - cpuTimeBefore;

        return isSatisfiable
            ? new SolverResult(SolverStatus.SAT, state.CreateModel(), statistics.Create(cpuTime))
            : new SolverResult(SolverStatus.UNSAT, [], statistics.Create(cpuTime));
    }

    private bool Search(
        SolverState state,
        ClauseDatabase clauses,
        IPropagationEngine propagator,
        IConflictAnalyzer analyzer,
        ILearnedClauseMinimizer minimizer,
        IRestartPolicy restartPolicy,
        IClauseDeletionPolicy deletionPolicy,
        LearnedClauseDeletionSchedule deletionSchedule,
        SearchStatistics statistics)
    {
        var conflictsSinceRestart = 0;

        while (true)
        {
            var propagation = propagator.Propagate(state, statistics);

            if (propagation.HasConflict)
            {
                statistics.RecordConflict();
                conflictsSinceRestart++;

                if (state.CurrentDecisionLevel == 0)
                    return false;

                var conflict = propagation.ConflictClause!.Value;
                var conflictClause = clauses.Get(conflict);
                if (conflictClause.IsLearned)
                    conflictClause.BumpActivity();

                var analysis = analyzer.Analyze(conflict, state, clauses);
                var assertingClause = minimizer.Minimize(analysis.AssertingClause, state, clauses);
                var assertionLevel = GetAssertionLevel(assertingClause, state);
                var assertingReference = AddLearnedClause(assertingClause, clauses, propagator, statistics);

                foreach (var clause in analysis.AdditionalClauses)
                    AddLearnedClause(minimizer.Minimize(clause, state, clauses), clauses, propagator, statistics);

                state.BacktrackTo(assertionLevel);
                statistics.RecordBackjump();

                // Asserting clause; unit after backjump
                if (!state.Enqueue(assertingClause.Literals[0], assertingReference))
                    throw new InvalidOperationException("The asserting learned clause is not unit.");

                clauses.Get(assertingReference).BumpActivity();
                statistics.RecordUnitPropagations(1);

                if (restartPolicy.ShouldRestart(conflictsSinceRestart))
                {
                    state.BacktrackTo(0);
                    restartPolicy.OnRestart();
                    conflictsSinceRestart = 0;
                    statistics.RecordRestart();
                }

                DeleteLearnedClauses(clauses, state, deletionPolicy, deletionSchedule, statistics);
                continue;
            }

            var decision = _decisionHeuristic.ChooseLiteral(state);
            if (!decision.HasValue)
                return true;

            state.BeginDecisionLevel();
            state.Enqueue(decision.Value, reason: null);
            statistics.RecordDecision();
        }
    }

    private static ClauseReference AddLearnedClause(
        LearnedClause clause,
        ClauseDatabase clauses,
        IPropagationEngine propagator,
        SearchStatistics statistics)
    {
        var reference = clauses.AddLearned(clause);
        propagator.RegisterClause(reference);
        statistics.RecordLearnedClause(clause.Literals.Count, clause.Lbd);
        return reference;
    }

    private static void DeleteLearnedClauses(
        ClauseDatabase clauses,
        SolverState state,
        IClauseDeletionPolicy policy,
        LearnedClauseDeletionSchedule schedule,
        SearchStatistics statistics)
    {
        var learnedClauseCount = clauses.LearnedClauses.Count();
        if (!schedule.ShouldDelete(learnedClauseCount))
            return;

        var deleted = policy.SelectForDeletion(clauses.LearnedClauses, state.GetLockedClauses());

        foreach (var clause in deleted)
        {
            clauses.DeleteLearned(clause);
            statistics.RecordDeletedLearnedClause();
        }

        schedule.OnDeletionRound();
    }

    private static int GetAssertionLevel(LearnedClause clause, SolverState state) =>
        clause.Literals
            .Skip(1)
            .Select(literal => state.GetDecisionLevel(literal.Variable))
            .DefaultIfEmpty(0)
            .Max();

}
