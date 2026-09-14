using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

/// <summary>Ranks eligible clauses by LBD and then by activity.</summary>
internal sealed class LbdThenActivityClauseDeletionPolicy(ClauseDeletionSettings settings) : ClauseDeletionPolicy(settings)
{
    protected override IOrderedEnumerable<SolverClause> OrderCandidates(
        IEnumerable<SolverClause> candidates) =>
        candidates.OrderByDescending(clause => clause.Lbd)
            .ThenBy(clause => clause.Activity);
}
