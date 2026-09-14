using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Heuristics;

public sealed class FirstUnassignedHeuristic : IDecisionHeuristic
{
    public void Initialize(CnfFormula _) { }

    public Literal? ChooseLiteral(ISolverStateView state)
    {
        for (var varId = 1; varId <= state.VariableCount; varId++)
        {
            if (!state.IsAssigned(varId))
                return new Literal(varId, IsNegated: false);
        }

        return null;
    }
}
