using SatSolver.Core.Solving.Clauses;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

public sealed class LbdClauseDeletionPolicy : ClauseDeletionPolicy
{
    public LbdClauseDeletionPolicy(
        int permanentLbdLimit = 2,
        double deletionFraction = 0.5)
        : base(permanentLbdLimit, deletionFraction)
    {
    }

    protected override IOrderedEnumerable<SolverClause> OrderCandidates(
        IEnumerable<SolverClause> candidates) =>
        candidates.OrderByDescending(clause => clause.Lbd);
}
