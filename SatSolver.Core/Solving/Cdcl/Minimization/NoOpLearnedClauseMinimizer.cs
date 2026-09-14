using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

/// <summary>Preserves the clause produced by conflict analysis.</summary>
internal sealed class NoOpLearnedClauseMinimizer : ILearnedClauseMinimizer
{
    public LearnedClause Minimize(
        LearnedClause learnedClause,
        SolverState state,
        ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(learnedClause);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(clauses);
        return learnedClause;
    }
}
