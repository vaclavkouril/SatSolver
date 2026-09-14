using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Contracts;

public interface ISolver
{
    SolverResult Solve(CnfFormula formula);
}
