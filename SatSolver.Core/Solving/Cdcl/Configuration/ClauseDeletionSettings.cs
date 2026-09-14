namespace SatSolver.Core.Solving.Cdcl.Configuration;

/// <summary>Configures periodic removal of learned clauses.</summary>
public sealed record ClauseDeletionSettings
{
    /// <summary>Deletion ranking.</summary>
    public ClauseDeletionMethod Method { get; init; } = ClauseDeletionMethod.LbdThenActivity;

    /// <summary>Initial learned-clause cache limit.</summary>
    public int InitialLearnedClauseLimit { get; init; } = 2_000;

    /// <summary>Cache-limit multiplier.</summary>
    public double LimitGrowthFactor { get; init; } = 1.5;

    /// <summary>Permanent-clause LBD threshold.</summary>
    public int PermanentLbdLimit { get; init; } = 2;

    /// <summary>Eligible-clause deletion fraction.</summary>
    public double DeletionFraction { get; init; } = 0.5;

    /// <summary>Default deletion configuration.</summary>
    public static ClauseDeletionSettings Default { get; } = new();

    internal void Validate()
    {
        if (!Enum.IsDefined(Method))
            throw new ArgumentOutOfRangeException(nameof(Method));

        if (Method == ClauseDeletionMethod.Disabled)
            return;

        if (InitialLearnedClauseLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(InitialLearnedClauseLimit));
        if (double.IsNaN(LimitGrowthFactor) || double.IsInfinity(LimitGrowthFactor) || LimitGrowthFactor <= 1)
            throw new ArgumentOutOfRangeException(nameof(LimitGrowthFactor));
        if (PermanentLbdLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(PermanentLbdLimit));
        if (double.IsNaN(DeletionFraction) || double.IsInfinity(DeletionFraction) ||
            DeletionFraction is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(DeletionFraction));
    }
}
