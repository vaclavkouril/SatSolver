using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

public sealed class DisabledClauseDeletionPolicy : IClauseDeletionPolicy
{
    public IReadOnlyList<ClauseReference> SelectForDeletion(
        IEnumerable<SolverClause> clauses,
        IReadOnlySet<ClauseReference> lockedClauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        ArgumentNullException.ThrowIfNull(lockedClauses);
        return [];
    }
}
