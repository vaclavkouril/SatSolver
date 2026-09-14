using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

public interface IPropagationEngine
{
    // Reset per-formula indexes before each solve.
    void Initialize(ClauseDatabase clauses);

    void RegisterClause(ClauseReference clause);

    PropagationResult Propagate(SolverState state, SearchStatistics statistics);
}
