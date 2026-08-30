namespace SatSolver.Core.Cnf;

public sealed record Clause(IReadOnlyList<Literal> Literals);
