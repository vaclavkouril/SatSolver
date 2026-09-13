namespace SatSolver.Core.Solving.Search;

/// <summary>Stable clause-store reference.</summary>
/// <remarks>Remains valid after logical deletion so it can remain a trail reason.</remarks>
internal readonly record struct ClauseReference(int Value);
