using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

public sealed class FirstUipConflictAnalyzer : ResolutionConflictAnalyzer
{
    protected override bool IsCutReached(
        int openCurrentLiterals,
        ClauseReference? antecedent) =>
        openCurrentLiterals == 0;
}
