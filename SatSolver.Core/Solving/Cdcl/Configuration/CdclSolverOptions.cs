using SatSolver.Core.Solving.Propagation;

namespace SatSolver.Core.Solving.Cdcl.Configuration;

/// <summary>Configures the algorithms used by a CDCL solver.</summary>
public sealed record CdclSolverOptions
{
    /// <summary>Propagation structure.</summary>
    public PropagationMethod Propagation { get; init; } = PropagationMethod.WatchedLiterals;

    /// <summary>Conflict-cut strategy.</summary>
    public ConflictAnalysisMethod ConflictAnalysis { get; init; } = ConflictAnalysisMethod.FirstUip;

    /// <summary>Learned-clause minimization.</summary>
    public ClauseMinimizationMethod Minimization { get; init; } = ClauseMinimizationMethod.None;

    /// <summary>Restart configuration.</summary>
    public RestartSettings Restart { get; init; } = RestartSettings.Default;

    /// <summary>Learned-clause deletion configuration.</summary>
    public ClauseDeletionSettings ClauseDeletion { get; init; } = ClauseDeletionSettings.Default;

    /// <summary>Default CDCL configuration.</summary>
    public static CdclSolverOptions Default { get; } = new();

    internal void Validate()
    {
        if (!Enum.IsDefined(Propagation))
            throw new ArgumentOutOfRangeException(nameof(Propagation));
        if (!Enum.IsDefined(ConflictAnalysis))
            throw new ArgumentOutOfRangeException(nameof(ConflictAnalysis));
        if (!Enum.IsDefined(Minimization))
            throw new ArgumentOutOfRangeException(nameof(Minimization));

        ArgumentNullException.ThrowIfNull(Restart);
        ArgumentNullException.ThrowIfNull(ClauseDeletion);
        Restart.Validate();
        ClauseDeletion.Validate();
    }
}
