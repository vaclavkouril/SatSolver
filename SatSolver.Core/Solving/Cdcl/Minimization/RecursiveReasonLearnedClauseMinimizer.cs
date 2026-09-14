using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

public sealed class RecursiveReasonLearnedClauseMinimizer : ILearnedClauseMinimizer
{
    public LearnedClause Minimize(
        LearnedClause learnedClause,
        SolverState state,
        ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(learnedClause);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(clauses);

        var source = learnedClause.Literals;
        var vars = source.Select(lit => lit.Variable).ToHashSet();
        var result = new List<Literal> { source[0] };

        foreach (var lit in source.Skip(1))
        {
            if (!IsRedundant(lit.Variable, vars, state, clauses, []))
                result.Add(lit);
        }

        return new LearnedClause(result, CalculateLbd(result, state));
    }

    private static bool IsRedundant(
        int varId,
        IReadOnlySet<int> vars,
        SolverState state,
        ClauseDatabase clauses,
        HashSet<int> visiting)
    {
        var reason = state.GetReason(varId);
        if (!reason.HasValue)
            return false;

        // guard malformed reason cycles
        if (!visiting.Add(varId))
            return false;

        try
        {
            return AreReasonLiteralsRedundant(
                reason.Value,
                varId,
                vars,
                state,
                clauses,
                visiting);
        }
        finally
        {
            visiting.Remove(varId);
        }
    }

    private static bool AreReasonLiteralsRedundant(
        ClauseReference reason,
        int resolvedVar,
        IReadOnlySet<int> vars,
        SolverState state,
        ClauseDatabase clauses,
        HashSet<int> visiting)
    {
        foreach (var lit in clauses.Get(reason).Literals)
        {
            if (IsAlreadyCovered(lit, resolvedVar, vars, state))
                continue;

            if (!IsRedundant(
                    lit.Variable,
                    vars,
                    state,
                    clauses,
                    visiting))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlreadyCovered(
        Literal literal,
        int resolvedVar,
        IReadOnlySet<int> vars,
        SolverState state)
    {
        return literal.Variable == resolvedVar ||
            state.GetDecisionLevel(literal.Variable) == 0 ||
            vars.Contains(literal.Variable);
    }

    private static int CalculateLbd(IReadOnlyList<Literal> lits, SolverState state)
    {
        var levels = new HashSet<int>();

        foreach (var lit in lits)
            levels.Add(state.GetDecisionLevel(lit.Variable));

        return levels.Count;
    }
}
