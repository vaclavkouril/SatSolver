using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

public sealed class WatchedLiteralPropagator : IPropagationEngine
{
    private WatchedLiteralDatabase? _watches;
    private bool _requiresInitialUnitPropagation;

    public void Initialize(ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        _watches = new WatchedLiteralDatabase(clauses);
        _requiresInitialUnitPropagation = true;
    }

    public void RegisterClause(ClauseReference clause) =>
        Watches.RegisterClause(clause);

    public PropagationResult Propagate(SolverState state, SearchStatistics statistics)
    {
        EnsureFormula(state);
        var result = PropagateInitialUnits(state, statistics);

        if (result.HasConflict)
            return result;

        while (state.TryTakeNextUnpropagatedLiteral(out var assigned))
        {
            result = PropagateFalsifiedLiteral(assigned.Negate(), state, statistics);

            if (result.HasConflict)
                return result;
        }

        return PropagationResult.NoConflict;
    }

    private PropagationResult PropagateInitialUnits(
        SolverState state,
        SearchStatistics statistics)
    {
        if (!_requiresInitialUnitPropagation)
            return PropagationResult.NoConflict;

        _requiresInitialUnitPropagation = false;

        if (Watches.EmptyClause is { } emptyClause)
        {
            statistics.RecordPropagationClauseCheck();
            return PropagationResult.Conflict(emptyClause);
        }

        foreach (var clauseRef in Watches.InitialUnitClauses)
        {
            var result = PropagateInitialUnitClause(clauseRef, state, statistics);

            if (result.HasConflict)
                return result;
        }

        return PropagationResult.NoConflict;
    }

    private PropagationResult PropagateInitialUnitClause(
        ClauseReference clauseRef,
        SolverState state,
        SearchStatistics statistics)
    {
        var clause = Watches.Clauses.Get(clauseRef);
        if (clause.IsDeleted)
            return PropagationResult.NoConflict;

        var lit = clause.Literals[0];
        var wasAssigned = state.IsAssigned(lit.Variable);
        statistics.RecordPropagationClauseCheck();

        if (!state.Enqueue(lit, clauseRef))
            return PropagationResult.Conflict(clauseRef);

        if (!wasAssigned)
            RecordUnitPropagation(clause, statistics);

        return PropagationResult.NoConflict;
    }

    private PropagationResult PropagateFalsifiedLiteral(
        Literal falseLiteral,
        SolverState state,
        SearchStatistics statistics)
    {
        var watchList = Watches.GetWatchList(falseLiteral);

        for (var idx = 0; idx < watchList.Count;)
        {
            var clauseRef = watchList[idx];
            var result = UpdateWatchForFalsifiedLiteral(
                clauseRef,
                falseLiteral,
                state,
                statistics,
                out var remove);

            if (result.HasConflict)
                return result;

            if (remove)
            {
                // retry this idx after swap-back
                RemoveAtSwapBack(watchList, idx);
                continue;
            }

            idx++;
        }

        return PropagationResult.NoConflict;
    }

    private PropagationResult UpdateWatchForFalsifiedLiteral(
        ClauseReference clauseRef,
        Literal falseLiteral,
        SolverState state,
        SearchStatistics statistics,
        out bool remove)
    {
        remove = false;

        var clause = Watches.Clauses.Get(clauseRef);
        if (clause.IsDeleted)
        {
            remove = true;
            return PropagationResult.NoConflict;
        }

        statistics.RecordPropagationClauseCheck();

        var positions = Watches.GetWatchPositions(clauseRef);
        var falsePos = FindFalsifiedWatchPosition(clause, positions, falseLiteral);
        var otherPos = GetOtherWatchedPosition(positions, falsePos);
        var otherLit = clause.Literals[otherPos];

        if (EvaluateLiteral(otherLit, state) == LiteralValue.True)
            return PropagationResult.NoConflict;

        if (TryReplaceFalsifiedWatch(
                clauseRef,
                clause,
                falsePos,
                otherPos,
                state))
        {
            remove = true;
            return PropagationResult.NoConflict;
        }

        // no replacement: the other watch is unit
        if (!state.Enqueue(otherLit, clauseRef))
            return PropagationResult.Conflict(clauseRef);

        RecordUnitPropagation(clause, statistics);
        return PropagationResult.NoConflict;
    }

    private static void RecordUnitPropagation(SolverClause clause, SearchStatistics statistics)
    {
        if (clause.IsLearned)
            clause.BumpActivity();

        statistics.RecordUnitPropagations(1);
    }

    private static int FindFalsifiedWatchPosition(
        SolverClause clause,
        WatchPositions positions,
        Literal falseLiteral)
    {
        if (clause.Literals[positions.First] == falseLiteral)
            return positions.First;

        if (clause.Literals[positions.Second] == falseLiteral)
            return positions.Second;

        throw new InvalidOperationException("The clause does not watch the expected literal.");
    }

    private static int GetOtherWatchedPosition(WatchPositions positions, int pos) =>
        positions.First == pos ? positions.Second : positions.First;

    private bool TryReplaceFalsifiedWatch(
        ClauseReference clauseRef,
        SolverClause clause,
        int falsePos,
        int otherPos,
        SolverState state)
    {
        var newPos = FindNonFalseReplacement(
            clause,
            falsePos,
            otherPos,
            state);
        if (newPos < 0)
            return false;

        Watches.MoveWatch(clauseRef, falsePos, newPos);
        return true;
    }

    private static int FindNonFalseReplacement(
        SolverClause clause,
        int falsePos,
        int otherPos,
        SolverState state)
    {
        for (var pos = 0; pos < clause.Literals.Count; pos++)
        {
            if (pos == falsePos || pos == otherPos)
                continue;

            if (EvaluateLiteral(clause.Literals[pos], state) != LiteralValue.False)
                return pos;
        }

        return -1;
    }

    private void EnsureFormula(SolverState state)
    {
        if (!ReferenceEquals(Watches.Clauses.Formula, state.Formula))
            throw new InvalidOperationException("The propagator belongs to another formula.");
    }

    private WatchedLiteralDatabase Watches => _watches
        ?? throw new InvalidOperationException("The propagator is not initialized.");

    private static LiteralValue EvaluateLiteral(Literal lit, SolverState state)
    {
        var val = state.GetValue(lit.Variable);

        if (!val.HasValue)
            return LiteralValue.Unassigned;

        var isTrue = lit.IsNegated ? !val.Value : val.Value;

        return isTrue
            ? LiteralValue.True
            : LiteralValue.False;
    }

    private static void RemoveAtSwapBack(List<ClauseReference> values, int idx)
    {
        var last = values.Count - 1;
        values[idx] = values[last];
        values.RemoveAt(last);
    }

    private enum LiteralValue
    {
        False,
        Unassigned,
        True
    }
}
