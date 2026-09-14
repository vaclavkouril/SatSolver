namespace SatSolver.Core.Solving.Cdcl.Configuration;

/// <summary>Configures the restart schedule of a CDCL run.</summary>
public sealed record RestartSettings
{
    /// <summary>Restart schedule.</summary>
    public RestartMethod Method { get; init; } = RestartMethod.Geometric;

    /// <summary>Initial conflict interval.</summary>
    public int InitialConflictLimit { get; init; } = 100;

    /// <summary>Geometric interval multiplier.</summary>
    public double GrowthFactor { get; init; } = 1.5;

    /// <summary>Luby sequence unit interval.</summary>
    public int LubyUnitRun { get; init; } = 100;

    /// <summary>Default restart configuration.</summary>
    public static RestartSettings Default { get; } = new();

    internal void Validate()
    {
        if (!Enum.IsDefined(Method))
            throw new ArgumentOutOfRangeException(nameof(Method));

        if (Method == RestartMethod.Disabled)
            return;

        if (InitialConflictLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(InitialConflictLimit));
        if (double.IsNaN(GrowthFactor) || double.IsInfinity(GrowthFactor) || GrowthFactor <= 1)
            throw new ArgumentOutOfRangeException(nameof(GrowthFactor));
        if (LubyUnitRun < 1)
            throw new ArgumentOutOfRangeException(nameof(LubyUnitRun));
    }
}
