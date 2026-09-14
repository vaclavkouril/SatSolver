using SatSolver.Core.Solving.Cdcl.Restarts;

namespace SatSolver.Tests;

/// <summary>Tests CDCL restart policies.</summary>
public sealed class CdclRestartPolicyTests
{
    /// <summary>Verifies geometric intervals.</summary>
    [Fact]
    public void GeometricPolicy_GrowsConflictLimitAfterRestart()
    {
        var policy = new GeometricRestartPolicy(initialConflictLimit: 4, growthFactor: 1.5);

        Assert.False(policy.ShouldRestart(3));
        Assert.True(policy.ShouldRestart(4));

        policy.OnRestart();

        Assert.Equal(6, policy.CurrentConflictLimit);
        Assert.False(policy.ShouldRestart(5));
        Assert.True(policy.ShouldRestart(6));
    }

    /// <summary>Verifies Luby intervals.</summary>
    [Fact]
    public void LubyPolicy_FollowsScaledLubyIntervals()
    {
        var policy = new LubyRestartPolicy(unitRun: 10);
        var limits = new List<int>();

        for (var run = 0; run < 7; run++)
        {
            limits.Add(policy.CurrentConflictLimit);
            Assert.False(policy.ShouldRestart(policy.CurrentConflictLimit - 1));
            Assert.True(policy.ShouldRestart(policy.CurrentConflictLimit));
            policy.OnRestart();
        }

        Assert.Equal([10, 10, 20, 10, 10, 20, 40], limits);
    }

    /// <summary>Verifies disabled restarts.</summary>
    [Fact]
    public void DisabledPolicy_NeverRequestsRestart()
    {
        var policy = new DisabledRestartPolicy();

        Assert.Equal(int.MaxValue, policy.CurrentConflictLimit);
        Assert.False(policy.ShouldRestart(0));
        Assert.False(policy.ShouldRestart(int.MaxValue));
    }

    /// <summary>Verifies conflict-count validation.</summary>
    [Fact]
    public void RestartPolicy_NegativeConflictCount_Throws()
    {
        var policy = new GeometricRestartPolicy(initialConflictLimit: 1, growthFactor: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => policy.ShouldRestart(-1));
    }
}
