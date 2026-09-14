namespace SatSolver.Core.Solving.Cdcl.Restarts;

public interface IRestartPolicy
{
    bool ShouldRestart(int conflictsSinceLastRestart);

    void OnRestart();

    void Reset();
}
