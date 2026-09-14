using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Clauses;

public sealed class ClauseDatabase
{
    private readonly List<SolverClause> _clauses = [];

    public ClauseDatabase(CnfFormula formula)
    {
        ArgumentNullException.ThrowIfNull(formula);

        Formula = formula;
        VariableCount = formula.VariableCount;

        foreach (var clause in formula.Clauses)
            AddOriginal(clause);
    }

    public CnfFormula Formula { get; }
    public int VariableCount { get; }
    public int Count => _clauses.Count;
    public IEnumerable<SolverClause> Clauses => _clauses;
    public IEnumerable<SolverClause> ActiveClauses => _clauses.Where(clause => !clause.IsDeleted);
    public IEnumerable<SolverClause> LearnedClauses => _clauses.Where(clause => clause.IsLearned && !clause.IsDeleted);

    internal ClauseReference AddOriginal(Clause clause)
    {
        ArgumentNullException.ThrowIfNull(clause);

        return AddClause(clause.Literals, isLearned: false, lbd: 0);
    }

    internal ClauseReference AddLearned(LearnedClause clause)
    {
        ArgumentNullException.ThrowIfNull(clause);

        return AddClause(clause.Literals, isLearned: true, clause.Lbd);
    }

    public SolverClause Get(ClauseReference reference)
    {
        if ((uint)reference.Value >= (uint)_clauses.Count)
            throw new ArgumentOutOfRangeException(nameof(reference));

        return _clauses[reference.Value];
    }

    internal void DeleteLearned(ClauseReference reference) => Get(reference).MarkDeleted();

    private ClauseReference AddClause(IReadOnlyList<Literal> literals, bool isLearned, int lbd)
    {
        ValidateLiterals(literals);

        var clauseRef = new ClauseReference(_clauses.Count);
        _clauses.Add(new SolverClause(clauseRef, literals, isLearned, lbd));
        return clauseRef;
    }

    private void ValidateLiterals(IReadOnlyList<Literal> literals)
    {
        foreach (var lit in literals)
        {
            if (lit.Variable < 1 || lit.Variable > VariableCount)
                throw new ArgumentOutOfRangeException(nameof(literals), "A literal references an unknown variable.");
        }
    }
}
