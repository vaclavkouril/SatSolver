namespace SatSolver.Core.Formulas;

public abstract record Formula;

public sealed record Variable(string Name) : Formula;

public sealed record Not(Variable Operand) : Formula;

public sealed record And(Formula Left, Formula Right) : Formula;

public sealed record Or(Formula Left, Formula Right) : Formula;