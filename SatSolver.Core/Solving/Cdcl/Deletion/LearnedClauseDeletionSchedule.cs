using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

/// <summary>Tracks the learned-clause cache limit between deletion rounds.</summary>
internal sealed class LearnedClauseDeletionSchedule
{
    private readonly bool _isEnabled;
    private readonly double _growthFactor;

    public LearnedClauseDeletionSchedule(ClauseDeletionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        _isEnabled = settings.Method != ClauseDeletionMethod.Disabled;
        CurrentLimit = _isEnabled ? settings.InitialLearnedClauseLimit : int.MaxValue;
        _growthFactor = settings.LimitGrowthFactor;
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

    private static int Grow(int currentLimit, double growthFactor)
    {
        if (currentLimit == int.MaxValue)
            return int.MaxValue;

        var grownLimit = Math.Ceiling(currentLimit * growthFactor);
        if (grownLimit >= int.MaxValue)
            return int.MaxValue;

        return Math.Max(currentLimit + 1, (int)grownLimit);
    }
}
