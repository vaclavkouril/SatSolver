using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

public sealed class DecisionHeuristicTests
{
    [Fact]
    public void StaticJeroslowWang_PrefersLiteralFromShortestClause()
    {
        var formula = Formula(3, Clause(1, 2, 3), Clause(-2), Clause(1, 3));
        var heuristic = new StaticJeroslowWangDecisionHeuristic();

        heuristic.Initialize(formula);

        var literal = heuristic.ChooseLiteral(new SolverState(formula));

        Assert.Equal(Literal(-2), literal);
    }

    [Fact]
    public void RandomDecision_UsesSeedAndSkipsAssignedVariables()
    {
        var formula = Formula(4);
        var first = new RandomDecisionHeuristic(seed: 17);
        var second = new RandomDecisionHeuristic(seed: 17);
        var firstState = StateWithAssignedVariable(formula, 1);
        var secondState = StateWithAssignedVariable(formula, 1);

        first.Initialize(formula);
        second.Initialize(formula);

        var firstChoice = first.ChooseLiteral(firstState);
        var secondChoice = second.ChooseLiteral(secondState);

        Assert.Equal(firstChoice, secondChoice);
        Assert.NotEqual(1, firstChoice!.Value.Variable);
    }

    [Fact]
    public void Vsids_BumpsEveryVariableInLearnedClause()
    {
        var formula = Formula(3);
        var heuristic = new VsidsDecisionHeuristic();

        heuristic.Initialize(formula);
        heuristic.OnLearnedClause([Literal(1), Literal(-2)]);

        Assert.Equal(1, heuristic.ActivityOf(1));
        Assert.Equal(1, heuristic.ActivityOf(2));
        Assert.Equal(0, heuristic.ActivityOf(3));
    }

    [Fact]
    public void Vsids_GrowsFutureBumpsAfterConflicts()
    {
        var formula = Formula(2);
        var heuristic = new VsidsDecisionHeuristic(decayFactor: 0.5);

        heuristic.Initialize(formula);
        heuristic.OnLearnedClause([Literal(1)]);
        heuristic.OnConflict();
        heuristic.OnLearnedClause([Literal(2)]);

        Assert.Equal(2, heuristic.CurrentBump);
        Assert.Equal(1, heuristic.ActivityOf(1));
        Assert.Equal(2, heuristic.ActivityOf(2));
    }

    [Fact]
    public void Vsids_BreaksEqualActivityTiesReproduciblyWithSeed()
    {
        var formula = Formula(20);
        var first = new VsidsDecisionHeuristic(randomSeed: 17);
        var second = new VsidsDecisionHeuristic(randomSeed: 17);

        first.Initialize(formula);
        second.Initialize(formula);

        var firstChoice = first.ChooseLiteral(new SolverState(formula));
        var secondChoice = second.ChooseLiteral(new SolverState(formula));

        Assert.Equal(firstChoice, secondChoice);
        Assert.True(firstChoice!.Value.IsNegated);
    }

    [Fact]
    public void Vsids_UsesDifferentSeededChoicesForEqualActivityTies()
    {
        var formula = Formula(20);
        var choices = Enumerable.Range(0, 10)
            .Select(seed => ChooseVsidsTie(formula, seed))
            .Select(literal => literal.Variable)
            .ToHashSet();

        Assert.True(choices.Count > 1);
    }

    [Fact]
    public void Vsids_RescalesActivitiesBeforeTheyOverflow()
    {
        var formula = Formula(1);
        var heuristic = new VsidsDecisionHeuristic(decayFactor: 0.5);

        heuristic.Initialize(formula);

        for (var index = 0; index < 5_000; index++)
        {
            heuristic.OnLearnedClause([Literal(1)]);
            heuristic.OnConflict();
        }

        Assert.True(double.IsFinite(heuristic.ActivityOf(1)));
        Assert.True(double.IsFinite(heuristic.CurrentBump));
    }

    [Fact]
    public void Cdcl_NotifiesConflictAwareHeuristicAboutLearning()
    {
        var formula = Formula(
            4,
            Clause(1, 3, 4),
            Clause(-1, 2, 3),
            Clause(-1, -3),
            Clause(-2, -4),
            Clause(3, 4));
        var heuristic = new TrackingConflictAwareHeuristic();

        new CdclSolver(heuristic).Solve(formula);

        Assert.True(heuristic.LearnedClauseCount >= 1);
        Assert.True(heuristic.ConflictCount >= 1);
    }

    private static SolverState StateWithAssignedVariable(CnfFormula formula, int variable)
    {
        var state = new SolverState(formula);
        state.Enqueue(new Literal(variable, IsNegated: false), reason: null);
        return state;
    }

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Literal ChooseVsidsTie(CnfFormula formula, int seed)
    {
        var heuristic = new VsidsDecisionHeuristic(randomSeed: seed);
        heuristic.Initialize(formula);

        return heuristic.ChooseLiteral(new SolverState(formula))!.Value;
    }

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);

    private sealed class TrackingConflictAwareHeuristic : IConflictAwareDecisionHeuristic
    {
        private readonly FirstUnassignedHeuristic _inner = new();

        public int LearnedClauseCount { get; private set; }
        public int ConflictCount { get; private set; }

        public void Initialize(CnfFormula formula) => _inner.Initialize(formula);

        public Literal? ChooseLiteral(ISolverStateView state) => _inner.ChooseLiteral(state);

        public void OnLearnedClause(IReadOnlyList<Literal> _) => LearnedClauseCount++;

        public void OnConflict() => ConflictCount++;
    }
}
