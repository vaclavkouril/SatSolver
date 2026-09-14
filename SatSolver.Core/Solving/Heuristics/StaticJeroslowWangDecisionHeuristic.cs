using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Heuristics;

public sealed class StaticJeroslowWangDecisionHeuristic : IDecisionHeuristic
{
    private double[] _positiveScores = [];
    private double[] _negativeScores = [];

    public void Initialize(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        _positiveScores = new double[formula.VariableCount + 1];
        _negativeScores = new double[formula.VariableCount + 1];

        foreach (var clause in formula.Clauses)
            AddClauseScores(clause);
    }

    public Literal? ChooseLiteral(ISolverStateView state)
    {
        Literal? bestLiteral = null;
        var bestScore = double.NegativeInfinity;

        for (var varId = 1; varId <= state.VariableCount; varId++)
        {
            if (state.IsAssigned(varId))
                continue;

            if (_positiveScores[varId] > bestScore)
            {
                bestLiteral = new Literal(varId, IsNegated: false);
                bestScore = _positiveScores[varId];
            }

            if (_negativeScores[varId] > bestScore)
            {
                bestLiteral = new Literal(varId, IsNegated: true);
                bestScore = _negativeScores[varId];
            }
        }

        return bestLiteral;
    }

    private void AddClauseScores(Clause clause)
    {
        // short clauses get more weight
        var weight = Math.Pow(2, -clause.Literals.Count);

        foreach (var lit in clause.Literals)
        {
            var scores = lit.IsNegated ? _negativeScores : _positiveScores;
            scores[lit.Variable] += weight;
        }
    }
}
