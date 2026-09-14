using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Tests;

public sealed class CdclClauseDeletionTests
{
    [Fact]
    public void LbdActivity_SelectsWorstEligibleClauses()
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

        var selected = CdclRun.SelectForDeletion(
            clauses.Clauses,
            new HashSet<ClauseReference> { locked.Reference },
            ClauseDeletionMethod.LbdActivity,
            keepLbd: 2,
            deletionFraction: 0.5);

        Assert.Equal([lowActivity.Reference, highActivity.Reference], selected);
        Assert.DoesNotContain(lowLbd.Reference, selected);
        Assert.DoesNotContain(binary.Reference, selected);
        Assert.DoesNotContain(medium.Reference, selected);
        Assert.DoesNotContain(locked.Reference, selected);
        Assert.DoesNotContain(deleted.Reference, selected);
    }

    [Fact]
    public void Activity_SelectsLeastActiveClausesFirst()
    {
        var clauses = CreateDatabase();
        var leastActive = AddLearned(clauses, lbd: 4, 1, 2, 3);
        var nextActive = AddLearned(clauses, lbd: 9, 1, 2, 4);
        var mostActive = AddLearned(clauses, lbd: 10, 1, 3, 4);

        nextActive.BumpActivity(1);
        mostActive.BumpActivity(2);

        var selected = CdclRun.SelectForDeletion(
            clauses.LearnedClauses, new HashSet<ClauseReference>(),
            ClauseDeletionMethod.Activity, keepLbd: 2, deletionFraction: 0.5);

        Assert.Equal([leastActive.Reference, nextActive.Reference], selected);
    }

    [Fact]
    public void DeletionMethods_MatchStableSortingForDifferentDeletionFractions()
    {
        var clauses = CreateDatabase();
        var random = new Random(721);
        for (var index = 0; index < 100; index++)
        {
            var clause = AddLearned(clauses, random.Next(3, 12), 1, 2, 3);
            clause.BumpActivity(random.Next(1, 5));
        }

        // Reversed input also checks that ties follow input order, not clause IDs.
        var input = clauses.LearnedClauses.Reverse().ToArray();
        foreach (var fraction in new[] { 0.01, 0.25, 0.5, 1.0 })
        {
            var count = (int)Math.Ceiling(input.Length * fraction);
            AssertSelection(ClauseDeletionMethod.Lbd,
                input.OrderByDescending(c => c.Lbd));
            AssertSelection(ClauseDeletionMethod.Activity,
                input.OrderBy(c => c.Activity).ThenByDescending(c => c.Lbd));
            AssertSelection(ClauseDeletionMethod.LbdActivity,
                input.OrderByDescending(c => c.Lbd).ThenBy(c => c.Activity));

            void AssertSelection(ClauseDeletionMethod method, IEnumerable<SolverClause> expected)
            {
                Assert.Equal(expected.Take(count).Select(c => c.Reference),
                    CdclRun.SelectForDeletion(input, new HashSet<ClauseReference>(), method, 2, fraction));
                Assert.Empty(CdclRun.SelectForDeletion([], new HashSet<ClauseReference>(), method, 2, fraction));
            }
        }
    }

    [Fact]
    public void Database_TracksActiveLearnedCountAcrossRepeatedDeletion()
    {
        var clauses = CreateDatabase();
        Assert.Equal(0, clauses.ActiveLearnedClauseCount);
        var first = AddLearned(clauses, 4, 1, 2, 3);
        AddLearned(clauses, 4, 1, 2, 4);
        Assert.Equal(2, clauses.ActiveLearnedClauseCount);
        clauses.DeleteLearned(first.Reference);
        clauses.DeleteLearned(first.Reference);
        Assert.Equal(1, clauses.ActiveLearnedClauseCount);
        Assert.Equal(clauses.LearnedClauses.Count(), clauses.ActiveLearnedClauseCount);
    }

    [Theory]
    [InlineData(ClauseDeletionMethod.Activity)]
    [InlineData(ClauseDeletionMethod.Lbd)]
    [InlineData(ClauseDeletionMethod.LbdActivity)]
    public void Solve_DeletesClausesAndStartsWithFreshLimitEachTime(ClauseDeletionMethod method)
    {
        var solver = new CdclSolver(
            restartPolicy: new DisabledRestartPolicy(),
            clauseDeletion: method, deletionLimit: 1, deletionGrowth: 1.5,
            keepLbd: 0, deletionFraction: 1);
        var formula = CompleteUnsatisfiableFormula();

        var first = solver.Solve(formula);
        var second = solver.Solve(formula);

        Assert.Equal(SolverStatus.UNSAT, first.Status);
        Assert.True(first.Statistics.DeletedLearnedClauses > 0);
        Assert.Equal(first.Status, second.Status);
        Assert.Equal(first.Statistics with { CpuTime = TimeSpan.Zero },
            second.Statistics with { CpuTime = TimeSpan.Zero });
    }

    [Fact]
    public void Solve_DisabledDeletion_KeepsAllLearnedClauses()
    {
        var result = new CdclSolver(
            clauseDeletion: ClauseDeletionMethod.Disabled, deletionLimit: 1,
            keepLbd: 0, deletionFraction: 1).Solve(CompleteUnsatisfiableFormula());

        Assert.Equal(SolverStatus.UNSAT, result.Status);
        Assert.True(result.Statistics.LearnedClauses > 1);
        Assert.Equal(0, result.Statistics.DeletedLearnedClauses);
    }

    [Fact]
    public void Solve_AtDeletionLimit_KeepsAllLearnedClauses()
    {
        var formula = CompleteUnsatisfiableFormula();
        var baseline = new CdclSolver(clauseDeletion: ClauseDeletionMethod.Disabled).Solve(formula);
        var result = new CdclSolver(
            deletionLimit: baseline.Statistics.LearnedClauses,
            keepLbd: 0, deletionFraction: 1).Solve(formula);

        Assert.Equal(baseline.Statistics.LearnedClauses, result.Statistics.LearnedClauses);
        Assert.Equal(0, result.Statistics.DeletedLearnedClauses);
    }

    [Fact]
    public void Solve_HugeGrowth_StopsFurtherDeletionWithoutOverflow()
    {
        var result = new CdclSolver(
            restartPolicy: new DisabledRestartPolicy(),
            deletionLimit: 1, deletionGrowth: double.MaxValue,
            keepLbd: 0, deletionFraction: 1).Solve(CompleteUnsatisfiableFormula());

        Assert.Equal(SolverStatus.UNSAT, result.Status);
        Assert.Equal(1, result.Statistics.DeletedLearnedClauses);
    }

    // One clause excludes each possible assignment, requiring several rounds of learning.
    private static CnfFormula CompleteUnsatisfiableFormula() =>
        new(5, Enumerable.Range(0, 32).Select(assignment =>
            new Clause(Enumerable.Range(1, 5).Select(variable =>
                new Literal(variable, (assignment & (1 << (variable - 1))) != 0)).ToArray())).ToArray());

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
