namespace SatSolver.Core.Solving.Propagation;

/// <summary>Selects a propagation structure.</summary>
public enum PropagationMethod
{
    /// <summary>Uses literal occurrence lists.</summary>
    AdjacencyLists,

    /// <summary>Uses lazy watched literals.</summary>
    WatchedLiterals
}
