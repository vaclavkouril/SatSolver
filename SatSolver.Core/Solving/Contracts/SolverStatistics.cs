namespace SatSolver.Core.Solving.Contracts;

/// <summary>Solver-run measurements.</summary>
public sealed record SolverStatistics(
    TimeSpan CpuTime,
    int Decisions,
    int UnitPropagations,
    long PropagationClauseChecks = 0,
    int Conflicts = 0,
    int Backjumps = 0,
    int Restarts = 0,
    int LearnedClauses = 0,
    int DeletedLearnedClauses = 0,
    long LearnedLiterals = 0,
    long LearnedLbdTotal = 0)
{
    /// <summary>Mean learned-clause length.</summary>
    public double AverageLearnedClauseLength =>
        LearnedClauses == 0 ? 0 : (double)LearnedLiterals / LearnedClauses;

    /// <summary>Mean learned-clause LBD.</summary>
    public double AverageLearnedClauseLbd =>
        LearnedClauses == 0 ? 0 : (double)LearnedLbdTotal / LearnedClauses;
}
