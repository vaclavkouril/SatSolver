using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Analysis;
using SatSolver.Core.Solving.Cdcl.Deletion;
using SatSolver.Core.Solving.Cdcl.Minimization;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Propagation;

namespace SatSolver.Tests;

public sealed class CdclDifferentialTests
{
    [Fact]
    public void Solve_SmallFormulas_MatchesExhaustiveSearch()
    {
        foreach (var formula in SmallFormulas())
        {
            var expectedStatus = IsSatisfiable(formula) ? SolverStatus.SAT : SolverStatus.UNSAT;

            foreach (var solver in Solvers())
            {
                var result = solver.Solve(formula);

                Assert.Equal(expectedStatus, result.Status);
            }
        }
    }

    [Fact]
    public void Solve_AggressiveMaintenance_MatchesExhaustiveSearch()
    {
        var solver = new CdclSolver(
            decisionHeuristic: new FirstUnassignedHeuristic(),
            propagator: new WatchedLiteralPropagator(),
            conflictAnalyzer: new FirstUipConflictAnalyzer(),
            minimizer: new RecursiveReasonLearnedClauseMinimizer(),
            restartPolicy: new GeometricRestartPolicy(initialConflictLimit: 1, growthFactor: 2),
            clauseDeletionPolicy: new LbdThenActivityClauseDeletionPolicy(
                permanentLbdLimit: 0,
                deletionFraction: 1),
            deletionSchedule: new LearnedClauseDeletionSchedule(
                initialLearnedClauseLimit: 1,
                growthFactor: 2));

        var results = new List<SolverResult>();

        foreach (var formula in SmallFormulas())
        {
            var expectedStatus = IsSatisfiable(formula) ? SolverStatus.SAT : SolverStatus.UNSAT;
            var result = solver.Solve(formula);

            Assert.Equal(expectedStatus, result.Status);
            results.Add(result);
        }

        Assert.Contains(results, result => result.Statistics.Restarts > 0);
    }

    private static IEnumerable<CdclSolver> Solvers()
    {
        foreach (var propagator in Propagators())
        {
            foreach (var analyzer in ConflictAnalyzers())
            {
                foreach (var minimizer in Minimizers())
                {
                    yield return new CdclSolver(
                        decisionHeuristic: new FirstUnassignedHeuristic(),
                        propagator: propagator,
                        conflictAnalyzer: analyzer,
                        minimizer: minimizer,
                        restartPolicy: new DisabledRestartPolicy(),
                        clauseDeletionPolicy: new DisabledClauseDeletionPolicy(),
                        deletionSchedule: new LearnedClauseDeletionSchedule(isEnabled: false));
                }
            }
        }
    }

    private static IEnumerable<IPropagationEngine> Propagators()
    {
        yield return new AdjacencyListPropagator();
        yield return new WatchedLiteralPropagator();
    }

    private static IEnumerable<IConflictAnalyzer> ConflictAnalyzers()
    {
        yield return new FirstUipConflictAnalyzer();
        yield return new DecisionLiteralConflictAnalyzer();
        yield return new MultipleCutsConflictAnalyzer();
    }

    private static IEnumerable<ILearnedClauseMinimizer> Minimizers()
    {
        yield return new NoOpLearnedClauseMinimizer();
        yield return new RecursiveReasonLearnedClauseMinimizer();
        yield return new SelfSubsumingResolutionMinimizer();
    }

    private static IEnumerable<CnfFormula> SmallFormulas()
    {
        yield return Formula(1, Clause(1), Clause(-1));
        yield return Formula(3,
            Clause(1, 2, 3),
            Clause(1, 2, -3),
            Clause(1, -2, 3),
            Clause(1, -2, -3),
            Clause(-1, 2, 3),
            Clause(-1, 2, -3),
            Clause(-1, -2, 3),
            Clause(-1, -2, -3));
        yield return Formula(4,
            Clause(-1, 2),
            Clause(-2, 3),
            Clause(-2, 4),
            Clause(-3, -4));

        var random = new Random(29_041);

        for (var formulaIndex = 0; formulaIndex < 32; formulaIndex++)
        {
            var variableCount = 3 + formulaIndex % 2;
            var clauses = new List<Clause>();

            for (var clauseIndex = 0; clauseIndex < 2 + random.Next(8); clauseIndex++)
                clauses.Add(CreateClause(variableCount, random));

            yield return new CnfFormula(variableCount, clauses);
        }
    }

    private static Clause CreateClause(int variableCount, Random random)
    {
        var variables = Enumerable.Range(1, variableCount)
            .OrderBy(_ => random.Next())
            .Take(1 + random.Next(Math.Min(3, variableCount)))
            .Select(variable => new Literal(variable, random.Next(2) == 0))
            .ToArray();

        return new Clause(variables);
    }

    private static bool IsSatisfiable(CnfFormula formula)
    {
        for (var assignment = 0; assignment < 1 << formula.VariableCount; assignment++)
        {
            if (formula.Clauses.All(clause => clause.Literals.Any(literal =>
                    HasLiteralValue(literal, assignment))))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasLiteralValue(Literal literal, int assignment)
    {
        var value = (assignment & (1 << (literal.Variable - 1))) != 0;
        return literal.IsNegated ? !value : value;
    }

    private static CnfFormula Formula(int variableCount, params Clause[] clauses) =>
        new(variableCount, clauses);

    private static Clause Clause(params int[] literals) =>
        new(literals.Select(literal => new Literal(Math.Abs(literal), literal < 0)).ToArray());
}
