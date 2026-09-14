using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

/// <summary>Trail-based resolution analysis.</summary>
internal sealed class ResolutionConflictAnalyzer(ConflictAnalysisMethod method) : IConflictAnalyzer
{
    private bool[] _seen = [];
    private readonly List<int> _touchedVariables = [];

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
        // Slot zero reserved for the asserting literal
        var learnedLiterals = new List<Literal> { default };
        var additionalClauses = new List<LearnedClause>();
        LearnedClause? assertingClause = null;
        var currentLevelCount = 0;
        var clauseReference = conflict;
        var trailIndex = state.Trail.Count - 1;
        var hasPivot = false;
        Literal pivot = default;

        while (true)
        {
            AddClauseLiterals(
                clauses.Get(clauseReference),
                hasPivot ? pivot.Variable : null,
                state,
                learnedLiterals,
                ref currentLevelCount);

            // Resolve the latest current-level assignment
            pivot = FindLatestCurrentLevelLiteral(state, ref trailIndex);
            _seen[pivot.Variable] = false;
            currentLevelCount--;

            var antecedent = state.GetReason(pivot.Variable);
            var reachedCut = ReachedCut(currentLevelCount, antecedent);

            if (reachedCut)
            {
                var learnedClause = CreateLearnedClause(learnedLiterals, pivot, state);

                // First cut = backjump clause
                if (assertingClause is null)
                    assertingClause = learnedClause;
                else
                    additionalClauses.Add(learnedClause);

                if (method != ConflictAnalysisMethod.MultipleCuts ||
                    !CanResolveFurther(antecedent, pivot.Variable, state, clauses))
                    return new ConflictAnalysisResult(
                        assertingClause,
                        additionalClauses);
            }

            if (!antecedent.HasValue)
                throw new InvalidOperationException("The conflict cut did not reach a decision literal.");

            clauseReference = antecedent.Value;
            hasPivot = true;
        }
    }

    private void AddClauseLiterals(
        SolverClause clause,
        int? pivotVariable,
        SolverState state,
        List<Literal> learnedLiterals,
        ref int currentLevelCount)
    {
        foreach (var literal in clause.Literals)
        {
            if (literal.Variable == pivotVariable || _seen[literal.Variable])
                continue;

            var level = state.GetDecisionLevel(literal.Variable);
            if (level == 0)
                continue;

            _seen[literal.Variable] = true;
            _touchedVariables.Add(literal.Variable);

            if (level == state.CurrentDecisionLevel)
                currentLevelCount++;
            else
                learnedLiterals.Add(literal);
        }
    }

    private Literal FindLatestCurrentLevelLiteral(SolverState state, ref int trailIndex)
    {
        while (trailIndex >= 0)
        {
            var literal = state.Trail[trailIndex--];
            if (_seen[literal.Variable])
                return literal;
        }

        throw new InvalidOperationException("The conflict has no current-level literal.");
    }

    private bool ReachedCut(int currentLevelCount, ClauseReference? antecedent) => method switch
    {
        ConflictAnalysisMethod.FirstUip => currentLevelCount == 0,
        ConflictAnalysisMethod.DecisionLiteral => !antecedent.HasValue,
        ConflictAnalysisMethod.MultipleCuts => currentLevelCount == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static bool CanResolveFurther(
        ClauseReference? antecedent,
        int pivotVariable,
        SolverState state,
        ClauseDatabase clauses)
    {
        // Another current-level pivot required
        return antecedent.HasValue &&
            clauses.Get(antecedent.Value).Literals.Any(
                literal => literal.Variable != pivotVariable &&
                    state.GetDecisionLevel(literal.Variable) == state.CurrentDecisionLevel);
    }

    private static LearnedClause CreateLearnedClause(
        List<Literal> learnedLiterals,
        Literal pivot,
        SolverState state)
    {
        learnedLiterals[0] = pivot.Negate();

        var literals = learnedLiterals.ToArray();
        var lbd = literals
            .Select(literal => state.GetDecisionLevel(literal.Variable))
            .Distinct()
            .Count();

        return new LearnedClause(literals, lbd);
    }

    private void EnsureCapacity(int variableCount)
    {
        if (_seen.Length <= variableCount)
            _seen = new bool[variableCount + 1];
    }

    private void ClearSeenVariables()
    {
        foreach (var variable in _touchedVariables)
            _seen[variable] = false;

        _touchedVariables.Clear();
    }
}
