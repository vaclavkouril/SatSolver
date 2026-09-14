using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace SatSolver.Core.Solving.Heuristics;

public sealed class RandomDecisionHeuristic(int seed = 0) : IDecisionHeuristic
{
    private readonly int _seed = seed;
    private Random _random = new(seed);

    public void Initialize(CnfFormula _)
    {
        _random = new Random(_seed);
    }

    public Literal? ChooseLiteral(ISolverStateView state)
    {
        var vars = GetUnassignedVariables(state);
        if (vars.Count == 0)
            return null;

        var varId = vars[_random.Next(vars.Count)];
        return new Literal(varId, IsNegated: _random.Next(2) == 0);
    }

    private static List<int> GetUnassignedVariables(ISolverStateView state)
    {
        var vars = new List<int>();

        for (var varId = 1; varId <= state.VariableCount; varId++)
        {
            if (!state.IsAssigned(varId))
                vars.Add(varId);
        }

        return vars;
    }
}
