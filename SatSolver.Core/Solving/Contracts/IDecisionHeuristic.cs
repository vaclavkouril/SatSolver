using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Contracts;

public interface IDecisionHeuristic
{
    void Initialize(CnfFormula formula);

    Literal? ChooseLiteral(ISolverStateView state);
}
