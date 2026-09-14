namespace SatSolver.Core.Solving.Cdcl.Deletion;

public sealed class LearnedClauseDeletionSchedule
{
    private readonly bool _isEnabled;
    private readonly int _initialLearnedClauseLimit;
    private readonly double _growthFactor;

    public LearnedClauseDeletionSchedule(
        int initialLearnedClauseLimit = 2_000,
        double growthFactor = 1.5,
        bool isEnabled = true)
    {
        if (isEnabled && initialLearnedClauseLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(initialLearnedClauseLimit));
        if (isEnabled && (!double.IsFinite(growthFactor) || growthFactor <= 1))
        {
            throw new ArgumentOutOfRangeException(nameof(growthFactor));
        }

        _isEnabled = isEnabled;
        _initialLearnedClauseLimit = initialLearnedClauseLimit;
        _growthFactor = growthFactor;
        Reset();
    }

    public int CurrentLimit { get; private set; }

    public bool ShouldDelete(int learnedClauseCount)
    {
        if (learnedClauseCount < 0)
            throw new ArgumentOutOfRangeException(nameof(learnedClauseCount));

        return _isEnabled && learnedClauseCount > CurrentLimit;
    }

    public void OnDeletionRound()
    {
        if (_isEnabled)
            CurrentLimit = Grow(CurrentLimit, _growthFactor);
    }

    public void Reset() => CurrentLimit = _isEnabled ? _initialLearnedClauseLimit : int.MaxValue;

    private static int Grow(int currentLimit, double growthFactor)
    {
        if (currentLimit == int.MaxValue)
            return int.MaxValue;

        var next = Math.Ceiling(currentLimit * growthFactor);
        if (next >= int.MaxValue)
            return int.MaxValue;

        return Math.Max(currentLimit + 1, (int)next);
    }
}
