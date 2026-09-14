using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

/// <summary>Removes literals by self-subsuming resolution.</summary>
internal sealed class SelfSubsumingResolutionMinimizer : ILearnedClauseMinimizer
{
    public LearnedClause Minimize(
        LearnedClause learnedClause,
        SolverState state,
        ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(learnedClause);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(clauses);

        var literals = learnedClause.Literals.ToList();

        for (var index = 1; index < literals.Count;)
        {
            if (CanResolveAway(literals[index], literals, clauses))
            {
                // Preserve asserting literal
                literals.RemoveAt(index);
                continue;
            }

            index++;
        }

        return new LearnedClause(literals, CalculateLbd(literals, state));
    }

    private static bool CanResolveAway(
        Literal literal,
        IReadOnlyList<Literal> learnedLiterals,
        ClauseDatabase clauses)
    {
        var learnedSet = learnedLiterals.ToHashSet();
        var opposite = literal.Negate();

        foreach (var clause in clauses.ActiveClauses)
        {
            if (!clause.Literals.Contains(opposite))
                continue;

            // Resolvent adds no literals
            if (clause.Literals.All(candidate =>
                    candidate == opposite || learnedSet.Contains(candidate)))
            {
                return true;
            }
        }

        return false;
    }

    private static int CalculateLbd(IEnumerable<Literal> literals, SolverState state) =>
        literals
            .Select(literal => state.GetDecisionLevel(literal.Variable))
            .Distinct()
            .Count();
}
