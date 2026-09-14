using System.Collections.ObjectModel;
using SatSolver.Core.Cnf;

namespace SatSolver.Core.Solving.Clauses;

public sealed class LearnedClause
{
    private readonly ReadOnlyCollection<Literal> _literals;

    public LearnedClause(IReadOnlyList<Literal> literals, int lbd)
    {
        ArgumentNullException.ThrowIfNull(literals);
        if (lbd < 0)
            throw new ArgumentOutOfRangeException(nameof(lbd));

        _literals = Array.AsReadOnly(literals.ToArray());
        Lbd = lbd;
    }

    public IReadOnlyList<Literal> Literals => _literals;
    public int Lbd { get; }
}
