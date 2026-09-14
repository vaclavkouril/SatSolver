using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

public abstract class ClauseDeletionPolicy : IClauseDeletionPolicy
{
    private readonly int _permanentLbdLimit;
    private readonly double _deletionFraction;

    protected ClauseDeletionPolicy(
        int permanentLbdLimit = 2,
        double deletionFraction = 0.5)
    {
        if (permanentLbdLimit < 0)
            throw new ArgumentOutOfRangeException(nameof(permanentLbdLimit));
        if (!double.IsFinite(deletionFraction) || deletionFraction is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deletionFraction));
        }

        _permanentLbdLimit = permanentLbdLimit;
        _deletionFraction = deletionFraction;
    }

    public IReadOnlyList<ClauseReference> SelectForDeletion(
        IEnumerable<SolverClause> clauses,
        IReadOnlySet<ClauseReference> lockedClauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        ArgumentNullException.ThrowIfNull(lockedClauses);

        var candidates = clauses
            .Where(clause => CanDelete(clause, lockedClauses))
            .ToArray();

        var count = GetDeletionCount(candidates.Length);
        return OrderCandidates(candidates)
            .Take(count)
            .Select(clause => clause.Reference)
            .ToArray();
    }

    protected abstract IOrderedEnumerable<SolverClause> OrderCandidates(
        IEnumerable<SolverClause> candidates);

    // keep binaries, low-LBD clauses and reason clauses
    private bool CanDelete(SolverClause clause, IReadOnlySet<ClauseReference> lockedClauses) =>
        clause.IsLearned &&
        !clause.IsDeleted &&
        clause.Literals.Count > 2 &&
        clause.Lbd > _permanentLbdLimit &&
        !lockedClauses.Contains(clause.Reference);

    private int GetDeletionCount(int count) =>
        (int)Math.Ceiling(count * _deletionFraction);
}
