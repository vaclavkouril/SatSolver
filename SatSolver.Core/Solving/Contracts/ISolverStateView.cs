namespace SatSolver.Core.Solving.Contracts;

/// <summary>Read-only assignment state for decision heuristics.</summary>
public interface ISolverStateView
{
    int VariableCount { get; }

    bool IsAssigned(int variable);

    bool? GetValue(int variable);
    int CurrentDecisionLevel { get; }
}
