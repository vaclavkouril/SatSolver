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
        var operation = Expect(FormulaTokenKind.Identifier).Text;
        FormulaNode formula = operation switch
        {
            "and" => new And(ParseFormula(), ParseFormula()),
            "or" => new Or(ParseFormula(), ParseFormula()),
            "not" => new Not(ParseVariable()),
            _ => throw Error($"Unknown operation '{operation}'.")
        };

        Expect(FormulaTokenKind.RightParenthesis);
        return formula;
    }

    private Variable ParseVariable() => new(Expect(FormulaTokenKind.Identifier).Text);

    private FormulaToken Expect(FormulaTokenKind expectedKind)
    {
        if (_current.Kind != expectedKind)
            throw Error($"Expected {Describe(expectedKind)}, found {_current.DisplayName}.");

        var token = _current;
        Advance();
        return token;
    }

    private void Advance() => _current = _lexer.NextToken();

    private FormatException Error(string message) =>
        new($"Line {_current.Line}, column {_current.Column}: {message}");

    private static string Describe(FormulaTokenKind kind) => kind switch
    {
        FormulaTokenKind.LeftParenthesis => "'('",
        FormulaTokenKind.RightParenthesis => "')'",
        FormulaTokenKind.Identifier => "an identifier",
        FormulaTokenKind.EndOfInput => "end of input",
        _ => throw new InvalidOperationException("Unknown token kind.")
    };
}
