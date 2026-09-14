using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Contracts;

public interface IConflictAwareDecisionHeuristic : IDecisionHeuristic
{
    void OnLearnedClause(IReadOnlyList<Literal> clause);

    void OnConflict();
}
