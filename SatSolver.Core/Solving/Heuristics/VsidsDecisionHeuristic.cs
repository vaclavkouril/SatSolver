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
    private int[] _tieRanks = [];
    private SortedSet<(double NegativeActivity, int TieRank, int Variable)> _candidates = [];
    private ISolverStateView? _state;

    public VsidsDecisionHeuristic(double decayFactor = 0.95, int randomSeed = 0)
    {
        if (!double.IsFinite(decayFactor) || decayFactor is <= 0 or >= 1)
            throw new ArgumentOutOfRangeException(nameof(decayFactor));

        _decay = decayFactor;
        _seed = randomSeed;
    }

    public void Initialize(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        if (_state is not null)
            _state.VariableUnassigned -= OnVariableUnassigned;
        _state = null;
        _activity = new double[formula.VariableCount + 1];
        _bump = 1;
        // fixed seeded permutation breaks ties without scanning the tied variables.
        _tieRanks = Enumerable.Range(0, formula.VariableCount + 1).ToArray();
        new Random(_seed).Shuffle(_tieRanks.AsSpan(1));
        RebuildCandidates();
    }

    public Literal? ChooseLiteral(ISolverStateView state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.VariableCount != _activity.Length - 1)
            throw new ArgumentException("The heuristic belongs to another formula size.", nameof(state));

        if (!ReferenceEquals(_state, state))
        {
            if (_state is not null)
            {
                _state.VariableUnassigned -= OnVariableUnassigned;
                RebuildCandidates();
            }
            _state = state;
            _state.VariableUnassigned += OnVariableUnassigned;
        }

        // remove assigned variables, once per assignment interval
        while (_candidates.Count > 0)
        {
            var best = _candidates.Min;
            if (!state.IsAssigned(best.Variable))
                return new Literal(best.Variable, IsNegated: true);
            _candidates.Remove(best);
        }

        return null;
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
        _bump /= _decay;

        if (_bump > ActivityRescaleLimit)
            RescaleActivities();
    }

    internal double ActivityOf(int varId) => _activity[varId];

    internal double CurrentBump => _bump;

    private void BumpActivity(int varId)
    {
        var wasCandidate = _candidates.Remove(PriorityOf(varId));
        _activity[varId] += _bump;
        if (wasCandidate)
            _candidates.Add(PriorityOf(varId));

        if (_activity[varId] > ActivityRescaleLimit)
            RescaleActivities();
    }

    private void RescaleActivities()
    {
        for (var idx = 1; idx < _activity.Length; idx++)
            _activity[idx] *= ActivityRescaleFactor;

        _bump *= ActivityRescaleFactor;
        _candidates = new(_candidates.Select(entry => PriorityOf(entry.Variable)));
    }

    private (double NegativeActivity, int TieRank, int Variable) PriorityOf(int variable) =>
        (-_activity[variable], _tieRanks[variable], variable);

    private void OnVariableUnassigned(int variable) => _candidates.Add(PriorityOf(variable));

    private void RebuildCandidates() =>
        _candidates = new(Enumerable.Range(1, _activity.Length - 1).Select(PriorityOf));
}
