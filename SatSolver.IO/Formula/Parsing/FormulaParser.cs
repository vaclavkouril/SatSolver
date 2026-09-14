using SatSolver.Core.Formulas;
using FormulaNode = SatSolver.Core.Formulas.Formula;

namespace SatSolver.IO.Formula.Parsing;

internal sealed class FormulaParser
{
    private readonly FormulaLexer _lexer;
    private FormulaToken _current;

    public FormulaParser(FormulaLexer lexer)
    {
        _lexer = lexer;
        _current = _lexer.NextToken();
    }

    public FormulaNode Parse()
    {
        var formula = ParseFormula();
        Expect(FormulaTokenKind.EndOfInput);
        return formula;
    }

    private FormulaNode ParseFormula() => _current.Kind switch
    {
        FormulaTokenKind.Identifier => ParseVariable(),
        FormulaTokenKind.LeftParenthesis => ParseCompoundFormula(),
        _ => throw Error("Expected a formula.")
    };

    private FormulaNode ParseCompoundFormula()
    {
        Expect(FormulaTokenKind.LeftParenthesis);
        var op = Expect(FormulaTokenKind.Identifier);
        var node = ParseOperation(op);

        Expect(FormulaTokenKind.RightParenthesis);
        return node;
    }

    private FormulaNode ParseOperation(FormulaToken op) => op.Text switch
    {
        "and" => ParseAnd(),
        "or" => ParseOr(),
        "not" => ParseNot(),
        _ => throw Error(op, $"Unknown operation '{op.Text}'.")
    };

    private And ParseAnd()
    {
        var left = ParseFormula();
        var right = ParseFormula();
        return new And(left, right);
    }

    private Or ParseOr()
    {
        var left = ParseFormula();
        var right = ParseFormula();
        return new Or(left, right);
    }

    private Not ParseNot() => new(ParseVariable());

    private Variable ParseVariable()
    {
        var name = Expect(FormulaTokenKind.Identifier).Text;
        return new Variable(name);
    }

    private FormulaToken Expect(FormulaTokenKind kind)
    {
        if (_current.Kind != kind)
            throw Error($"Expected {Describe(kind)}, found {_current.DisplayName}.");

        var tok = _current;
        Advance();
        return tok;
    }

    private void Advance() => _current = _lexer.NextToken();

    private FormatException Error(string message) =>
        new($"Line {_current.Line}, column {_current.Column}: {message}");

    private static FormatException Error(FormulaToken tok, string message) =>
        new($"Line {tok.Line}, column {tok.Column}: {message}");

    private static string Describe(FormulaTokenKind kind) => kind switch
    {
        FormulaTokenKind.LeftParenthesis => "'('",
        FormulaTokenKind.RightParenthesis => "')'",
        FormulaTokenKind.Identifier => "an identifier",
        FormulaTokenKind.EndOfInput => "end of input",
        _ => throw new InvalidOperationException("Unknown token kind.")
    };
}
