using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

/// <summary>Removes literals explained by recursive antecedents.</summary>
internal sealed class RecursiveReasonLearnedClauseMinimizer : ILearnedClauseMinimizer
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
        var clauseVariables = source.Select(literal => literal.Variable).ToHashSet();
        // Index zero: asserting literal
        var minimized = new List<Literal> { source[0] };

        foreach (var literal in source.Skip(1))
        {
            if (!IsRedundant(literal.Variable, clauseVariables, state, clauses, []))
                minimized.Add(literal);
        }

        return new LearnedClause(minimized, CalculateLbd(minimized, state));
    }

    private static bool IsRedundant(
        int variable,
        IReadOnlySet<int> clauseVariables,
        SolverState state,
        ClauseDatabase clauses,
        HashSet<int> resolvingVariables)
    {
        var reason = state.GetReason(variable);
        if (!reason.HasValue)
            return false;

        // Defensive cycle guard
        if (!resolvingVariables.Add(variable))
            return false;

        try
        {
            foreach (var antecedent in clauses.Get(reason.Value).Literals)
            {
                // Root and clause vars already covered
                if (antecedent.Variable == variable ||
                    state.GetDecisionLevel(antecedent.Variable) == 0 ||
                    clauseVariables.Contains(antecedent.Variable))
                {
                    continue;
                }

                if (!IsRedundant(
                        antecedent.Variable,
                        clauseVariables,
                        state,
                        clauses,
                        resolvingVariables))
                {
                    return false;
                }
            }

            return true;
        }
        finally
        {
            resolvingVariables.Remove(variable);
        }
    }

    private static int CalculateLbd(IEnumerable<Literal> literals, SolverState state) =>
        literals
            .Select(literal => state.GetDecisionLevel(literal.Variable))
            .Distinct()
            .Count();
}
