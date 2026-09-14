using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

/// <summary>Provides common eligibility rules for learned-clause deletion.</summary>
internal abstract class ClauseDeletionPolicy(ClauseDeletionSettings settings) : IClauseDeletionPolicy
{
    protected ClauseDeletionSettings Settings { get; } = ValidateSettings(settings);

    public IReadOnlyList<ClauseReference> SelectForDeletion(
        IEnumerable<SolverClause> clauses,
        IReadOnlySet<ClauseReference> lockedClauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        ArgumentNullException.ThrowIfNull(lockedClauses);

        var candidates = OrderCandidates(
                clauses.Where(clause => CanDelete(clause, lockedClauses)))
            .ToArray();

        var deletionCount = GetDeletionCount(candidates.Length);
        return candidates
            .Take(deletionCount)
            .Select(clause => clause.Reference)
            .ToArray();
    }

    protected abstract IOrderedEnumerable<SolverClause> OrderCandidates(
        IEnumerable<SolverClause> candidates);

    // Protected: binaries, low-LBD clauses, trail reasons
    private bool CanDelete(SolverClause clause, IReadOnlySet<ClauseReference> lockedClauses) =>
        clause.IsLearned &&
        !clause.IsDeleted &&
        clause.Literals.Count > 2 &&
        clause.Lbd > Settings.PermanentLbdLimit &&
        !lockedClauses.Contains(clause.Reference);

    private int GetDeletionCount(int candidateCount) =>
        (int)Math.Ceiling(candidateCount * Settings.DeletionFraction);

    private static ClauseDeletionSettings ValidateSettings(ClauseDeletionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        return settings;
    }
}
