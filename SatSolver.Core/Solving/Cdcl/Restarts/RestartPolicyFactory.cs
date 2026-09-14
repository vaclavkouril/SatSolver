using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Restarts;

/// <summary>Creates restart policies from solver configuration.</summary>
internal static class RestartPolicyFactory
{
    public static IRestartPolicy Create(RestartSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        return settings.Method switch
        {
            RestartMethod.Disabled => new DisabledRestartPolicy(),
            RestartMethod.Geometric => new GeometricRestartPolicy(
                settings.InitialConflictLimit,
                settings.GrowthFactor),
            RestartMethod.Luby => new LubyRestartPolicy(settings.LubyUnitRun),
            _ => throw new ArgumentOutOfRangeException(nameof(settings))
        };
    }
}
