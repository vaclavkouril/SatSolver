using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

/// <summary>Creates learned-clause deletion policies from solver configuration.</summary>
internal static class ClauseDeletionPolicyFactory
{
    public static IClauseDeletionPolicy Create(ClauseDeletionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        return settings.Method switch
        {
            ClauseDeletionMethod.Disabled => new DisabledClauseDeletionPolicy(),
            ClauseDeletionMethod.Activity => new ActivityClauseDeletionPolicy(settings),
            ClauseDeletionMethod.Lbd => new LbdClauseDeletionPolicy(settings),
            ClauseDeletionMethod.LbdThenActivity => new LbdThenActivityClauseDeletionPolicy(settings),
            _ => throw new ArgumentOutOfRangeException(nameof(settings))
        };
    }
}
