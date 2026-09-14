using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

/// <summary>Deletes the least active eligible learned clauses first.</summary>
internal sealed class ActivityClauseDeletionPolicy(ClauseDeletionSettings settings) : ClauseDeletionPolicy(settings)
{
    protected override IOrderedEnumerable<SolverClause> OrderCandidates(
        IEnumerable<SolverClause> candidates) =>
        candidates.OrderBy(clause => clause.Activity)
            .ThenByDescending(clause => clause.Lbd);
}
