using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Cdcl;

/// <summary>Configurable CDCL solver.</summary>
public sealed class CdclSolver : ISolver
{
    private readonly IDecisionHeuristic _decisionHeuristic;
    private readonly CdclSolverOptions _options;

    /// <summary>Creates a configurable CDCL solver.</summary>
    /// <param name="decisionHeuristic">Branching strategy.</param>
    /// <param name="options">Algorithm configuration.</param>
    public CdclSolver(
        IDecisionHeuristic decisionHeuristic,
        CdclSolverOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(decisionHeuristic);

        _decisionHeuristic = decisionHeuristic;
        _options = options ?? CdclSolverOptions.Default;
        _options.Validate();
    }

    public SolverResult Solve(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);
        return new CdclRun(_decisionHeuristic, _options).Solve(formula);
    }
}
