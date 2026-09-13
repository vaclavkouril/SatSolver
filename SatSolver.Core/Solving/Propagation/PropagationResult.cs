using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

/// <summary>Propagation outcome.</summary>
internal readonly record struct PropagationResult(ClauseReference? ConflictClause)
{
    public static PropagationResult NoConflict => new(null);

    public bool HasConflict => ConflictClause.HasValue;

    public static PropagationResult Conflict(ClauseReference clause) => new(clause);
}
