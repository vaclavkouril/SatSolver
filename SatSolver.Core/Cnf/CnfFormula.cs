namespace SatSolver.Core.Cnf;

public sealed record CnfFormula(int VariableCount, IReadOnlyList<Clause> Clauses)
{
    /// <summary>Optional descriptions used when writing DIMACS comments.</summary>
    public IReadOnlyList<CnfVariable> Variables { get; init; } = [];

    /// <summary>The signed literal representing the root of the encoded formula.</summary>
    public int RootLiteral { get; init; }
}
