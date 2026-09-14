namespace SatSolver.Core.Solving.Cdcl.Restarts;

/// <summary>Restart policy with geometrically growing conflict intervals.</summary>
internal sealed class GeometricRestartPolicy : IRestartPolicy
{
    private readonly double _growthFactor;

    public GeometricRestartPolicy(int initialConflictLimit, double growthFactor)
    {
        if (initialConflictLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(initialConflictLimit));
        if (double.IsNaN(growthFactor) || double.IsInfinity(growthFactor) || growthFactor <= 1)
            throw new ArgumentOutOfRangeException(nameof(growthFactor));

        CurrentConflictLimit = initialConflictLimit;
        _growthFactor = growthFactor;
    }

    public int CurrentConflictLimit { get; private set; }

    public bool ShouldRestart(int conflictsSinceLastRestart)
    {
        if (conflictsSinceLastRestart < 0)
            throw new ArgumentOutOfRangeException(nameof(conflictsSinceLastRestart));

        return conflictsSinceLastRestart >= CurrentConflictLimit;
    }

    public void OnRestart() => CurrentConflictLimit = Grow(CurrentConflictLimit, _growthFactor);

    private static int Grow(int currentLimit, double growthFactor)
    {
        if (currentLimit == int.MaxValue)
            return int.MaxValue;

        var grownLimit = Math.Ceiling(currentLimit * growthFactor);
        if (grownLimit >= int.MaxValue)
            return int.MaxValue;

        return Math.Max(currentLimit + 1, (int)grownLimit);
    }
}
