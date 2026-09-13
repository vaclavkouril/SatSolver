using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Contracts;

/// <summary>Chooses the next branching literal.</summary>
public interface IDecisionHeuristic
{
    void Initialize(CnfFormula formula);

    /// <summary>Returns an unassigned decision literal, or <see langword="null"/>.</summary>
    Literal? ChooseLiteral(ISolverStateView state);
}
