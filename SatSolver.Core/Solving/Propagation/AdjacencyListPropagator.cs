using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

/// <summary>Adjacency-list propagation.</summary>
internal sealed class AdjacencyListPropagator : IPropagationEngine
{
    private ClauseDatabase? _clauses;
    private List<ClauseReference>[]? _clausesByLiteral;
    private bool _requiresInitialScan;

    public void Initialize(ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        _clauses = clauses;
        _clausesByLiteral = CreateLiteralIndex(clauses.VariableCount);

        foreach (var clause in clauses.ActiveClauses)
            AddClauseToIndex(clause);

        // Initial scan; no trail trigger
        _requiresInitialScan = true;
    }

    public void RegisterClause(ClauseReference clause)
    {
        var clauses = GetInitializedClauses();
        AddClauseToIndex(clauses.Get(clause));
    }

    public PropagationResult Propagate(SolverState state, SearchStatistics statistics)
    {
        var clauses = GetInitializedClauses(state);

        if (_requiresInitialScan)
        {
            _requiresInitialScan = false;

            foreach (var clause in clauses.ActiveClauses)
            {
                var result = EvaluateClause(clause, state, statistics);
                if (result.HasConflict)
                    return result;
            }
        }

        while (state.TryTakeNextUnpropagatedLiteral(out var assignedLiteral))
        {
            var falsifiedLiteral = assignedLiteral.Negate();

            // Clauses that can become unit
            foreach (var clauseReference in _clausesByLiteral![GetLiteralIndex(falsifiedLiteral)])
            {
                var result = EvaluateClause(clauses.Get(clauseReference), state, statistics);
                if (result.HasConflict)
                    return result;
            }
        }

        return PropagationResult.NoConflict;
    }

    private PropagationResult EvaluateClause(
        SolverClause clause,
        SolverState state,
        SearchStatistics statistics)
    {
        if (clause.IsDeleted)
            return PropagationResult.NoConflict;

        statistics.RecordPropagationClauseCheck();

        Literal? onlyUnassigned = null;

        foreach (var literal in clause.Literals)
        {
            if (IsSatisfied(literal, state))
                return PropagationResult.NoConflict;

            if (!state.IsAssigned(literal.Variable))
            {
                if (onlyUnassigned.HasValue)
                    return PropagationResult.NoConflict;

                onlyUnassigned = literal;
            }
        }

        if (!onlyUnassigned.HasValue)
            return PropagationResult.Conflict(clause.Reference);

        if (!state.Enqueue(onlyUnassigned.Value, clause.Reference))
            return PropagationResult.Conflict(clause.Reference);

        if (clause.IsLearned)
            clause.BumpActivity();

        statistics.RecordUnitPropagations(1);
        return PropagationResult.NoConflict;
    }

    private void AddClauseToIndex(SolverClause clause)
    {
        if (clause.IsDeleted)
            return;

        foreach (var literal in clause.Literals)
            _clausesByLiteral![GetLiteralIndex(literal)].Add(clause.Reference);
    }

    private ClauseDatabase GetInitializedClauses(SolverState? state = null)
    {
        if (_clauses is null || _clausesByLiteral is null)
            throw new InvalidOperationException("The propagator was not initialized.");
        if (state is not null && !ReferenceEquals(_clauses.Formula, state.Formula))
            throw new InvalidOperationException("The propagator belongs to another formula.");

        return _clauses;
    }

    private static bool IsSatisfied(Literal literal, SolverState state) =>
        state.GetValue(literal.Variable) == !literal.IsNegated;

    private static List<ClauseReference>[] CreateLiteralIndex(int variableCount) =>
        Enumerable.Range(0, variableCount * 2)
            .Select(_ => new List<ClauseReference>())
            .ToArray();

    private static int GetLiteralIndex(Literal literal) =>
        2 * (literal.Variable - 1) + (literal.IsNegated ? 1 : 0);
}
