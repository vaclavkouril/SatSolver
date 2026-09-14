using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

public interface IConflictAnalyzer
{
    ConflictAnalysisResult Analyze(
        ClauseReference conflict,
        SolverState state,
        ClauseDatabase clauses);
}
