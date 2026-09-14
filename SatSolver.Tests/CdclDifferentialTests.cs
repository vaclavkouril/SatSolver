using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Propagation;

namespace SatSolver.Tests;

/// <summary>Cross-checks configurable CDCL search.</summary>
public sealed class CdclDifferentialTests
{
    /// <summary>Configured CDCL variants agree with exhaustive search.</summary>
    [Fact]
    public void Solve_SmallFormulas_MatchesExhaustiveSearch()
    {
        foreach (var formula in SmallFormulas())
        {
            var expectedStatus = IsSatisfiable(formula) ? SolverStatus.SAT : SolverStatus.UNSAT;

            foreach (var options in Configurations())
            {
                var result = new CdclSolver(new FirstUnassignedHeuristic(), options).Solve(formula);

                Assert.Equal(expectedStatus, result.Status);
            }
        }
    }

    /// <summary>Aggressive restarts and deletion preserve satisfiability.</summary>
    [Fact]
    public void Solve_AggressiveMaintenance_MatchesExhaustiveSearch()
    {
        var options = new CdclSolverOptions
        {
            Propagation = PropagationMethod.WatchedLiterals,
            ConflictAnalysis = ConflictAnalysisMethod.FirstUip,
            Minimization = ClauseMinimizationMethod.RecursiveReasons,
            Restart = new RestartSettings
            {
                Method = RestartMethod.Geometric,
                InitialConflictLimit = 1,
                GrowthFactor = 2
            },
            ClauseDeletion = new ClauseDeletionSettings
            {
                Method = ClauseDeletionMethod.LbdThenActivity,
                InitialLearnedClauseLimit = 1,
                LimitGrowthFactor = 2,
                PermanentLbdLimit = 0,
                DeletionFraction = 1
            }
        };

        var results = new List<SolverResult>();

        foreach (var formula in SmallFormulas())
        {
            var expectedStatus = IsSatisfiable(formula) ? SolverStatus.SAT : SolverStatus.UNSAT;
            var result = new CdclSolver(new FirstUnassignedHeuristic(), options).Solve(formula);

            Assert.Equal(expectedStatus, result.Status);
            results.Add(result);
        }

        Assert.Contains(results, result => result.Statistics.Restarts > 0);
    }

    private static IEnumerable<CdclSolverOptions> Configurations()
    {
        foreach (var propagation in Enum.GetValues<PropagationMethod>())
        {
            foreach (var analysis in Enum.GetValues<ConflictAnalysisMethod>())
            {
                foreach (var minimization in Enum.GetValues<ClauseMinimizationMethod>())
                {
                    yield return new CdclSolverOptions
                    {
                        Propagation = propagation,
                        ConflictAnalysis = analysis,
                        Minimization = minimization,
                        Restart = new RestartSettings { Method = RestartMethod.Disabled },
                        ClauseDeletion = new ClauseDeletionSettings { Method = ClauseDeletionMethod.Disabled }
                    };
                }
            }
        }
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
