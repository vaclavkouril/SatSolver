namespace SatSolver.Core.Cnf;

public readonly record struct Literal(int Variable, bool IsNegated)
{
    public Literal Negate() => new(Variable, !IsNegated);
}
