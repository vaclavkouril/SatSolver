using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Cdcl.Configuration;

namespace SatSolver.Core.Solving.Cdcl.Deletion;

/// <summary>Deletes eligible clauses with the highest LBD first.</summary>
internal sealed class LbdClauseDeletionPolicy(ClauseDeletionSettings settings) : ClauseDeletionPolicy(settings)
{
    protected override IOrderedEnumerable<SolverClause> OrderCandidates(
        IEnumerable<SolverClause> candidates) =>
        candidates.OrderByDescending(clause => clause.Lbd);
}
