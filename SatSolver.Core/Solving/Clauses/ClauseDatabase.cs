using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Clauses;

public sealed class ClauseDatabase
{
    private readonly List<SolverClause> _clauses = [];
    private Dictionary<Literal, List<SolverClause>>? _byLiteral;

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
    public int ActiveLearnedClauseCount { get; private set; }
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

    internal void DeleteLearned(ClauseReference reference)
    {
        var clause = Get(reference);
        if (clause.IsDeleted)
            return;
        clause.MarkDeleted();
        ActiveLearnedClauseCount--;
    }

    public IEnumerable<SolverClause> GetActiveClausesContaining(Literal literal)
    {
        // Only minimizers that need occurrences pay for this index.
        if (_byLiteral is null)
        {
            _byLiteral = [];
            foreach (var clause in ActiveClauses)
                IndexClause(clause);
        }

        if (!_byLiteral.TryGetValue(literal, out var occurrences))
            return [];

        // Compact on access so deleted clauses are not scanned on every lookup.
        occurrences.RemoveAll(clause => clause.IsDeleted);
        return occurrences;
    }

    private ClauseReference AddClause(IReadOnlyList<Literal> literals, bool isLearned, int lbd)
    {
        ValidateLiterals(literals);

        var clauseRef = new ClauseReference(_clauses.Count);
        var clause = new SolverClause(clauseRef, literals, isLearned, lbd);
        _clauses.Add(clause);
        if (isLearned)
            ActiveLearnedClauseCount++;
        if (_byLiteral is not null)
            IndexClause(clause);
        return clauseRef;
    }

    private void IndexClause(SolverClause clause)
    {
        foreach (var literal in clause.Literals)
        {
            if (!_byLiteral!.TryGetValue(literal, out var occurrences))
                _byLiteral[literal] = occurrences = [];
            // Repeated literals in a clause need only one occurrence entry.
            if (occurrences.Count == 0 || occurrences[^1] != clause)
                occurrences.Add(clause);
        }
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
