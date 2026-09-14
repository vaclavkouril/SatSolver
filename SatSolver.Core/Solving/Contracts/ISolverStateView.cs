namespace SatSolver.Core.Solving.Contracts;

public interface ISolverStateView
{
    int VariableCount { get; }

    // Raised after a variable becomes unassigned during backtracking.
    event Action<int>? VariableUnassigned;

    bool IsAssigned(int variable);
}
