using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Contracts;

public sealed record SolverResult(
    SolverStatus Status,
    IReadOnlyList<Literal> Model,
    SolverStatistics Statistics);
