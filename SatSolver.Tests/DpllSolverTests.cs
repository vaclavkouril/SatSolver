using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Dpll;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Heuristics;

namespace SatSolver.Tests;

public sealed class DpllSolverTests
{
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

    [Fact]
    public void Solve_FirstBranchConflicts_BacktracksToASatisfyingBranch()
    {
        var formula = Formula(2, Clause(-1, 2), Clause(-1, -2), Clause(1, 2));

        var result = Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.Equal([Literal(-1), Literal(2)], result.Model);
        Assert.True(result.Statistics.Decisions >= 2);
    }

    [Fact]
    public void Solve_WatchedLiterals_FirstBranchConflicts_BacktracksToASatisfyingBranch()
    {
        var formula = Formula(3, Clause(-2), Clause(-1, 2, 3), Clause(-1, -3));

        var result = Solve(formula, new WatchedLiteralPropagator());

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.True(result.Statistics.Decisions >= 2);
    }

    [Fact]
    public void Solve_UnsatisfiableFormula_ReturnsNoModel()
    {
        var formula = Formula(1, Clause(1), Clause(-1));

        var result = Solve(formula);

        Assert.Equal(SolverStatus.UNSAT, result.Status);
        Assert.Empty(result.Model);
    }

    [Fact]
    public void Solve_ReusesAnInjectedPropagatorForTheNextFormula()
    {
        var solver = new DpllSolver(
            new FirstUnassignedHeuristic(),
            new WatchedLiteralPropagator());

        var first = solver.Solve(Formula(1, Clause(1)));
        var second = solver.Solve(Formula(1, Clause(-1)));

        Assert.Equal(SolverStatus.SAT, first.Status);
        Assert.Equal([Literal(1)], first.Model);
        Assert.Equal(SolverStatus.SAT, second.Status);
        Assert.Equal([Literal(-1)], second.Model);
    }

    private static SolverResult Solve(
        CnfFormula formula,
        IPropagationEngine? propagator = null) =>
        new DpllSolver(new FirstUnassignedHeuristic(), propagator).Solve(formula);

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
