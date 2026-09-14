using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

/// <summary>Optionally removes redundant literals from a learned clause.</summary>
internal interface ILearnedClauseMinimizer
{
    LearnedClause Minimize(
        LearnedClause learnedClause,
        SolverState state,
        ClauseDatabase clauses);
}
