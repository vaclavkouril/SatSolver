namespace SatSolver.Core.Solving.Cdcl.Configuration;

/// <summary>Selects the cut used to learn clauses from a conflict.</summary>
public enum ConflictAnalysisMethod
{
    /// <summary>Stops at the first unique implication point.</summary>
    FirstUip,

    /// <summary>Stops at the decision literal without an antecedent.</summary>
    DecisionLiteral,

    /// <summary>Learns clauses from more than one valid cut.</summary>
    MultipleCuts
}
