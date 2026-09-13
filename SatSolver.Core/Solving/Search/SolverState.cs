using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Search;

/// <summary>DPLL backtracking checkpoint.</summary>
internal readonly record struct SearchCheckpoint(
    int TrailCount,
    int PropagationHead,
    int DecisionLevelCount);

/// <summary>Mutable assignment trail for DPLL and CDCL.</summary>
/// <remarks>Propagated assignments retain their reason clauses for conflict analysis.</remarks>
internal sealed class SolverState(CnfFormula formula) : ISolverStateView
{
    private readonly sbyte[] _values = new sbyte[formula.VariableCount + 1];
    private readonly int[] _decisionLevels = new int[formula.VariableCount + 1];
    private readonly ClauseReference?[] _reasons = new ClauseReference?[formula.VariableCount + 1];
    private readonly List<Literal> _trail = [];
    private readonly List<int> _decisionTrailStarts = [];
    private int _propagationHead;

    public CnfFormula Formula { get; } = formula;
    public int VariableCount => Formula.VariableCount;
    public IReadOnlyList<Literal> Trail => _trail;
    /// <summary>Gets the current decision level.</summary>
    public int CurrentDecisionLevel => _decisionTrailStarts.Count;

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

    public SearchCheckpoint CreateCheckpoint() =>
        new(_trail.Count, _propagationHead, _decisionTrailStarts.Count);

    /// <summary>Starts a decision level.</summary>
    public void BeginDecisionLevel() => _decisionTrailStarts.Add(_trail.Count);

    public bool TryAssign(Literal literal) => Enqueue(literal, reason: null);

    /// <summary>Adds a consistent literal to the trail.</summary>
    /// <param name="reason">Reason clause; <see langword="null"/> for a decision.</param>
    /// <returns><see langword="false"/> if the variable has the opposite value.</returns>
    public bool Enqueue(Literal literal, ClauseReference? reason)
    {
        if (literal.Variable < 1 || literal.Variable > VariableCount)
            throw new ArgumentOutOfRangeException(nameof(literal));

        var value = literal.IsNegated ? (sbyte)-1 : (sbyte)1;
        var existingValue = _values[literal.Variable];
        if (existingValue != 0)
            return existingValue == value;

        _values[literal.Variable] = value;
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

    /// <summary>Gets the reason clause, if any.</summary>
    public ClauseReference? GetReason(int variable)
    {
        EnsureAssigned(variable);
        return _reasons[variable];
    }

    /// <summary>Gets clauses locked as trail reasons.</summary>
    public IReadOnlySet<ClauseReference> GetLockedClauses() =>
        _reasons
            .Where(reason => reason.HasValue)
            .Select(reason => reason!.Value)
            .ToHashSet();

    /// <summary>Gets the next unpropagated trail literal.</summary>
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

    /// <summary>Restores a DPLL checkpoint.</summary>
    public void Restore(SearchCheckpoint checkpoint)
    {
        UnassignFrom(checkpoint.TrailCount);
        RemoveDecisionLevelsFrom(checkpoint.DecisionLevelCount);
        _propagationHead = checkpoint.PropagationHead;
    }

    /// <summary>Removes assignments above a decision level.</summary>
    /// <remarks>Supports non-chronological CDCL backjumping.</remarks>
    public void BacktrackTo(int decisionLevel)
    {
        if (decisionLevel < 0 || decisionLevel > CurrentDecisionLevel)
            throw new ArgumentOutOfRangeException(nameof(decisionLevel));
        if (decisionLevel == CurrentDecisionLevel)
            return;

        // Trail start above target level
        var retainedTrailCount = _decisionTrailStarts[decisionLevel];
        UnassignFrom(retainedTrailCount);
        RemoveDecisionLevelsFrom(decisionLevel);
        _propagationHead = Math.Min(_propagationHead, _trail.Count);
    }

    public IReadOnlyList<Literal> CreateModel() =>
        // Arbitrary false values for unassigned vars
        Enumerable.Range(1, VariableCount)
            .Select(variable => new Literal(variable, _values[variable] is not 1))
            .ToArray();

    private void UnassignFrom(int trailCount)
    {
        while (_trail.Count > trailCount)
        {
            var literal = _trail[^1];
            _values[literal.Variable] = 0;
            _decisionLevels[literal.Variable] = 0;
            _reasons[literal.Variable] = null;
            _trail.RemoveAt(_trail.Count - 1);
        }
    }

    private void RemoveDecisionLevelsFrom(int index)
    {
        var count = _decisionTrailStarts.Count - index;
        if (count > 0)
            _decisionTrailStarts.RemoveRange(index, count);
    }

    private void EnsureAssigned(int variable)
    {
        if (variable < 1 || variable > VariableCount)
            throw new ArgumentOutOfRangeException(nameof(variable));
        if (!IsAssigned(variable))
            throw new InvalidOperationException("The variable is not assigned.");
    }
}
