using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Analysis;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;

namespace SatSolver.Tests;

public sealed class CdclSolverTests
{
    [Fact]
    public void Solve_UnitClauseChain_ReturnsSat()
    {
        var formula = Formula(3, Clause(1), Clause(-1, 2), Clause(-2, 3));

        var result = Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.Equal([Literal(1), Literal(2), Literal(3)], result.Model);
        Assert.Equal(0, result.Statistics.Decisions);
        Assert.Equal(3, result.Statistics.UnitPropagations);
    }

    [Fact]
    public void Solve_OppositeUnitClauses_ReturnsUnsat()
    {
        var result = Solve(Formula(1, Clause(1), Clause(-1)));

        Assert.Equal(SolverStatus.UNSAT, result.Status);
        Assert.Equal(1, result.Statistics.Conflicts);
        Assert.Empty(result.Model);
    }

    [Fact]
    public void Solve_ConflictLearnsAndBackjumps()
    {
        var formula = Formula(
            4,
            Clause(1, 3, 4),
            Clause(-1, 2, 3),
            Clause(-1, -3),
            Clause(-2, -4),
            Clause(3, 4));

        var result = Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.True(result.Statistics.Conflicts >= 1);
        Assert.True(result.Statistics.LearnedClauses >= 1);
        Assert.True(result.Statistics.Backjumps >= 1);
    }

    [Theory]
    [MemberData(nameof(ConflictAnalyzers))]
    public void Solve_AlternativeConflictCuts_ReturnSat(IConflictAnalyzer analyzer)
    {
        var formula = Formula(
            4,
            Clause(1, 3, 4),
            Clause(-1, 2, 3),
            Clause(-1, -3),
            Clause(-2, -4),
            Clause(3, 4));

        var result = new CdclSolver(
            decisionHeuristic: new FirstUnassignedHeuristic(),
            conflictAnalyzer: analyzer).Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.True(result.Statistics.LearnedClauses >= 1);
    }

    [Fact]
    public void Solve_MultipleCuts_LearnsMoreThanOneClause()
    {
        var formula = Formula(
            4,
            Clause(-1, 2),
            Clause(-2, 3),
            Clause(-2, 4),
            Clause(-3, -4));

        var result = new CdclSolver(
            decisionHeuristic: new FirstUnassignedHeuristic(),
            conflictAnalyzer: new MultipleCutsConflictAnalyzer(),
            restartPolicy: new DisabledRestartPolicy(),
            clauseDeletion: ClauseDeletionMethod.Disabled).Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.True(result.Statistics.LearnedClauses >= 2);
    }

    public static IEnumerable<object[]> ConflictAnalyzers()
    {
        yield return [new FirstUipConflictAnalyzer()];
        yield return [new DecisionLiteralConflictAnalyzer()];
        yield return [new MultipleCutsConflictAnalyzer()];
    }

    private static SolverResult Solve(CnfFormula formula) =>
        new CdclSolver(new FirstUnassignedHeuristic()).Solve(formula);

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
