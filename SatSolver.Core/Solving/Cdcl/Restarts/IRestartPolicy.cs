namespace SatSolver.Core.Solving.Cdcl.Restarts;

/// <summary>Determines when a CDCL run restarts from decision level zero.</summary>
internal interface IRestartPolicy
{
    int CurrentConflictLimit { get; }

    bool ShouldRestart(int conflictsSinceLastRestart);

    void OnRestart();
}
