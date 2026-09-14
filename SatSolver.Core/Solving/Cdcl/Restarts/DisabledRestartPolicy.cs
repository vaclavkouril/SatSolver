namespace SatSolver.Core.Solving.Cdcl.Restarts;

/// <summary>Restart policy that never requests a restart.</summary>
internal sealed class DisabledRestartPolicy : IRestartPolicy
{
    public int CurrentConflictLimit => int.MaxValue;

    public bool ShouldRestart(int conflictsSinceLastRestart)
    {
        EnsureNonNegative(conflictsSinceLastRestart);
        return false;
    }

    public void OnRestart() { }

    private static void EnsureNonNegative(int conflictsSinceLastRestart)
    {
        if (conflictsSinceLastRestart < 0)
            throw new ArgumentOutOfRangeException(nameof(conflictsSinceLastRestart));
    }
}
