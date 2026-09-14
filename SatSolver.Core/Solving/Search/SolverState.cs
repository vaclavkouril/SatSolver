using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Search;

public sealed class SolverState(CnfFormula formula) : ISolverStateView
{
    private readonly sbyte[] _values = new sbyte[formula.VariableCount + 1];
    private readonly int[] _decisionLevels = new int[formula.VariableCount + 1];
    private readonly ClauseReference?[] _reasons = new ClauseReference?[formula.VariableCount + 1];
    private readonly List<Literal> _trail = [];
    private readonly List<int> _levelStarts = [];

    // first trail literal not propagated yet
    private int _propagationHead;

    public CnfFormula Formula { get; } = formula;
    public int VariableCount => Formula.VariableCount;
    public IReadOnlyList<Literal> Trail => _trail;
    public int CurrentDecisionLevel => _levelStarts.Count;
    public event Action<int>? VariableUnassigned;

    public bool IsAssigned(int variable) => GetValue(variable).HasValue;

    public bool? GetValue(int variable)
    {
        if (variable < 1 || variable > VariableCount)
            throw new ArgumentOutOfRangeException(nameof(variable));

        return _values[variable] switch
        {
            1 => true,
            -1 => false,
            _ => null
        };
    }

    public void BeginDecisionLevel() => _levelStarts.Add(_trail.Count);

    public bool Enqueue(Literal literal, ClauseReference? reason)
    {
        if (literal.Variable < 1 || literal.Variable > VariableCount)
            throw new ArgumentOutOfRangeException(nameof(literal));

        var val = literal.IsNegated ? (sbyte)-1 : (sbyte)1;
        var oldVal = _values[literal.Variable];
        if (oldVal != 0)
            return oldVal == val;

        _values[literal.Variable] = val;
        _decisionLevels[literal.Variable] = CurrentDecisionLevel;
        _reasons[literal.Variable] = reason;
        _trail.Add(literal);
        return true;
    }

    public int GetDecisionLevel(int variable)
    {
        EnsureAssigned(variable);
        return _decisionLevels[variable];
    }

    public ClauseReference? GetReason(int variable)
    {
        EnsureAssigned(variable);
        return _reasons[variable];
    }

    public IReadOnlySet<ClauseReference> GetLockedClauses()
    {
        var locked = new HashSet<ClauseReference>();

        foreach (var reason in _reasons)
        {
            if (reason.HasValue)
                locked.Add(reason.Value);
        }

        return locked;
    }

    public bool TryTakeNextUnpropagatedLiteral(out Literal literal)
    {
        if (_propagationHead == _trail.Count)
        {
            literal = default;
            return false;
        }

        literal = _trail[_propagationHead++];
        return true;
    }

    public void BacktrackTo(int decisionLevel)
    {
        if (decisionLevel < 0 || decisionLevel > CurrentDecisionLevel)
            throw new ArgumentOutOfRangeException(nameof(decisionLevel));
        if (decisionLevel == CurrentDecisionLevel)
            return;

        var trailCount = _levelStarts[decisionLevel];
        UnassignFrom(trailCount);
        RemoveDecisionLevelsFrom(decisionLevel);
        _propagationHead = Math.Min(_propagationHead, _trail.Count);
    }

    public IReadOnlyList<Literal> CreateModel()
    {
        // unassigned variables to false
        var model = new Literal[VariableCount];

        for (var varId = 1; varId <= VariableCount; varId++)
            model[varId - 1] = new Literal(varId, _values[varId] is not 1);

        return model;
    }

    private void UnassignFrom(int trailCount)
    {
        while (_trail.Count > trailCount)
        {
            var lit = _trail[^1];
            _values[lit.Variable] = 0;
            _decisionLevels[lit.Variable] = 0;
            _reasons[lit.Variable] = null;
            _trail.RemoveAt(_trail.Count - 1);
            VariableUnassigned?.Invoke(lit.Variable);
        }
    }

    private void RemoveDecisionLevelsFrom(int level)
    {
        var count = _levelStarts.Count - level;
        if (count > 0)
            _levelStarts.RemoveRange(level, count);
    }

    private void EnsureAssigned(int variable)
    {
        if (variable < 1 || variable > VariableCount)
            throw new ArgumentOutOfRangeException(nameof(variable));
        if (!IsAssigned(variable))
            throw new InvalidOperationException("The variable is not assigned.");
    }
}
