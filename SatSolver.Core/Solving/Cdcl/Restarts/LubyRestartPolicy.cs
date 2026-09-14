namespace SatSolver.Core.Solving.Cdcl.Restarts;

/// <summary>Restart policy whose intervals follow the Luby sequence.</summary>
internal sealed class LubyRestartPolicy : IRestartPolicy
{
    private readonly int _unitRun;
    private int _runIndex = 1;

    public LubyRestartPolicy(int unitRun)
    {
        if (unitRun < 1)
            throw new ArgumentOutOfRangeException(nameof(unitRun));

        _unitRun = unitRun;
    }

    public int CurrentConflictLimit => SaturatingMultiply(_unitRun, LubySequence.ValueAt(_runIndex));

    public bool ShouldRestart(int conflictsSinceLastRestart)
    {
        if (conflictsSinceLastRestart < 0)
            throw new ArgumentOutOfRangeException(nameof(conflictsSinceLastRestart));

        return conflictsSinceLastRestart >= CurrentConflictLimit;
    }

    public void OnRestart()
    {
        if (_runIndex < int.MaxValue)
            _runIndex++;
    }

    private static int SaturatingMultiply(int left, int right) =>
        (long)left * right >= int.MaxValue ? int.MaxValue : left * right;
}
