using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Tests;

public sealed class CdclSolverDefaultsTests
{
    [Fact]
    public void DefaultSolver_ReusesComponentsAcrossSolves()
    {
        var solver = new CdclSolver();

        var satisfiable = solver.Solve(Formula(2, Clause(1), Clause(-1, 2)));
        var unsatisfiable = solver.Solve(Formula(1, Clause(1), Clause(-1)));

        Assert.Equal(SolverStatus.SAT, satisfiable.Status);
        Assert.Equal(SolverStatus.UNSAT, unsatisfiable.Status);
    }

    [Fact]
    public void StrategyConstructors_RejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GeometricRestartPolicy(initialConflictLimit: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LubyRestartPolicy(unitRun: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CdclSolver(deletionLimit: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CdclSolver(keepLbd: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CdclSolver(deletionFraction: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CdclSolver(clauseDeletion: (ClauseDeletionMethod)99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Solver_RejectsInvalidDeletionGrowth(double growth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CdclSolver(deletionGrowth: growth));
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Solver_RejectsInvalidDeletionFraction(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CdclSolver(deletionFraction: fraction));
    }

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
