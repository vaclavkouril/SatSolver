using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Contracts;

/// <summary>Solves one immutable CNF formula.</summary>
public interface ISolver
{
    SolverResult Solve(CnfFormula formula);
}
