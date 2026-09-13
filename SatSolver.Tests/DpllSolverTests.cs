using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Dpll;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Heuristics;

namespace SatSolver.Tests;

/// <summary>Tests the DPLL solver.</summary>
public sealed class DpllSolverTests
{
    /// <summary>Verifies solution by unit propagation.</summary>
    [Fact]
    public void Solve_UnitClauseChain_ReturnsSatModelAndPropagationStatistics()
    {
        var formula = Formula(3, Clause(1), Clause(-1, 2), Clause(-2, 3));

        var result = Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.Equal([Literal(1), Literal(2), Literal(3)], result.Model);
        Assert.Equal(0, result.Statistics.Decisions);
        Assert.Equal(3, result.Statistics.UnitPropagations);
        Assert.True(result.Statistics.CpuTime >= TimeSpan.Zero);
    }

    /// <summary>Verifies branch backtracking.</summary>
    [Fact]
    public void Solve_FirstBranchConflicts_BacktracksToASatisfyingBranch()
    {
        var formula = Formula(2, Clause(-1, 2), Clause(-1, -2), Clause(1, 2));

        var result = Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.Equal([Literal(-1), Literal(2)], result.Model);
        Assert.True(result.Statistics.Decisions >= 2);
    }

    /// <summary>Verifies watched propagation after backtracking.</summary>
    [Fact]
    public void Solve_WatchedLiterals_FirstBranchConflicts_BacktracksToASatisfyingBranch()
    {
        var formula = Formula(3, Clause(-2), Clause(-1, 2, 3), Clause(-1, -3));

        var result = Solve(formula, PropagationMethod.WatchedLiterals);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.True(result.Statistics.Decisions >= 2);
    }

    /// <summary>Verifies an unsatisfiable formula.</summary>
    [Fact]
    public void Solve_UnsatisfiableFormula_ReturnsNoModel()
    {
        var formula = Formula(1, Clause(1), Clause(-1));

        var result = Solve(formula);

        Assert.Equal(SolverStatus.UNSAT, result.Status);
        Assert.Empty(result.Model);
    }

    private static SolverResult Solve(
        CnfFormula formula,
        PropagationMethod propagationMethod = PropagationMethod.AdjacencyLists) =>
        new DpllSolver(new FirstUnassignedHeuristic(), propagationMethod).Solve(formula);

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
