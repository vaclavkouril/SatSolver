using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

public sealed class SelfSubsumingResolutionMinimizer : ILearnedClauseMinimizer
{
    public LearnedClause Minimize(
        LearnedClause learnedClause,
        SolverState state,
        ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(learnedClause);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(clauses);

        var lits = learnedClause.Literals.ToList();
        var literalSet = lits.ToHashSet();

        // asserting literal stays first
        for (var idx = 1; idx < lits.Count;)
        {
            if (CanResolveAway(lits[idx], literalSet, clauses))
            {
                literalSet.Remove(lits[idx]);
                lits.RemoveAt(idx);
                continue;
            }

            idx++;
        }

        return new LearnedClause(lits, CalculateLbd(lits, state));
    }

    private static bool CanResolveAway(
        Literal literal,
        IReadOnlySet<Literal> learnedLiterals,
        ClauseDatabase clauses)
    {
        var opposite = literal.Negate();

        foreach (var clause in clauses.GetActiveClausesContaining(opposite))
        {
            if (FitsInsideLearnedClause(clause.Literals, opposite, learnedLiterals))
                return true;
        }

        return false;
    }

    private static bool FitsInsideLearnedClause(
        IReadOnlyList<Literal> reason,
        Literal resolvedLiteral,
        IReadOnlySet<Literal> learnedLiterals)
    {
        // no new literals in the resolvent
        foreach (var lit in reason)
        {
            if (lit != resolvedLiteral && !learnedLiterals.Contains(lit))
                return false;
        }

        return true;
    }

    private static int CalculateLbd(IReadOnlyList<Literal> lits, SolverState state)
    {
        var levels = new HashSet<int>();

        foreach (var lit in lits)
            levels.Add(state.GetDecisionLevel(lit.Variable));

        return levels.Count;
    }
}
