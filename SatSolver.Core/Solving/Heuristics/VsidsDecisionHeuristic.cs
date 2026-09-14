using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Heuristics;

public sealed class VsidsDecisionHeuristic : IConflictAwareDecisionHeuristic
{
    // MiniSAT overflow guard
    private const double ActivityRescaleLimit = 1e100;
    private const double ActivityRescaleFactor = 1e-100;

    private readonly double _decay;
    private readonly int _seed;
    private double[] _activity = [];
    private double _bump = 1;
    private Random _random;

    public VsidsDecisionHeuristic(double decayFactor = 0.95, int randomSeed = 0)
    {
        if (!double.IsFinite(decayFactor) || decayFactor is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(decayFactor));

        _decay = decayFactor;
        _seed = randomSeed;
        _random = new Random(randomSeed);
    }

    public void Initialize(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        _activity = new double[formula.VariableCount + 1];
        _bump = 1;
        _random = new Random(_seed);
    }

    public Literal? ChooseLiteral(ISolverStateView state)
    {
        var bestVar = 0;
        var best = double.NegativeInfinity;
        var ties = 0;

        for (var varId = 1; varId <= state.VariableCount; varId++)
        {
            if (state.IsAssigned(varId))
                continue;

            var activity = _activity[varId];
            if (activity > best)
            {
                bestVar = varId;
                best = activity;
                ties = 1;
                continue;
            }

            // keep tied variables equally likely
            if (activity == best && _random.Next(++ties) == 0)
                bestVar = varId;
        }

        return bestVar == 0
            ? null
            : new Literal(bestVar, IsNegated: true);
    }

    public void OnLearnedClause(IReadOnlyList<Literal> clause)
    {
        ArgumentNullException.ThrowIfNull(clause);

        // tolerate repeated variables
        var seen = new HashSet<int>();
        foreach (var lit in clause)
        {
            if (seen.Add(lit.Variable))
                BumpActivity(lit.Variable);
        }
    }

    public void OnConflict()
    {
        // decay by growing future bumps
        _bump /= _decay;

        if (_bump > ActivityRescaleLimit)
            RescaleActivities();
    }

    internal double ActivityOf(int varId) => _activity[varId];

    internal double CurrentBump => _bump;

    private void BumpActivity(int varId)
    {
        _activity[varId] += _bump;

        if (_activity[varId] > ActivityRescaleLimit)
            RescaleActivities();
    }

    private void RescaleActivities()
    {
        for (var idx = 1; idx < _activity.Length; idx++)
            _activity[idx] *= ActivityRescaleFactor;

        _bump *= ActivityRescaleFactor;
    }
}
