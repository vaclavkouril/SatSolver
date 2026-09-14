using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

public sealed class SolverStateTests
{
    [Fact]
    public void Enqueue_StoresDecisionLevelAndReason()
    {
        var state = new SolverState(new CnfFormula(2, []));

        state.Enqueue(Literal(1), new ClauseReference(7));
        state.BeginDecisionLevel();
        state.Enqueue(Literal(-2), reason: null);

        Assert.Equal(0, state.GetDecisionLevel(1));
        Assert.Equal(new ClauseReference(7), state.GetReason(1));
        Assert.Equal(1, state.GetDecisionLevel(2));
        Assert.Null(state.GetReason(2));
    }

    [Fact]
    public void BacktrackToZero_KeepsRootAssignments()
    {
        var state = new SolverState(new CnfFormula(3, []));

        state.Enqueue(Literal(1), new ClauseReference(1));
        state.BeginDecisionLevel();
        state.Enqueue(Literal(-2), reason: null);
        state.BeginDecisionLevel();
        state.Enqueue(Literal(3), new ClauseReference(2));

        state.BacktrackTo(0);

        Assert.Equal(0, state.CurrentDecisionLevel);
        Assert.Equal([Literal(1)], state.Trail);
        Assert.True(state.IsAssigned(1));
        Assert.False(state.IsAssigned(2));
        Assert.False(state.IsAssigned(3));
    }

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
