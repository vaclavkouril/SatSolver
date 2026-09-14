using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

public sealed class AdjacencyListPropagator : IPropagationEngine
{
    private ClauseDatabase? _clauses;
    private List<ClauseReference>[]? _byLiteral;
    private bool _requiresInitialScan;

    public void Initialize(ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        _clauses = clauses;
        _byLiteral = CreateLiteralIndex(clauses.VariableCount);
        _requiresInitialScan = true;

        foreach (var clause in clauses.ActiveClauses)
            AddClauseToIndex(clause);
    }

    public void RegisterClause(ClauseReference clause)
    {
        AddClauseToIndex(Clauses.Get(clause));
    }

    public PropagationResult Propagate(SolverState state, SearchStatistics statistics)
    {
        EnsureFormula(state);
        var result = PropagateInitialClauses(state, statistics);

        return result.HasConflict
            ? result
            : PropagateTrail(state, statistics);
    }

    private PropagationResult PropagateInitialClauses(
        SolverState state,
        SearchStatistics statistics)
    {
        if (!_requiresInitialScan)
            return PropagationResult.NoConflict;

        _requiresInitialScan = false;
        foreach (var clause in Clauses.ActiveClauses)
        {
            var result = EvaluateClause(clause, state, statistics);
            if (result.HasConflict)
                return result;
        }

        return PropagationResult.NoConflict;
    }

    private PropagationResult PropagateTrail(
        SolverState state,
        SearchStatistics statistics)
    {
        while (state.TryTakeNextUnpropagatedLiteral(out var assigned))
        {
            var result = PropagateFalsifiedLiteral(assigned.Negate(), state, statistics);
            if (result.HasConflict)
                return result;
        }

        return PropagationResult.NoConflict;
    }

    private PropagationResult PropagateFalsifiedLiteral(
        Literal falseLiteral,
        SolverState state,
        SearchStatistics statistics)
    {
        foreach (var clauseRef in ClausesByLiteral[GetLiteralIndex(falseLiteral)])
        {
            var result = EvaluateClause(Clauses.Get(clauseRef), state, statistics);
            if (result.HasConflict)
                return result;
        }

        return PropagationResult.NoConflict;
    }

    private static PropagationResult EvaluateClause(
        SolverClause clause,
        SolverState state,
        SearchStatistics statistics)
    {
        if (clause.IsDeleted)
            return PropagationResult.NoConflict;

        statistics.RecordPropagationClauseCheck();

        Literal? unit = null;

        foreach (var lit in clause.Literals)
        {
            if (IsSatisfied(lit, state))
                return PropagationResult.NoConflict;

            if (state.IsAssigned(lit.Variable))
                continue;

            if (unit.HasValue)
                return PropagationResult.NoConflict;

            unit = lit;
        }

        if (!unit.HasValue)
            return PropagationResult.Conflict(clause.Reference);

        if (!state.Enqueue(unit.Value, clause.Reference))
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

        foreach (var lit in clause.Literals)
            ClausesByLiteral[GetLiteralIndex(lit)].Add(clause.Reference);
    }

    private void EnsureFormula(SolverState state)
    {
        if (!ReferenceEquals(Clauses.Formula, state.Formula))
            throw new InvalidOperationException("The propagator belongs to another formula.");
    }

    private ClauseDatabase Clauses => _clauses
        ?? throw new InvalidOperationException("The propagator is not initialized.");

    private List<ClauseReference>[] ClausesByLiteral => _byLiteral
        ?? throw new InvalidOperationException("The propagator is not initialized.");

    private static bool IsSatisfied(Literal lit, SolverState state) =>
        state.GetValue(lit.Variable) == !lit.IsNegated;

    private static List<ClauseReference>[] CreateLiteralIndex(int varCount)
    {
        var byLiteral = new List<ClauseReference>[varCount * 2];

        for (var idx = 0; idx < byLiteral.Length; idx++)
            byLiteral[idx] = [];

        return byLiteral;
    }

    private static int GetLiteralIndex(Literal lit) =>
        2 * (lit.Variable - 1) + (lit.IsNegated ? 1 : 0);
}
