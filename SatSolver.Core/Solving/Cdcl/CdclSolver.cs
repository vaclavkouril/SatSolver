using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl.Analysis;
using SatSolver.Core.Solving.Cdcl.Deletion;
using SatSolver.Core.Solving.Cdcl.Minimization;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Propagation;

namespace SatSolver.Core.Solving.Cdcl;

public sealed class CdclSolver : ISolver
{
    private readonly IDecisionHeuristic _heuristic;
    private readonly IPropagationEngine _propagator;
    private readonly IConflictAnalyzer _analyzer;
    private readonly ILearnedClauseMinimizer _minimizer;
    private readonly IRestartPolicy _restart;
    private readonly IClauseDeletionPolicy _deletion;
    private readonly LearnedClauseDeletionSchedule _schedule;

    public CdclSolver(
        IDecisionHeuristic? decisionHeuristic = null,
        IPropagationEngine? propagator = null,
        IConflictAnalyzer? conflictAnalyzer = null,
        ILearnedClauseMinimizer? minimizer = null,
        IRestartPolicy? restartPolicy = null,
        IClauseDeletionPolicy? clauseDeletionPolicy = null,
        LearnedClauseDeletionSchedule? deletionSchedule = null)
    {
        _heuristic = decisionHeuristic ?? new FirstUnassignedHeuristic();
        _propagator = propagator ?? new WatchedLiteralPropagator();
        _analyzer = conflictAnalyzer ?? new FirstUipConflictAnalyzer();
        _minimizer = minimizer ?? new NoOpLearnedClauseMinimizer();
        _restart = restartPolicy ?? new GeometricRestartPolicy();
        _deletion = clauseDeletionPolicy ?? new LbdThenActivityClauseDeletionPolicy();
        _schedule = deletionSchedule ?? new LearnedClauseDeletionSchedule();
    }

    public SolverResult Solve(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);
        return new CdclRun(
            formula,
            _heuristic,
            _propagator,
            _analyzer,
            _minimizer,
            _restart,
            _deletion,
            _schedule).Solve();
    }
}
