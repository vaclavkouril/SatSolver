using System.Diagnostics;
using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Propagation;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Dpll;

/// <summary>DPLL solver with chronological backtracking.</summary>
public sealed class DpllSolver : ISolver
{
    private readonly IDecisionHeuristic _decisionHeuristic;
    private readonly IPropagationEngine _propagator;

    /// <summary>Creates a DPLL solver.</summary>
    /// <param name="decisionHeuristic">Branching strategy.</param>
    /// <param name="propagationMethod">Propagation structure.</param>
    public DpllSolver(
        IDecisionHeuristic decisionHeuristic,
        PropagationMethod propagationMethod = PropagationMethod.AdjacencyLists)
    {
        ArgumentNullException.ThrowIfNull(decisionHeuristic);

        _decisionHeuristic = decisionHeuristic;
        _propagator = PropagationEngineFactory.Create(propagationMethod);
    }

    public SolverResult Solve(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        _decisionHeuristic.Initialize(formula);
        _propagator.Initialize(new ClauseDatabase(formula));
        var state = new SolverState(formula);
        var statistics = new SearchStatistics();
        using var process = Process.GetCurrentProcess();
        var cpuTimeBefore = process.TotalProcessorTime;

        var isSatisfiable = Search(state, statistics);
        var cpuTime = process.TotalProcessorTime - cpuTimeBefore;
        var resultStatistics = statistics.Create(cpuTime);

        return isSatisfiable
            ? new SolverResult(SolverStatus.SAT, state.CreateModel(), resultStatistics)
            : new SolverResult(SolverStatus.UNSAT, Array.Empty<Literal>(), resultStatistics);
    }

    private bool Search(SolverState state, SearchStatistics statistics)
    {
        if (_propagator.Propagate(state, statistics).HasConflict)
            return false;

        var decisionLiteral = _decisionHeuristic.ChooseLiteral(state);
        if (!decisionLiteral.HasValue)
            return true;

        var checkpoint = state.CreateCheckpoint();
        if (TryBranch(decisionLiteral.Value, state, statistics))
            return true;

        // Same propagated prefix
        state.Restore(checkpoint);
        if (TryBranch(decisionLiteral.Value.Negate(), state, statistics))
            return true;

        state.Restore(checkpoint);
        return false;
    }

    private bool TryBranch(Literal decision, SolverState state, SearchStatistics statistics)
    {
        statistics.RecordDecision();
        return state.TryAssign(decision) && Search(state, statistics);
    }
}
