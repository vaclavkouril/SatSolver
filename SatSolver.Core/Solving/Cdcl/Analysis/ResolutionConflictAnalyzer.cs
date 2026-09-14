using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

public abstract class ResolutionConflictAnalyzer : IConflictAnalyzer
{
    private bool[] _seen = [];
    private readonly List<int> _touchedVars = [];

    public ConflictAnalysisResult Analyze(
        ClauseReference conflict,
        SolverState state,
        ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(clauses);

        if (state.CurrentDecisionLevel == 0)
            throw new InvalidOperationException("Root-level conflicts have no asserting clause.");

        EnsureCapacity(state.VariableCount);

        try
        {
            return AnalyzeConflict(conflict, state, clauses);
        }
        finally
        {
            ClearSeenVariables();
        }
    }

    private ConflictAnalysisResult AnalyzeConflict(
        ClauseReference conflict,
        SolverState state,
        ClauseDatabase clauses)
    {
        // slot 0 = asserting literal
        var lits = new List<Literal> { default };
        var learned = new List<LearnedClause>();
        var clauseRef = conflict;
        int? resolvedVar = null;
        var trailIdx = state.Trail.Count - 1;
        var openCurrentLiterals = 0;

        // walk the trail backwards
        while (true)
        {
            openCurrentLiterals += AddUnseenClauseLiterals(
                clauses.Get(clauseRef),
                resolvedVar,
                state,
                lits);

            var pivot = FindLatestCurrentLevelLiteral(state, ref trailIdx);
            _seen[pivot.Variable] = false;
            openCurrentLiterals--;

            var antecedent = state.GetReason(pivot.Variable);
            if (IsCutReached(openCurrentLiterals, antecedent))
            {
                learned.Add(CreateLearnedClause(lits, pivot, state));

                if (ShouldFinishAnalysis(antecedent, pivot.Variable, state, clauses))
                    return CreateAnalysisResult(learned);
            }

            if (!antecedent.HasValue)
                throw new InvalidOperationException("The conflict cut did not reach a decision literal.");

            clauseRef = antecedent.Value;
            resolvedVar = pivot.Variable;
        }
    }

    private int AddUnseenClauseLiterals(
        SolverClause clause,
        int? resolvedVar,
        SolverState state,
        List<Literal> learnedLiterals)
    {
        var currentCount = 0;

        foreach (var lit in clause.Literals)
        {
            if (lit.Variable == resolvedVar || _seen[lit.Variable])
                continue;

            var level = state.GetDecisionLevel(lit.Variable);
            if (level == 0)
                continue;

            _seen[lit.Variable] = true;
            _touchedVars.Add(lit.Variable);

            if (level == state.CurrentDecisionLevel)
                currentCount++;
            else
                learnedLiterals.Add(lit);
        }

        return currentCount;
    }

    private Literal FindLatestCurrentLevelLiteral(SolverState state, ref int trailIdx)
    {
        while (trailIdx >= 0)
        {
            var lit = state.Trail[trailIdx--];
            if (_seen[lit.Variable])
                return lit;
        }

        throw new InvalidOperationException("The conflict has no current-level literal.");
    }

    protected abstract bool IsCutReached(
        int openCurrentLiterals,
        ClauseReference? antecedent);

    protected virtual bool ShouldFinishAnalysis(
        ClauseReference? antecedent,
        int pivotVar,
        SolverState state,
        ClauseDatabase clauses) => true;

    protected static bool HasAnotherCurrentLevelPivot(
        ClauseReference? antecedent,
        int pivotVar,
        SolverState state,
        ClauseDatabase clauses)
    {
        if (!antecedent.HasValue)
            return false;

        // another current-level pivot means another cut
        foreach (var lit in clauses.Get(antecedent.Value).Literals)
        {
            if (lit.Variable != pivotVar &&
                state.GetDecisionLevel(lit.Variable) == state.CurrentDecisionLevel)
            {
                return true;
            }
        }

        return false;
    }

    private static ConflictAnalysisResult CreateAnalysisResult(List<LearnedClause> learned)
    {
        return new ConflictAnalysisResult(
            learned[0],
            learned.Skip(1).ToArray());
    }

    private static LearnedClause CreateLearnedClause(
        List<Literal> lits,
        Literal pivot,
        SolverState state)
    {
        // negated pivot asserts after backtracking
        lits[0] = pivot.Negate();

        var literals = lits.ToArray();
        return new LearnedClause(literals, CountDecisionLevels(literals, state));
    }

    private static int CountDecisionLevels(
        IReadOnlyList<Literal> literals,
        SolverState state)
    {
        var levels = new HashSet<int>();

        foreach (var lit in literals)
            levels.Add(state.GetDecisionLevel(lit.Variable));

        return levels.Count;
    }

    private void EnsureCapacity(int varCount)
    {
        if (_seen.Length <= varCount)
            _seen = new bool[varCount + 1];
    }

    private void ClearSeenVariables()
    {
        foreach (var varId in _touchedVars)
            _seen[varId] = false;

        _touchedVars.Clear();
    }
}
