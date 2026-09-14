using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

public sealed class DecisionLiteralConflictAnalyzer : ResolutionConflictAnalyzer
{
    protected override bool IsCutReached(
        int openCurrentLiterals,
        ClauseReference? antecedent) =>
        !antecedent.HasValue;
}
