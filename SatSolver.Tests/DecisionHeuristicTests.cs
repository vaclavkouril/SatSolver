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
    public void Vsids_ReinsertsBacktrackedVariablesWithTheirLatestActivity()
    {
        var formula = Formula(3);
        var state = new SolverState(formula);
        var heuristic = new VsidsDecisionHeuristic();
        heuristic.Initialize(formula);
        heuristic.OnLearnedClause([Literal(1)]);
        Assert.Equal(Literal(-1), heuristic.ChooseLiteral(state));

        state.BeginDecisionLevel();
        state.Enqueue(Literal(1), null);
        state.Enqueue(Literal(2), null); // propagated variables must also return
        Assert.Equal(Literal(-3), heuristic.ChooseLiteral(state));
        heuristic.OnLearnedClause([Literal(2)]);
        heuristic.OnLearnedClause([Literal(2)]);
        state.BacktrackTo(0);

        Assert.Equal(Literal(-2), heuristic.ChooseLiteral(state));
        // Choosing alone must not consume an unassigned candidate.
        Assert.Equal(Literal(-2), heuristic.ChooseLiteral(state));
    }

    [Fact]
    public void Vsids_DecisionsDoNotRepeatedlyScanAllVariables()
    {
        const int variableCount = 4_000;
        var formula = Formula(variableCount);
        var state = new CountingState(formula);
        var heuristic = new VsidsDecisionHeuristic();
        heuristic.Initialize(formula);

        for (var index = 0; index < variableCount; index++)
        {
            var choice = heuristic.ChooseLiteral(state);
            Assert.NotNull(choice);
            Assert.True(state.Inner.Enqueue(choice.Value, null));
        }

        Assert.Null(heuristic.ChooseLiteral(state));
        Assert.InRange(state.AssignmentChecks, variableCount, 3 * variableCount);
    }

    [Fact]
    public void Vsids_MatchesMaximumActivityAcrossRandomBacktracksAndRescaling()
    {
        var formula = Formula(32);
        var state = new SolverState(formula);
        var heuristic = new VsidsDecisionHeuristic(decayFactor: 0.5);
        var random = new Random(812);
        heuristic.Initialize(formula);

        for (var step = 0; step < 2_000; step++)
        {
            var bumped = Literal(random.Next(1, 33));
            var previous = heuristic.ActivityOf(bumped.Variable);
            var increment = heuristic.CurrentBump;
            heuristic.OnLearnedClause([bumped, bumped.Negate(), bumped]);
            if (previous + increment <= 1e100)
                Assert.Equal(previous + increment, heuristic.ActivityOf(bumped.Variable));
            heuristic.OnConflict();

            if (state.CurrentDecisionLevel > 0 && random.Next(4) == 0)
                state.BacktrackTo(random.Next(state.CurrentDecisionLevel));

            var available = Enumerable.Range(1, 32).Where(v => !state.IsAssigned(v)).ToArray();
            var choice = heuristic.ChooseLiteral(state);
            if (available.Length == 0)
            {
                Assert.Null(choice);
                state.BacktrackTo(0);
                continue;
            }

            Assert.NotNull(choice);
            Assert.Contains(choice.Value.Variable, available);
            Assert.Equal(available.Max(heuristic.ActivityOf), heuristic.ActivityOf(choice.Value.Variable));
            state.BeginDecisionLevel();
            state.Enqueue(choice.Value, null);
        }
    }

    [Fact]
    public void Vsids_CanSwitchStateAndReinitializeAfterExhaustion()
    {
        var formula = Formula(2);
        var heuristic = new VsidsDecisionHeuristic(randomSeed: 17);
        heuristic.Initialize(formula);
        var oldState = new SolverState(formula);
        oldState.BeginDecisionLevel();
        oldState.Enqueue(Literal(1), null);
        oldState.Enqueue(Literal(2), null);
        Assert.Null(heuristic.ChooseLiteral(oldState));
        Assert.NotNull(heuristic.ChooseLiteral(new SolverState(formula)));

        var smaller = Formula(1);
        heuristic.Initialize(smaller);
        oldState.BacktrackTo(0); // old subscriptions must not affect the new formula
        Assert.Equal(Literal(-1), heuristic.ChooseLiteral(new SolverState(smaller)));
        heuristic.Initialize(Formula(0));
        Assert.Null(heuristic.ChooseLiteral(new SolverState(Formula(0))));
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

    private sealed class CountingState(CnfFormula formula) : ISolverStateView
    {
        public SolverState Inner { get; } = new(formula);
        public int VariableCount => Inner.VariableCount;
        public int AssignmentChecks { get; private set; }
        public event Action<int>? VariableUnassigned
        {
            add => Inner.VariableUnassigned += value;
            remove => Inner.VariableUnassigned -= value;
        }

        public bool IsAssigned(int variable)
        {
            AssignmentChecks++;
            return Inner.IsAssigned(variable);
        }
    }

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
