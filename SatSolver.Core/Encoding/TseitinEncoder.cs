using SatSolver.Core.Cnf;
using SatSolver.Core.Formulas;

namespace SatSolver.Core.Encoding;

public sealed class TseitinEncoder
{
    private readonly Dictionary<string, int> _ids = new();
    private readonly List<CnfVariable> _vars = [];
    private readonly List<Clause> _clauses = [];
    private int _nextVar;
    private TseitinEncoding _encoding;

    public CnfFormula Encode(Formula formula, TseitinEncoding encoding = TseitinEncoding.Implications)
    {
        ArgumentNullException.ThrowIfNull(formula);

        Reset(encoding);

        AddInputVars(formula);
        var root = EncodeNode(formula);
        AddClause(root);

        return new CnfFormula(_nextVar, _clauses.ToArray())
        {
            Variables = _vars.ToArray(),
            RootLiteral = root
        };
    }

    private void Reset(TseitinEncoding encoding)
    {
        _ids.Clear();
        _vars.Clear();
        _clauses.Clear();
        _nextVar = 0;
        _encoding = encoding;
    }

    private void AddInputVars(Formula formula)
    {
        switch (formula)
        {
            case Variable v:
                GetOrAddInputVar(v.Name);
                break;
            case Not not:
                GetOrAddInputVar(not.Operand.Name);
                break;
            case And and:
                AddInputVars(and.Left);
                AddInputVars(and.Right);
                break;
            case Or or:
                AddInputVars(or.Left);
                AddInputVars(or.Right);
                break;
            default:
                throw new ArgumentException("Unknown formula type.", nameof(formula));
        }
    }

    private int EncodeNode(Formula formula)
    {
        switch (formula)
        {
            case Variable v:
                return GetOrAddInputVar(v.Name);
            case Not not:
                return -GetOrAddInputVar(not.Operand.Name);
            case And and:
                return EncodeAnd(EncodeNode(and.Left), EncodeNode(and.Right));
            case Or or:
                return EncodeOr(EncodeNode(or.Left), EncodeNode(or.Right));
            default:
                throw new ArgumentException("Unknown formula type.", nameof(formula));
        }
    }

    private int EncodeAnd(int left, int right)
    {
        var outVar = AddGateVar("and gate");
        AddClause(-outVar, left);
        AddClause(-outVar, right);

        if (_encoding == TseitinEncoding.Equivalences)
            AddClause(outVar, -left, -right);

        return outVar;
    }

    private int EncodeOr(int left, int right)
    {
        var outVar = AddGateVar("or gate");
        AddClause(-outVar, left, right);

        if (_encoding == TseitinEncoding.Equivalences)
        {
            AddClause(outVar, -left);
            AddClause(outVar, -right);
        }

        return outVar;
    }

    private int GetOrAddInputVar(string name)
    {
        if (_ids.TryGetValue(name, out var id))
            return id;

        id = ++_nextVar;
        _ids.Add(name, id);
        _vars.Add(new CnfVariable(id, name, CnfVariableKind.Original));
        return id;
    }

    private int AddGateVar(string description)
    {
        var id = ++_nextVar;
        _vars.Add(new CnfVariable(id, description, CnfVariableKind.Auxiliary));
        return id;
    }

    private void AddClause(params int[] vals)
    {
        var kept = new List<int>(vals.Length);
        var seen = new HashSet<int>();

        foreach (var val in vals)
        {
            if (seen.Contains(-val))
                return;

            if (!seen.Add(val))
                continue;

            kept.Add(val);
        }

        var literals = new Literal[kept.Count];
        for (var idx = 0; idx < kept.Count; idx++)
        {
            var val = kept[idx];
            literals[idx] = new Literal(Math.Abs(val), val < 0);
        }

        _clauses.Add(new Clause(literals));
    }
}
