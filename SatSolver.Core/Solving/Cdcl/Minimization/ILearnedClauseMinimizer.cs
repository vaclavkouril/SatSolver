using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Cdcl.Minimization;

public interface ILearnedClauseMinimizer
{
    LearnedClause Minimize(
        LearnedClause learnedClause,
        SolverState state,
        ClauseDatabase clauses);
}
