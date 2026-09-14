using System.Diagnostics;
using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Dpll;

public sealed class DpllSolver : ISolver
{
    private readonly IDecisionHeuristic _decisionHeuristic;
    private readonly IPropagationEngine _propagator;

    public DpllSolver(
        IDecisionHeuristic decisionHeuristic,
        IPropagationEngine? propagator = null)
    {
        ArgumentNullException.ThrowIfNull(decisionHeuristic);

        _decisionHeuristic = decisionHeuristic;
        _propagator = propagator ?? new AdjacencyListPropagator();
    }

    public SolverResult Solve(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        _decisionHeuristic.Initialize(formula);
        var clauses = new ClauseDatabase(formula);
        _propagator.Initialize(clauses);
        var state = new SolverState(formula);
        var statistics = new SearchStatistics();
        using var process = Process.GetCurrentProcess();
        var cpuTimeBefore = process.TotalProcessorTime;

        var isSatisfiable = Search(state, statistics);
        var cpuTime = process.TotalProcessorTime - cpuTimeBefore;

        return CreateResult(isSatisfiable, state, statistics, cpuTime);
    }

    private bool Search(SolverState state, SearchStatistics statistics)
    {
        if (_propagator.Propagate(state, statistics).HasConflict)
            return false;

        var decision = _decisionHeuristic.ChooseLiteral(state);
        if (!decision.HasValue)
            return true;

        var level = state.CurrentDecisionLevel;
        if (TryBranch(decision.Value, state, statistics))
            return true;

        // Negated branch from the same propagated prefix
        state.BacktrackTo(level);
        if (TryBranch(decision.Value.Negate(), state, statistics))
            return true;

        state.BacktrackTo(level);
        return false;
    }

    private bool TryBranch(
        Literal decision,
        SolverState state,
        SearchStatistics statistics)
    {
        state.BeginDecisionLevel();
        statistics.RecordDecision();
        return state.Enqueue(decision, reason: null) && Search(state, statistics);
    }

    private static SolverResult CreateResult(
        bool isSatisfiable,
        SolverState state,
        SearchStatistics statistics,
        TimeSpan cpuTime)
    {
        var resultStatistics = statistics.Create(cpuTime);

        return isSatisfiable
            ? new SolverResult(SolverStatus.SAT, state.CreateModel(), resultStatistics)
            : new SolverResult(SolverStatus.UNSAT, Array.Empty<Literal>(), resultStatistics);
    }
}
