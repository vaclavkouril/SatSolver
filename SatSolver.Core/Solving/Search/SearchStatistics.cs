using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Search;

/// <summary>Per-run statistics collector.</summary>
internal sealed class SearchStatistics
{
    public int Decisions { get; private set; }
    public int UnitPropagations { get; private set; }
    public long PropagationClauseChecks { get; private set; }
    public int Conflicts { get; private set; }
    public int Backjumps { get; private set; }
    public int Restarts { get; private set; }
    public int LearnedClauses { get; private set; }
    public int DeletedLearnedClauses { get; private set; }
    public long LearnedLiterals { get; private set; }
    public long LearnedLbdTotal { get; private set; }

    public void RecordDecision() => Decisions++;
    public void RecordUnitPropagations(int count) => UnitPropagations += count;
    public void RecordPropagationClauseCheck() => PropagationClauseChecks++;
    public void RecordConflict() => Conflicts++;
    public void RecordBackjump() => Backjumps++;
    public void RecordRestart() => Restarts++;
    public void RecordLearnedClause(int literalCount, int lbd)
    {
        LearnedClauses++;
        LearnedLiterals += literalCount;
        LearnedLbdTotal += lbd;
    }

    public void RecordDeletedLearnedClause() => DeletedLearnedClauses++;

    public SolverStatistics Create(TimeSpan cpuTime) =>
        new(
            cpuTime,
            Decisions,
            UnitPropagations,
            PropagationClauseChecks,
            Conflicts,
            Backjumps,
            Restarts,
            LearnedClauses,
            DeletedLearnedClauses,
            LearnedLiterals,
            LearnedLbdTotal);
}
