namespace SatSolver.Core.Solving.Cdcl.Restarts;

public sealed class DisabledRestartPolicy : IRestartPolicy
{
    public int CurrentConflictLimit => int.MaxValue;

    public bool ShouldRestart(int conflictsSinceLastRestart)
    {
        if (conflictsSinceLastRestart < 0)
            throw new ArgumentOutOfRangeException(nameof(conflictsSinceLastRestart));

        return false;
    }

    public void OnRestart() { }

    public void Reset() { }
}
