using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Cdcl.Deletion;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

/// <summary>Tests learned-clause deletion.</summary>
public sealed class CdclClauseDeletionTests
{
    /// <summary>Verifies LBD and activity ranking.</summary>
    [Fact]
    public void LbdThenActivityPolicy_SelectsWorstEligibleClauses()
    {
        var clauses = CreateDatabase();
        var lowLbd = AddLearned(clauses, lbd: 2, 1, 2, 3);
        var binary = AddLearned(clauses, lbd: 8, 1, 2);
        var medium = AddLearned(clauses, lbd: 4, 1, 2, 3);
        var highActivity = AddLearned(clauses, lbd: 7, 1, 2, 4);
        var lowActivity = AddLearned(clauses, lbd: 7, 1, 3, 4);
        var locked = AddLearned(clauses, lbd: 9, 2, 3, 4);
        var deleted = AddLearned(clauses, lbd: 10, 1, 2, 3);

        highActivity.BumpActivity(10);
        locked.BumpActivity(1);
        clauses.DeleteLearned(deleted.Reference);

        var policy = new LbdThenActivityClauseDeletionPolicy(new ClauseDeletionSettings
        {
            Method = ClauseDeletionMethod.LbdThenActivity,
            PermanentLbdLimit = 2,
            DeletionFraction = 0.5
        });

        var selected = policy.SelectForDeletion(
            clauses.Clauses,
            new HashSet<ClauseReference> { locked.Reference });

        Assert.Equal([lowActivity.Reference, highActivity.Reference], selected);
        Assert.DoesNotContain(lowLbd.Reference, selected);
        Assert.DoesNotContain(binary.Reference, selected);
        Assert.DoesNotContain(medium.Reference, selected);
        Assert.DoesNotContain(locked.Reference, selected);
        Assert.DoesNotContain(deleted.Reference, selected);
    }

    /// <summary>Verifies activity ranking.</summary>
    [Fact]
    public void ActivityPolicy_SelectsLeastActiveClausesFirst()
    {
        var clauses = CreateDatabase();
        var leastActive = AddLearned(clauses, lbd: 4, 1, 2, 3);
        var nextActive = AddLearned(clauses, lbd: 9, 1, 2, 4);
        var mostActive = AddLearned(clauses, lbd: 10, 1, 3, 4);

        nextActive.BumpActivity(1);
        mostActive.BumpActivity(2);

        var policy = new ActivityClauseDeletionPolicy(new ClauseDeletionSettings
        {
            Method = ClauseDeletionMethod.Activity,
            DeletionFraction = 0.5
        });

        var selected = policy.SelectForDeletion(clauses.LearnedClauses, new HashSet<ClauseReference>());

        Assert.Equal([leastActive.Reference, nextActive.Reference], selected);
    }

    /// <summary>Verifies deletion schedule growth.</summary>
    [Fact]
    public void DeletionSchedule_UsesStrictLimitAndGrowsAfterRound()
    {
        var schedule = new LearnedClauseDeletionSchedule(new ClauseDeletionSettings
        {
            Method = ClauseDeletionMethod.Lbd,
            InitialLearnedClauseLimit = 3,
            LimitGrowthFactor = 1.5
        });

        Assert.False(schedule.ShouldDelete(3));
        Assert.True(schedule.ShouldDelete(4));

        schedule.OnDeletionRound();

        Assert.Equal(5, schedule.CurrentLimit);
        Assert.False(schedule.ShouldDelete(5));
        Assert.True(schedule.ShouldDelete(6));
    }

    /// <summary>Verifies disabled deletion.</summary>
    [Fact]
    public void DisabledPolicy_SelectsNoClauses()
    {
        var clauses = CreateDatabase();
        AddLearned(clauses, lbd: 10, 1, 2, 3);

        var selected = new DisabledClauseDeletionPolicy()
            .SelectForDeletion(clauses.LearnedClauses, new HashSet<ClauseReference>());

        Assert.Empty(selected);
    }

    /// <summary>A disabled schedule never starts a deletion round.</summary>
    [Fact]
    public void DisabledSchedule_NeverRequestsDeletion()
    {
        var schedule = new LearnedClauseDeletionSchedule(new ClauseDeletionSettings
        {
            Method = ClauseDeletionMethod.Disabled
        });

        Assert.Equal(int.MaxValue, schedule.CurrentLimit);
        Assert.False(schedule.ShouldDelete(int.MaxValue));

        schedule.OnDeletionRound();

        Assert.Equal(int.MaxValue, schedule.CurrentLimit);
    }

    private static ClauseDatabase CreateDatabase() =>
        new(Formula(variableCount: 4, Clause(1, 2, 3)));

    private static SolverClause AddLearned(ClauseDatabase database, int lbd, params int[] literals)
    {
        var reference = database.AddLearned(new LearnedClause(literals.Select(Literal).ToArray(), lbd));
        return database.Get(reference);
    }

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(Literal).ToArray());

    private static Literal Literal(int signedVariable) =>
        new(Math.Abs(signedVariable), signedVariable < 0);
}
