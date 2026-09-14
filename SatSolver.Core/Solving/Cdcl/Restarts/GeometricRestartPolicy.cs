namespace SatSolver.Core.Solving.Cdcl.Restarts;

public sealed class GeometricRestartPolicy : IRestartPolicy
{
    private readonly int _initialConflictLimit;
    private readonly double _growthFactor;

    public GeometricRestartPolicy(
        int initialConflictLimit = 100,
        double growthFactor = 1.5)
    {
        if (initialConflictLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(initialConflictLimit));
        if (!double.IsFinite(growthFactor) || growthFactor <= 1)
            throw new ArgumentOutOfRangeException(nameof(growthFactor));

        _initialConflictLimit = initialConflictLimit;
        _growthFactor = growthFactor;
        Reset();
    }

    public int CurrentConflictLimit { get; private set; }

    public bool ShouldRestart(int conflictsSinceLastRestart)
    {
        if (conflictsSinceLastRestart < 0)
            throw new ArgumentOutOfRangeException(nameof(conflictsSinceLastRestart));

        return conflictsSinceLastRestart >= CurrentConflictLimit;
    }

    public void OnRestart() => CurrentConflictLimit = Grow(CurrentConflictLimit, _growthFactor);

    public void Reset() => CurrentConflictLimit = _initialConflictLimit;

    private static int Grow(int currentLimit, double growthFactor)
    {
        if (currentLimit == int.MaxValue)
            return int.MaxValue;

        var next = Math.Ceiling(currentLimit * growthFactor);
        if (next >= int.MaxValue)
            return int.MaxValue;

        return Math.Max(currentLimit + 1, (int)next);
    }
}
