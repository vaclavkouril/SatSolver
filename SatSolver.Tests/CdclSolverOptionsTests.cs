using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Propagation;

namespace SatSolver.Tests;

/// <summary>Tests CDCL configuration validation.</summary>
public sealed class CdclSolverOptionsTests
{
    /// <summary>Verifies default CDCL configuration.</summary>
    [Fact]
    public void DefaultOptions_SelectStandardCdclComponents()
    {
        var options = CdclSolverOptions.Default;

        Assert.Equal(PropagationMethod.WatchedLiterals, options.Propagation);
        Assert.Equal(ConflictAnalysisMethod.FirstUip, options.ConflictAnalysis);
        Assert.Equal(ClauseMinimizationMethod.None, options.Minimization);
        Assert.Equal(RestartMethod.Geometric, options.Restart.Method);
        Assert.Equal(ClauseDeletionMethod.LbdThenActivity, options.ClauseDeletion.Method);
    }

    /// <summary>Verifies invalid propagation validation.</summary>
    [Fact]
    public void Constructor_UnknownPropagationMethod_Throws()
    {
        var options = new CdclSolverOptions
        {
            Propagation = (PropagationMethod)999
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSolver(options));
    }

    /// <summary>Verifies restart-limit validation.</summary>
    [Fact]
    public void Constructor_InvalidActiveRestartSettings_Throws()
    {
        var options = new CdclSolverOptions
        {
            Restart = new RestartSettings
            {
                Method = RestartMethod.Geometric,
                InitialConflictLimit = 0
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSolver(options));
    }

    /// <summary>Verifies deletion-limit validation.</summary>
    [Fact]
    public void Constructor_InvalidActiveDeletionSettings_Throws()
    {
        var options = new CdclSolverOptions
        {
            ClauseDeletion = new ClauseDeletionSettings
            {
                Method = ClauseDeletionMethod.Lbd,
                InitialLearnedClauseLimit = 0
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateSolver(options));
    }

    private static CdclSolver CreateSolver(CdclSolverOptions options) =>
        new(new FirstUnassignedHeuristic(), options);
}
