using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Minimization;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

public sealed class CdclMinimizationTests
{
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelfSubsumingResolution_IndexTracksAdditionsAndDeletions(bool buildIndexFirst)
    {
        var formula = Formula(4, Clause(2, 3)); // wrong polarity must not be a candidate
        var clauses = new ClauseDatabase(formula);
        var state = CreateLevelOneState(formula, 2, 3, 4);
        var source = LearnedClause(-4, 2, 3);
        var minimizer = new SelfSubsumingResolutionMinimizer();
        if (buildIndexFirst)
            Assert.Equal(source.Literals, minimizer.Minimize(source, state, clauses).Literals);

        var reference = clauses.AddLearned(LearnedClause(-2, 3));
        Assert.Equal([Literal(-4), Literal(3)], minimizer.Minimize(source, state, clauses).Literals);
        clauses.DeleteLearned(reference);
        Assert.Equal(source.Literals, minimizer.Minimize(source, state, clauses).Literals);

        var original = clauses.AddOriginal(Clause(-2, 3, -2));
        Assert.Equal([Literal(-4), Literal(3)], minimizer.Minimize(source, state, clauses).Literals);
        Assert.Equal([original], clauses.GetActiveClausesContaining(Literal(-2)).Select(c => c.Reference));
    }

    [Theory]
    [MemberData(nameof(Minimizers))]
    public void Solve_AllMinimizationMethods_ReturnSat(ILearnedClauseMinimizer minimizer)
    {
        var formula = Formula(
            variableCount: 4,
            Clause(1, 3, 4),
            Clause(-1, 2, 3),
            Clause(-1, -3),
            Clause(-2, -4),
            Clause(3, 4));

        var result = new CdclSolver(
            decisionHeuristic: new FirstUnassignedHeuristic(),
            minimizer: minimizer).Solve(formula);

        Assert.Equal(SolverStatus.SAT, result.Status);
        Assert.True(result.Statistics.LearnedClauses >= 1);
    }

    public static IEnumerable<object[]> Minimizers()
    {
        yield return [new NoOpLearnedClauseMinimizer()];
        yield return [new RecursiveReasonLearnedClauseMinimizer()];
        yield return [new SelfSubsumingResolutionMinimizer()];
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
