using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

/// <summary>Two watch offsets for one clause.</summary>
internal struct WatchPositions(int first, int second)
{
    public int First = first;
    public int Second = second;
}

/// <summary>Mutable watched-literal index.</summary>
internal sealed class WatchedLiteralDatabase
{
    private readonly List<WatchPositions> _watchPositions = [];
    private readonly List<ClauseReference>[] _clausesWatchingLiteral;
    private readonly List<ClauseReference> _initialUnitClauses = [];

    public WatchedLiteralDatabase(ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        Clauses = clauses;
        _clausesWatchingLiteral = CreateWatchLists(clauses.VariableCount);

        foreach (var clause in clauses.ActiveClauses)
            RegisterClause(clause.Reference);
    }

    public ClauseDatabase Clauses { get; }
    public ClauseReference? EmptyClause { get; private set; }
    public IReadOnlyList<ClauseReference> InitialUnitClauses => _initialUnitClauses;

    public void RegisterClause(ClauseReference reference)
    {
        var clause = Clauses.Get(reference);
        if (clause.IsDeleted)
            return;

        EnsureWatchSlot(reference);

        switch (clause.Literals.Count)
        {
            case 0:
                EmptyClause ??= reference;
                break;
            case 1:
                RegisterUnitClause(reference, clause.Literals[0]);
                break;
            default:
                RegisterNonUnitClause(reference, clause.Literals);
                break;
        }
    }

    public List<ClauseReference> GetClausesWatching(Literal literal) =>
        _clausesWatchingLiteral[GetLiteralIndex(literal)];

    public WatchPositions GetWatchPositions(ClauseReference clause) =>
        _watchPositions[clause.Value];

    public void MoveWatch(ClauseReference clause, int previousPosition, int replacementPosition)
    {
        var positions = _watchPositions[clause.Value];

        if (positions.First == previousPosition)
            positions.First = replacementPosition;
        else if (positions.Second == previousPosition)
            positions.Second = replacementPosition;
        else
            throw new InvalidOperationException("The requested position is not watched.");

        _watchPositions[clause.Value] = positions;

        // Old watch removal in caller
        var replacement = Clauses.Get(clause).Literals[replacementPosition];
        AddWatch(replacement, clause);
    }

    private void RegisterUnitClause(ClauseReference clause, Literal literal)
    {
        // One shared position for a unit clause
        _watchPositions[clause.Value] = new WatchPositions(0, 0);
        _initialUnitClauses.Add(clause);
        AddWatch(literal, clause);
    }

    private void RegisterNonUnitClause(ClauseReference clause, IReadOnlyList<Literal> literals)
    {
        _watchPositions[clause.Value] = new WatchPositions(0, 1);
        AddWatch(literals[0], clause);
        AddWatch(literals[1], clause);
    }

    private void EnsureWatchSlot(ClauseReference clause)
    {
        while (_watchPositions.Count <= clause.Value)
            _watchPositions.Add(default);
    }

    private void AddWatch(Literal literal, ClauseReference clause) =>
        GetClausesWatching(literal).Add(clause);

    private static List<ClauseReference>[] CreateWatchLists(int variableCount) =>
        Enumerable.Range(0, variableCount * 2)
            .Select(_ => new List<ClauseReference>())
            .ToArray();

    private static int GetLiteralIndex(Literal literal) =>
        2 * (literal.Variable - 1) + (literal.IsNegated ? 1 : 0);
}
