using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Contracts;

/// <summary>Solver run result.</summary>
/// <param name="Status">Satisfiability status.</param>
/// <param name="Model">Model; empty for an unsatisfiable formula.</param>
/// <param name="Statistics">Run measurements.</param>
public sealed record SolverResult(
    SolverStatus Status,
    IReadOnlyList<Literal> Model,
    SolverStatistics Statistics);
