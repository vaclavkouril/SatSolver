namespace SatSolver.Core.Solving.Contracts;

public interface ISolverStateView
{
    int VariableCount { get; }

    bool IsAssigned(int variable);
}
