using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

/// <summary>Selects deletable learned clauses without modifying the database.</summary>
internal interface IClauseDeletionPolicy
{
    IReadOnlyList<ClauseReference> SelectForDeletion(
        IEnumerable<SolverClause> clauses,
        IReadOnlySet<ClauseReference> lockedClauses);
}
