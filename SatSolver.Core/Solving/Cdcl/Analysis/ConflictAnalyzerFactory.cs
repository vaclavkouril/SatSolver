using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Analysis;

/// <summary>Conflict-analyzer factory.</summary>
internal static class ConflictAnalyzerFactory
{
    public static IConflictAnalyzer Create(ConflictAnalysisMethod method) =>
        new ResolutionConflictAnalyzer(method);
}
