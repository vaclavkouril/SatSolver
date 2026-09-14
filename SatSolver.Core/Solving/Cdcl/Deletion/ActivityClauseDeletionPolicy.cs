using SatSolver.Core.Solving.Clauses;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

public sealed class ActivityClauseDeletionPolicy : ClauseDeletionPolicy
{
    public ActivityClauseDeletionPolicy(
        int permanentLbdLimit = 2,
        double deletionFraction = 0.5)
        : base(permanentLbdLimit, deletionFraction)
    {
    }

    protected override IOrderedEnumerable<SolverClause> OrderCandidates(
        IEnumerable<SolverClause> candidates) =>
        candidates.OrderBy(clause => clause.Activity)
            .ThenByDescending(clause => clause.Lbd);
}
