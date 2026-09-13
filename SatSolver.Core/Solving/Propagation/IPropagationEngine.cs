using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

/// <summary>Per-run propagation structure.</summary>
internal interface IPropagationEngine
{
    /// <summary>Initializes the clause index.</summary>
    void Initialize(ClauseDatabase clauses);

    /// <summary>Registers a learned clause.</summary>
    void RegisterClause(ClauseReference clause);

    /// <summary>Propagates pending trail assignments.</summary>
    PropagationResult Propagate(SolverState state, SearchStatistics statistics);
}
