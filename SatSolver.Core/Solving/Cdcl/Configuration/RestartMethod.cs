namespace SatSolver.Core.Solving.Cdcl.Configuration;

/// <summary>Selects the conflict schedule used for restarts.</summary>
public enum RestartMethod
{
    /// <summary>Disables restarts.</summary>
    Disabled,

    /// <summary>Grows the conflict interval by a constant factor.</summary>
    Geometric,

    /// <summary>Uses the Luby universal restart sequence.</summary>
    Luby
}
