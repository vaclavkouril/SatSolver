using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl.Analysis;
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
    private readonly ClauseDeletionMethod _clauseDeletion;
    private readonly double _deletionGrowth;
    private readonly int _keepLbd;
    private readonly double _deletionFraction;
    private readonly int _deletionLimit;

    public CdclSolver(
        IDecisionHeuristic? decisionHeuristic = null,
        IPropagationEngine? propagator = null,
        IConflictAnalyzer? conflictAnalyzer = null,
        ILearnedClauseMinimizer? minimizer = null,
        IRestartPolicy? restartPolicy = null,
        ClauseDeletionMethod clauseDeletion = ClauseDeletionMethod.LbdActivity,
        int deletionLimit = 2_000,
        double deletionGrowth = 1.5,
        int keepLbd = 2,
        double deletionFraction = 0.5)
    {
        if (!Enum.IsDefined(clauseDeletion))
            throw new ArgumentOutOfRangeException(nameof(clauseDeletion));
        if (clauseDeletion != ClauseDeletionMethod.Disabled)
        {
            if (deletionLimit < 1)
                throw new ArgumentOutOfRangeException(nameof(deletionLimit));
            if (!double.IsFinite(deletionGrowth) || deletionGrowth <= 1)
                throw new ArgumentOutOfRangeException(nameof(deletionGrowth));
            if (keepLbd < 0)
                throw new ArgumentOutOfRangeException(nameof(keepLbd));
            if (!double.IsFinite(deletionFraction) || deletionFraction is <= 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(deletionFraction));
        }

        _heuristic = decisionHeuristic ?? new FirstUnassignedHeuristic();
        _propagator = propagator ?? new WatchedLiteralPropagator();
        _analyzer = conflictAnalyzer ?? new FirstUipConflictAnalyzer();
        _minimizer = minimizer ?? new NoOpLearnedClauseMinimizer();
        _restart = restartPolicy ?? new GeometricRestartPolicy();
        _clauseDeletion = clauseDeletion;
        _deletionLimit = deletionLimit;
        _deletionGrowth = deletionGrowth;
        _keepLbd = keepLbd;
        _deletionFraction = deletionFraction;
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
            _clauseDeletion,
            _deletionLimit,
            _deletionGrowth,
            _keepLbd,
            _deletionFraction).Solve();
    }
}
