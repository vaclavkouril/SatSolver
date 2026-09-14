namespace SatSolver.Core.Solving.Cdcl.Configuration;

/// <summary>Selects optional simplification of learned clauses.</summary>
public enum ClauseMinimizationMethod
{
    /// <summary>Preserves every literal produced by conflict analysis.</summary>
    None,

    /// <summary>Removes literals implied recursively by the remaining literals.</summary>
    RecursiveReasons,

    /// <summary>Uses self-subsuming resolution where applicable.</summary>
    SelfSubsumingResolution
}
