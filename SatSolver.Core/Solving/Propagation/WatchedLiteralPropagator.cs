using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

/// <summary>Lazy watched-literal propagation.</summary>
internal sealed class WatchedLiteralPropagator : IPropagationEngine
{
    private WatchedLiteralDatabase? _watches;
    private bool _requiresInitialUnitPropagation;

    public void Initialize(ClauseDatabase clauses)
    {
        _watches = new WatchedLiteralDatabase(clauses);

        // Root units; no false-watch event
        _requiresInitialUnitPropagation = true;
    }

    public void RegisterClause(ClauseReference clause) =>
        GetInitializedWatches().RegisterClause(clause);

    public PropagationResult Propagate(SolverState state, SearchStatistics statistics)
    {
        var watches = GetInitializedWatches(state);
        var initialResult = PropagateInitialUnits(watches, state, statistics);

        if (initialResult.HasConflict)
            return initialResult;

        while (state.TryTakeNextUnpropagatedLiteral(out var assignedLiteral))
        {
            var result = PropagateFalsifiedLiteral(
                watches,
                assignedLiteral.Negate(),
                state,
                statistics);

            if (result.HasConflict)
                return result;
        }

        return PropagationResult.NoConflict;
    }

    private PropagationResult PropagateInitialUnits(
        WatchedLiteralDatabase watches,
        SolverState state,
        SearchStatistics statistics)
    {
        if (!_requiresInitialUnitPropagation)
            return PropagationResult.NoConflict;

        _requiresInitialUnitPropagation = false;

        if (watches.EmptyClause is { } emptyClause)
        {
            statistics.RecordPropagationClauseCheck();
            return PropagationResult.Conflict(emptyClause);
        }

        foreach (var clauseReference in watches.InitialUnitClauses)
        {
            var clause = watches.Clauses.Get(clauseReference);
            if (clause.IsDeleted)
                continue;

            var literal = clause.Literals[0];
            var wasAssigned = state.IsAssigned(literal.Variable);
            statistics.RecordPropagationClauseCheck();

            if (!state.Enqueue(literal, clauseReference))
                return PropagationResult.Conflict(clauseReference);

            if (!wasAssigned)
            {
                if (clause.IsLearned)
                    clause.BumpActivity();

                statistics.RecordUnitPropagations(1);
            }
        }

        return PropagationResult.NoConflict;
    }

    private static PropagationResult PropagateFalsifiedLiteral(
        WatchedLiteralDatabase watches,
        Literal falsifiedLiteral,
        SolverState state,
        SearchStatistics statistics)
    {
        var watchedClauses = watches.GetClausesWatching(falsifiedLiteral);

        for (var index = 0; index < watchedClauses.Count;)
        {
            var clauseReference = watchedClauses[index];
            var update = UpdateClause(watches, clauseReference, falsifiedLiteral, state, statistics);

            if (update.Result.HasConflict)
                return update.Result;

            if (update.RemoveFromWatchList)
            {
                // Swap-back; retry this index
                RemoveAtSwapBack(watchedClauses, index);
                continue;
            }

            index++;
        }

        return PropagationResult.NoConflict;
    }

    private static ClauseUpdate UpdateClause(
        WatchedLiteralDatabase watches,
        ClauseReference clauseReference,
        Literal falsifiedLiteral,
        SolverState state,
        SearchStatistics statistics)
    {
        var clause = watches.Clauses.Get(clauseReference);
        if (clause.IsDeleted)
            return ClauseUpdate.Remove;

        statistics.RecordPropagationClauseCheck();

        var positions = watches.GetWatchPositions(clauseReference);
        var (falsifiedPosition, otherPosition) = GetAffectedPositions(clause, positions, falsifiedLiteral);
        var otherLiteral = clause.Literals[otherPosition];

        if (GetLiteralValue(otherLiteral, state) == LiteralValue.True)
            return ClauseUpdate.Keep;

        var replacementPosition = FindReplacementPosition(clause, falsifiedPosition, otherPosition, state);
        if (replacementPosition >= 0)
        {
            watches.MoveWatch(clauseReference, falsifiedPosition, replacementPosition);
            return ClauseUpdate.Remove;
        }

        // No replacement: unit or conflict
        if (!state.Enqueue(otherLiteral, clauseReference))
            return ClauseUpdate.Conflict(clauseReference);

        if (clause.IsLearned)
            clause.BumpActivity();

        statistics.RecordUnitPropagations(1);
        return ClauseUpdate.Keep;
    }

    private static (int Falsified, int Other) GetAffectedPositions(
        SolverClause clause,
        WatchPositions positions,
        Literal falsifiedLiteral)
    {
        // Two watches; one just became false
        if (clause.Literals[positions.First] == falsifiedLiteral)
            return (positions.First, positions.Second);

        if (clause.Literals[positions.Second] == falsifiedLiteral)
            return (positions.Second, positions.First);

        throw new InvalidOperationException("The clause does not watch the expected literal.");
    }

    private static int FindReplacementPosition(
        SolverClause clause,
        int falsifiedPosition,
        int otherPosition,
        SolverState state)
    {
        for (var position = 0; position < clause.Literals.Count; position++)
        {
            if (position == falsifiedPosition || position == otherPosition)
                continue;

            if (GetLiteralValue(clause.Literals[position], state) != LiteralValue.False)
                return position;
        }

        return -1;
    }

    private WatchedLiteralDatabase GetInitializedWatches(SolverState? state = null)
    {
        if (_watches is null)
            throw new InvalidOperationException("The propagator was not initialized.");
        if (state is not null && !ReferenceEquals(_watches.Clauses.Formula, state.Formula))
            throw new InvalidOperationException("The propagator belongs to another formula.");

        return _watches;
    }

    private static LiteralValue GetLiteralValue(Literal literal, SolverState state)
    {
        var value = state.GetValue(literal.Variable);

        if (!value.HasValue)
            return LiteralValue.Unassigned;

        return value.Value != literal.IsNegated
            ? LiteralValue.True
            : LiteralValue.False;
    }

    private static void RemoveAtSwapBack(List<ClauseReference> values, int index)
    {
        var lastIndex = values.Count - 1;
        values[index] = values[lastIndex];
        values.RemoveAt(lastIndex);
    }

    private readonly record struct ClauseUpdate(
        PropagationResult Result,
        bool RemoveFromWatchList)
    {
        public static ClauseUpdate Keep => new(PropagationResult.NoConflict, false);
        public static ClauseUpdate Remove => new(PropagationResult.NoConflict, true);
        public static ClauseUpdate Conflict(ClauseReference clause) =>
            new(PropagationResult.Conflict(clause), false);
    }

    private enum LiteralValue
    {
        False,
        Unassigned,
        True
    }
}
