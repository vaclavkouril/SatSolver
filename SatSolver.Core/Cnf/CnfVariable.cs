namespace SatSolver.Core.Cnf;

public enum CnfVariableKind
{
    Original,
    Auxiliary
}

/// <summary>Human-readable information about a DIMACS variable.</summary>
public sealed record CnfVariable(int Index, string Description, CnfVariableKind Kind);
