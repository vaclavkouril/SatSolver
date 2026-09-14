namespace SatSolver.Core.Solving.Cdcl.Configuration;

/// <summary>Selects the ranking used when deleting learned clauses.</summary>
public enum ClauseDeletionMethod
{
    /// <summary>Retains all learned clauses.</summary>
    Disabled,

    /// <summary>Prefers deleting clauses with low propagation activity.</summary>
    Activity,

    /// <summary>Prefers deleting clauses with high literal-block distance.</summary>
    Lbd,

    /// <summary>Ranks by literal-block distance and then activity.</summary>
    LbdThenActivity
}
