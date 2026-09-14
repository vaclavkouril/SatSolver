using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

/// <summary>Creates learned-clause minimizers from solver configuration.</summary>
internal static class LearnedClauseMinimizerFactory
{
    public static ILearnedClauseMinimizer Create(ClauseMinimizationMethod method) => method switch
    {
        ClauseMinimizationMethod.None => new NoOpLearnedClauseMinimizer(),
        ClauseMinimizationMethod.RecursiveReasons => new RecursiveReasonLearnedClauseMinimizer(),
        ClauseMinimizationMethod.SelfSubsumingResolution => new SelfSubsumingResolutionMinimizer(),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };
}
