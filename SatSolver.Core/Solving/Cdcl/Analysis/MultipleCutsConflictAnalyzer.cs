using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

public sealed class MultipleCutsConflictAnalyzer : ResolutionConflictAnalyzer
{
    protected override bool IsCutReached(
        int openCurrentLiterals,
        ClauseReference? antecedent) =>
        openCurrentLiterals == 0;

    protected override bool ShouldFinishAnalysis(
        ClauseReference? antecedent,
        int pivotVar,
        SolverState state,
        ClauseDatabase clauses) =>
        !HasAnotherCurrentLevelPivot(antecedent, pivotVar, state, clauses);
}
