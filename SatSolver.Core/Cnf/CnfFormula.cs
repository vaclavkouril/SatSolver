namespace SatSolver.Core.Cnf;

public sealed record CnfFormula(int VariableCount, IReadOnlyList<Clause> Clauses)
{
    public IReadOnlyList<CnfVariable> Variables { get; init; } = [];

    public int RootLiteral { get; init; }
}
