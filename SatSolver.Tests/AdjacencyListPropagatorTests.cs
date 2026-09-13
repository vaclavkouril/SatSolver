using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Dpll;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

/// <summary>Tests adjacency-list propagation.</summary>
public sealed class AdjacencyListPropagatorTests
{
    /// <summary>Verifies a formula without units.</summary>
    [Fact]
    public void Propagate_WithoutUnitClauses_ReturnsNoAssignments()
    {
        var result = Propagate(Formula(2, Clause(1, 2)));

        Assert.False(result.HasConflict);
        Assert.Empty(result.Assignments);
        Assert.Equal(0, result.UnitPropagations);
    }

    /// <summary>Verifies unit-clause propagation.</summary>
    [Fact]
    public void Propagate_UnitClauseChain_DerivesEveryConsequence()
    {
        var result = Propagate(Formula(3, Clause(1), Clause(-1, 2), Clause(-2, 3)));

        Assert.False(result.HasConflict);
        Assert.Equal([Literal(1), Literal(2), Literal(3)], result.Assignments);
        Assert.Equal(3, result.UnitPropagations);
    }

    /// <summary>Verifies contradictory unit clauses.</summary>
    [Fact]
    public void Propagate_OppositeUnitClauses_ReportsConflict()
    {
        var result = Propagate(Formula(1, Clause(1), Clause(-1)));

        Assert.True(result.HasConflict);
    }

    /// <summary>Verifies an initial empty-clause conflict.</summary>
    [Fact]
    public void Propagate_EmptyClause_ReportsConflict()
    {
        var result = Propagate(Formula(1, Clause()));

        Assert.True(result.HasConflict);
    }

    /// <summary>Verifies propagation after initial assignments.</summary>
    [Fact]
    public void Propagate_InitialAssignments_DerivesOnlyTheirConsequences()
    {
        var result = Propagate(Formula(3, Clause(1, 2, 3)), Literal(-1), Literal(-2));

        Assert.False(result.HasConflict);
        Assert.Equal([Literal(-1), Literal(-2), Literal(3)], result.Assignments);
        Assert.Equal(1, result.UnitPropagations);
    }

    /// <summary>Verifies a decision conflicting with a unit clause.</summary>
    [Fact]
    public void Propagate_InitialAssignmentContradictsUnitClause_ReportsConflict()
    {
        var result = Propagate(Formula(1, Clause(1)), Literal(-1));

        Assert.True(result.HasConflict);
    }

    /// <summary>Verifies that satisfied clauses do not propagate.</summary>
    [Fact]
    public void Propagate_SatisfiedClause_DoesNotCreateAFalseUnitLiteral()
    {
        var result = Propagate(Formula(2, Clause(1, 2)), Literal(1));

        Assert.False(result.HasConflict);
        Assert.Equal([Literal(1)], result.Assignments);
        Assert.Equal(0, result.UnitPropagations);
    }

    private static PropagationTestResult Propagate(CnfFormula formula, params Literal[] initialAssignments)
    {
        var state = new SolverState(formula);
        var initialAssignmentConflict = initialAssignments.Any(literal => !state.TryAssign(literal));
        var statistics = new SearchStatistics();
        var propagator = new AdjacencyListPropagator();
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
