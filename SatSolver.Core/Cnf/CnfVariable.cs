namespace SatSolver.Core.Cnf;

public enum CnfVariableKind
{
    Original,
    Auxiliary
}

public sealed record CnfVariable(int Index, string Description, CnfVariableKind Kind);
