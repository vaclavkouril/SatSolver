using System.Collections.ObjectModel;
using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Clauses;

public sealed class SolverClause
{
    private readonly ReadOnlyCollection<Literal> _literals;

    internal SolverClause(
        ClauseReference clauseRef,
        IReadOnlyList<Literal> literals,
        bool isLearned,
        int lbd)
    {
        ArgumentNullException.ThrowIfNull(literals);
        if (lbd < 0)
            throw new ArgumentOutOfRangeException(nameof(lbd));

        Reference = clauseRef;
        _literals = Array.AsReadOnly(literals.ToArray());
        IsLearned = isLearned;
        Lbd = lbd;
    }

    public ClauseReference Reference { get; }
    public IReadOnlyList<Literal> Literals => _literals;
    public bool IsLearned { get; }
    public bool IsDeleted { get; private set; }
    public int Lbd { get; }
    public double Activity { get; private set; }

    internal void BumpActivity(double amount = 1)
    {
        if (amount <= 0 || !double.IsFinite(amount))
            throw new ArgumentOutOfRangeException(nameof(amount));

        Activity += amount;
    }

    internal void MarkDeleted()
    {
        if (!IsLearned)
            throw new InvalidOperationException("Original clauses cannot be deleted.");

        IsDeleted = true;
    }
}
