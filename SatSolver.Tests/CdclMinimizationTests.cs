using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Cdcl.Minimization;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

/// <summary>CDCL learned-clause minimization tests.</summary>
public sealed class CdclMinimizationTests
{
    /// <summary>Recursive reasons remove a literal implied through another antecedent.</summary>
    [Fact]
    public void RecursiveReasons_RemovesTransitivelyImpliedLiteral()
    {
        var formula = Formula(variableCount: 5);
        var clauses = new ClauseDatabase(formula);
        var firstReason = clauses.AddOriginal(Clause(-1, 2));
        var secondReason = clauses.AddOriginal(Clause(-2, 3));
        var state = new SolverState(formula);

        state.Enqueue(Literal(1), reason: null);
        state.BeginDecisionLevel();
        state.Enqueue(Literal(2), firstReason);
        state.Enqueue(Literal(3), secondReason);
        state.Enqueue(Literal(4), reason: null);
        state.Enqueue(Literal(5), reason: null);

        var source = LearnedClause(-4, 3, 5);

        var minimized = new RecursiveReasonLearnedClauseMinimizer()
            .Minimize(source, state, clauses);

        Assert.Equal([Literal(-4), Literal(5)], minimized.Literals);
        Assert.Equal(1, minimized.Lbd);
    }

    /// <summary>Recursive reasons retain a literal that depends on an external decision.</summary>
    [Fact]
    public void RecursiveReasons_KeepsLiteralWithExternalDecisionReason()
    {
        var formula = Formula(variableCount: 4);
        var clauses = new ClauseDatabase(formula);
        var reason = clauses.AddOriginal(Clause(-1, 2));
        var state = new SolverState(formula);

        state.BeginDecisionLevel();
        state.Enqueue(Literal(1), reason: null);
        state.Enqueue(Literal(2), reason);
        state.Enqueue(Literal(3), reason: null);
        state.Enqueue(Literal(4), reason: null);

        var source = LearnedClause(-3, 2, 4);

        var minimized = new RecursiveReasonLearnedClauseMinimizer()
            .Minimize(source, state, clauses);

        Assert.Equal(source.Literals, minimized.Literals);
    }

    /// <summary>Self-subsuming resolution removes a literal with a subsuming resolvent.</summary>
    [Fact]
    public void SelfSubsumingResolution_RemovesLiteralWhenResolventAddsNothing()
    {
        var formula = Formula(variableCount: 4);
        var clauses = new ClauseDatabase(formula);
        clauses.AddOriginal(Clause(-2, 3));
        var state = CreateLevelOneState(formula, 2, 3, 4);
        var source = LearnedClause(-4, 2, 3);

        var minimized = new SelfSubsumingResolutionMinimizer()
            .Minimize(source, state, clauses);

        Assert.Equal([Literal(-4), Literal(3)], minimized.Literals);
        Assert.Equal(1, minimized.Lbd);
    }

    /// <summary>Self-subsuming resolution retains a literal when its resolvent adds a literal.</summary>
    [Fact]
    public void SelfSubsumingResolution_KeepsLiteralWhenResolventAddsLiteral()
    {
        var formula = Formula(variableCount: 5);
        var clauses = new ClauseDatabase(formula);
        clauses.AddOriginal(Clause(-2, 5));
        var state = CreateLevelOneState(formula, 2, 3, 4, 5);
        var source = LearnedClause(-4, 2, 3);

        var minimized = new SelfSubsumingResolutionMinimizer()
            .Minimize(source, state, clauses);

        Assert.Equal(source.Literals, minimized.Literals);
    }

    /// <summary>All configured minimizers preserve the solver result.</summary>
    [Theory]
    [InlineData(ClauseMinimizationMethod.None)]
    [InlineData(ClauseMinimizationMethod.RecursiveReasons)]
    [InlineData(ClauseMinimizationMethod.SelfSubsumingResolution)]
    public void Solve_AllMinimizationMethods_ReturnSat(ClauseMinimizationMethod method)
    {
        var formula = Formula(
            variableCount: 4,
            Clause(1, 3, 4),
            Clause(-1, 2, 3),
            Clause(-1, -3),
            Clause(-2, -4),
            Clause(3, 4));

        var result = new CdclSolver(
            new FirstUnassignedHeuristic(),
            new CdclSolverOptions { Minimization = method }).Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.True(result.Statistics.LearnedClauses >= 1);
    }

    private static SolverState CreateLevelOneState(CnfFormula formula, params int[] literals)
    {
        var state = new SolverState(formula);
        state.BeginDecisionLevel();

        foreach (var literal in literals)
            state.Enqueue(Literal(literal), reason: null);

        return state;
    }

    private static LearnedClause LearnedClause(params int[] literals) =>
        new(literals.Select(Literal).ToArray(), lbd: 0);

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
