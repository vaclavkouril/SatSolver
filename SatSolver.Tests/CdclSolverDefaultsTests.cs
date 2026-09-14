using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Deletion;
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
            new LearnedClauseDeletionSchedule(initialLearnedClauseLimit: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LbdThenActivityClauseDeletionPolicy(permanentLbdLimit: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new LbdThenActivityClauseDeletionPolicy(deletionFraction: 0));
    }

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
