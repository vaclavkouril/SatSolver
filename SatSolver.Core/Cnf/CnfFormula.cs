namespace SatSolver.Core.Cnf;

public sealed record CnfFormula(int VariableCount, IReadOnlyList<Clause> Clauses);