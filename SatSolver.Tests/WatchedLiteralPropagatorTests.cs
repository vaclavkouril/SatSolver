using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Dpll;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

/// <summary>Tests watched-literal propagation.</summary>
public sealed class WatchedLiteralPropagatorTests
{
    /// <summary>Verifies watch movement before propagation.</summary>
    [Fact]
    public void Propagate_MovedWatch_PropagatesTheRemainingLiteral()
    {
        var result = Propagate(Formula(3, Clause(1, 2, 3)), Literal(-1), Literal(-2));

        Assert.False(result.HasConflict);
        Assert.Equal([Literal(-1), Literal(-2), Literal(3)], result.Assignments);
        Assert.Equal(1, result.UnitPropagations);
    }

    /// <summary>Verifies initial unit-clause propagation.</summary>
    [Fact]
    public void Propagate_UnitClauseChain_DerivesEveryConsequence()
    {
        var result = Propagate(Formula(3, Clause(1), Clause(-1, 2), Clause(-2, 3)));

        Assert.False(result.HasConflict);
        Assert.Equal([Literal(1), Literal(2), Literal(3)], result.Assignments);
        Assert.Equal(3, result.UnitPropagations);
    }

    /// <summary>Verifies an initial empty-clause conflict.</summary>
    [Fact]
    public void Propagate_EmptyClause_ReportsConflict()
    {
        var result = Propagate(Formula(1, Clause()));

        Assert.True(result.HasConflict);
    }

    private static PropagationTestResult Propagate(CnfFormula formula, params Literal[] initialAssignments)
    {
        var state = new SolverState(formula);
        var initialAssignmentConflict = initialAssignments.Any(literal => !state.TryAssign(literal));
        var statistics = new SearchStatistics();
        var propagator = new WatchedLiteralPropagator();
        propagator.Initialize(new ClauseDatabase(formula));

        var propagationResult = initialAssignmentConflict
            ? new PropagationResult(new ClauseReference(0))
            : propagator.Propagate(state, statistics);

        return new PropagationTestResult(
            propagationResult.HasConflict,
            state.Trail,
            statistics.UnitPropagations);
    }

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);

    private sealed record PropagationTestResult(
        bool HasConflict,
        IReadOnlyList<Literal> Assignments,
        int UnitPropagations);
}
