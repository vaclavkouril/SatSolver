namespace SatSolver.Core.Solving.Cdcl.Restarts;

public sealed class LubyRestartPolicy : IRestartPolicy
{
    private readonly int _unitRun;
    private int _run = 1;

    public LubyRestartPolicy(int unitRun = 100)
    {
        if (unitRun < 1)
            throw new ArgumentOutOfRangeException(nameof(unitRun));

        _unitRun = unitRun;
    }

    public int CurrentConflictLimit => SaturatingMultiply(_unitRun, ValueAt(_run));

    public bool ShouldRestart(int conflictsSinceLastRestart)
    {
        if (conflictsSinceLastRestart < 0)
            throw new ArgumentOutOfRangeException(nameof(conflictsSinceLastRestart));

        return conflictsSinceLastRestart >= CurrentConflictLimit;
    }

    public void OnRestart()
    {
        if (_run < int.MaxValue)
            _run++;
    }

    public void Reset() => _run = 1;

    // avoid restart interval overflow
    private static int SaturatingMultiply(int left, int right) =>
        (long)left * right >= int.MaxValue ? int.MaxValue : left * right;

    private static int ValueAt(int run)
    {
        // end of this Luby block
        var end = 1;
        var value = 1;
        while (end < run)
        {
            end = (end << 1) + 1;
            value <<= 1;
        }

        return run == end
            ? value
            : ValueAt(run - (end / 2));
    }
}
