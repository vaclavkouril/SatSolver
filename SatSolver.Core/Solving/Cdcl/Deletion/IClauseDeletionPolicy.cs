using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

public interface IClauseDeletionPolicy
{
    IReadOnlyList<ClauseReference> SelectForDeletion(
        IEnumerable<SolverClause> clauses,
        IReadOnlySet<ClauseReference> lockedClauses);
}
