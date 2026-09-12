using SatSolver.Core.Cnf;
using SatSolver.Core.Formulas;

namespace SatSolver.Core.Encoding;

public sealed class TseitinEncoder
{
    private readonly Dictionary<string, int> _variables = new();
    private readonly List<CnfVariable> _variableDescriptions = [];
    private readonly List<Clause> _clauses = [];
    private int _nextVariable;
    private TseitinEncoding _encoding;

    public CnfFormula Encode(Formula formula, TseitinEncoding encoding = TseitinEncoding.Implications)
    {
        ArgumentNullException.ThrowIfNull(formula);

        _variables.Clear();
        _variableDescriptions.Clear();
        _clauses.Clear();
        _nextVariable = 0;
        _encoding = encoding;

        AssignInputVariables(formula);
        var root = EncodeFormula(formula);
        AddClause(root);

        return new CnfFormula(_nextVariable, _clauses.ToArray())
        {
            Variables = _variableDescriptions.ToArray(),
            RootLiteral = root
        };
    }

    private void AssignInputVariables(Formula formula)
    {
        switch (formula)
        {
            case Variable variable:
                GetVariable(variable.Name);
                break;
            case Not not:
                GetVariable(not.Operand.Name);
                break;
            case And and:
                AssignInputVariables(and.Left);
                AssignInputVariables(and.Right);
                break;
            case Or or:
                AssignInputVariables(or.Left);
                AssignInputVariables(or.Right);
                break;
            default:
                throw new ArgumentException("Unknown formula type.", nameof(formula));
        }
    }

    private int EncodeFormula(Formula formula) => formula switch
    {
        Variable variable => GetVariable(variable.Name),
        Not not => -GetVariable(not.Operand.Name),
        And and => EncodeAnd(EncodeFormula(and.Left), EncodeFormula(and.Right)),
        Or or => EncodeOr(EncodeFormula(or.Left), EncodeFormula(or.Right)),
        _ => throw new ArgumentException("Unknown formula type.", nameof(formula))
    };

    private int EncodeAnd(int left, int right)
    {
        var variable = NewAuxiliaryVariable("and gate");
        AddClause(-variable, left);
        AddClause(-variable, right);
        if (_encoding == TseitinEncoding.Equivalences)
            AddClause(variable, -left, -right);

        return variable;
    }

    private int EncodeOr(int left, int right)
    {
        var variable = NewAuxiliaryVariable("or gate");
        AddClause(-variable, left, right);
        if (_encoding == TseitinEncoding.Equivalences)
        {
            AddClause(variable, -left);
            AddClause(variable, -right);
        }

        return variable;
    }

    private int GetVariable(string name)
    {
        if (_variables.TryGetValue(name, out var variable))
            return variable;

        variable = ++_nextVariable;
        _variables.Add(name, variable);
        _variableDescriptions.Add(new CnfVariable(variable, name, CnfVariableKind.Original));
        return variable;
    }

    private int NewAuxiliaryVariable(string description)
    {
        var variable = ++_nextVariable;
        _variableDescriptions.Add(new CnfVariable(variable, description, CnfVariableKind.Auxiliary));
        return variable;
    }

    private void AddClause(params int[] literals)
    {
        var distinctLiterals = new HashSet<int>();
        foreach (var literal in literals)
        {
            if (distinctLiterals.Contains(-literal))
                return;
            distinctLiterals.Add(literal);
        }

        _clauses.Add(new Clause(distinctLiterals
            .Select(literal => new Literal(Math.Abs(literal), literal < 0))
            .ToArray()));
    }
}
