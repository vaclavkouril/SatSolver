using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Heuristics;

/// <summary>Chooses the first unassigned positive literal.</summary>
public sealed class FirstUnassignedHeuristic : IDecisionHeuristic
{
    public void Initialize(CnfFormula formula) { }

    public Literal? ChooseLiteral(ISolverStateView state)
    {
        for (var variable = 1; variable <= state.VariableCount; variable++)
        {
            if (!state.IsAssigned(variable))
                return new Literal(variable, IsNegated: false);
        }

        return null;
    }
}
