using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Clauses;
using SatSolver.Core.Solving.Search;

namespace SatSolver.Core.Solving.Propagation;

// both watch positions; units use 0 twice
internal struct WatchPositions
{
    public WatchPositions(int first, int second)
    {
        First = first;
        Second = second;
    }

    public int First;
    public int Second;
}

internal sealed class WatchedLiteralDatabase
{
    private readonly List<WatchPositions> _watchPositions = [];
    private readonly List<ClauseReference>[] _watchLists;
    private readonly List<ClauseReference> _initialUnitClauses = [];

    public WatchedLiteralDatabase(ClauseDatabase clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);

        Clauses = clauses;
        _watchLists = CreateWatchLists(clauses.VariableCount);

        foreach (var clause in clauses.ActiveClauses)
            RegisterClause(clause.Reference);
    }

    public ClauseDatabase Clauses { get; }
    public ClauseReference? EmptyClause { get; private set; }
    public IReadOnlyList<ClauseReference> InitialUnitClauses => _initialUnitClauses;

    public void RegisterClause(ClauseReference clauseRef)
    {
        var clause = Clauses.Get(clauseRef);
        if (clause.IsDeleted)
            return;

        EnsureWatchSlot(clauseRef);

        switch (clause.Literals.Count)
        {
            case 0:
                EmptyClause ??= clauseRef;
                break;
            case 1:
                RegisterUnitClause(clauseRef, clause.Literals[0]);
                break;
            default:
                RegisterNonUnitClause(clauseRef, clause.Literals);
                break;
        }
    }

    public List<ClauseReference> GetWatchList(Literal lit) =>
        _watchLists[GetLiteralIndex(lit)];

    public WatchPositions GetWatchPositions(ClauseReference clause) =>
        _watchPositions[clause.Value];

    public void MoveWatch(ClauseReference clause, int oldPos, int newPos)
    {
        var positions = _watchPositions[clause.Value];

        if (positions.First == oldPos)
            positions.First = newPos;
        else if (positions.Second == oldPos)
            positions.Second = newPos;
        else
            throw new InvalidOperationException("The requested position is not watched.");

        _watchPositions[clause.Value] = positions;

        // old entry is removed while its list is traversed
        var replacement = Clauses.Get(clause).Literals[newPos];
        AddWatch(replacement, clause);
    }

    private void RegisterUnitClause(ClauseReference clause, Literal lit)
    {
        // unit clauses use their only literal twice
        _watchPositions[clause.Value] = new WatchPositions(0, 0);
        _initialUnitClauses.Add(clause);
        AddWatch(lit, clause);
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

    private void AddWatch(Literal lit, ClauseReference clause) =>
        GetWatchList(lit).Add(clause);

    private static List<ClauseReference>[] CreateWatchLists(int varCount)
    {
        var watchLists = new List<ClauseReference>[varCount * 2];

        for (var idx = 0; idx < watchLists.Length; idx++)
            watchLists[idx] = [];

        return watchLists;
    }

    private static int GetLiteralIndex(Literal lit)
    {
        var offset = 2 * (lit.Variable - 1);
        return lit.IsNegated ? offset + 1 : offset;
    }
}
