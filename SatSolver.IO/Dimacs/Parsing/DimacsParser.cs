using System.Globalization;
using SatSolver.Core.Cnf;

namespace SatSolver.IO.Dimacs.Parsing;

internal sealed class DimacsParser
{
    private readonly DimacsLexer _lexer;
    private DimacsToken _current;

    public DimacsParser(DimacsLexer lexer)
    {
        _lexer = lexer;
        _current = _lexer.NextToken();
    }

    public CnfFormula Parse()
    {
        ExpectWord("p");
        ExpectWord("cnf");

        var varCount = ReadNonNegativeInteger("variable count");
        var clauseCount = ReadNonNegativeInteger("clause count");
        var clauses = ReadClauses(varCount, clauseCount);

        ConsumeTrailingInput();
        return new CnfFormula(varCount, clauses);
    }

    private IReadOnlyList<Clause> ReadClauses(int varCount, int clauseCount)
    {
        var clauses = new List<Clause>(clauseCount);
        for (var idx = 0; idx < clauseCount; idx++)
        {
            var clause = ReadClause(varCount);
            if (clause is not null)
                clauses.Add(clause);
        }

        return clauses;
    }

    private void ConsumeTrailingInput()
    {
        if (_current.Kind == DimacsTokenKind.EndMarker)
        {
            // SATLIB '%' terminator
            return;
        }

        Expect(DimacsTokenKind.EndOfInput);
    }

    private Clause? ReadClause(int varCount)
    {
        var lits = new List<Literal>();
        var seen = new HashSet<int>();
        var hasOpposite = false;

        while (true)
        {
            var val = ReadLiteralValue(varCount);
            if (val == 0)
            {
                if (hasOpposite)
                    return null;

                return new Clause(lits);
            }

            if (seen.Contains(-val))
            {
                hasOpposite = true;
                continue;
            }

            if (!seen.Add(val))
                continue;

            lits.Add(new Literal(Math.Abs(val), val < 0));
        }
    }

    private int ReadLiteralValue(int varCount)
    {
        var tok = ReadIntegerToken("a DIMACS literal or 0");
        var val = ParseInteger(tok);
        if (val != 0)
            ValidateLiteralValue(tok, val, varCount);

        return val;
    }

    private static void ValidateLiteralValue(DimacsToken tok, int val, int varCount)
    {
        if (val == int.MinValue || Math.Abs(val) > varCount)
            throw Error(tok, $"Literal '{val}' is outside the range -{varCount} to {varCount}.");
    }

    private int ReadNonNegativeInteger(string description)
    {
        var tok = ReadIntegerToken(description);
        var val = ParseInteger(tok);
        if (val < 0)
            throw Error(tok, $"Expected a non-negative {description}.");

        return val;
    }

    private DimacsToken ReadIntegerToken(string expectation) =>
        Expect(DimacsTokenKind.Integer, expectation);

    private static int ParseInteger(DimacsToken tok) =>
        int.Parse(tok.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    private DimacsToken Expect(DimacsTokenKind kind, string? expectation = null)
    {
        if (_current.Kind != kind)
            throw Error($"Expected {expectation ?? Describe(kind)}, found {_current.DisplayName}.");

        var tok = _current;
        Advance();
        return tok;
    }

    private void ExpectWord(string word)
    {
        if (_current.Kind != DimacsTokenKind.Word || _current.Text != word)
            throw Error($"Expected '{word}', found {_current.DisplayName}.");

        Advance();
    }

    private void Advance() => _current = _lexer.NextToken();

    private FormatException Error(string message) =>
        new($"Line {_current.Line}, column {_current.Column}: {message}");

    private static FormatException Error(DimacsToken tok, string message) =>
        new($"Line {tok.Line}, column {tok.Column}: {message}");

    private static string Describe(DimacsTokenKind kind) => kind switch
    {
        DimacsTokenKind.Integer => "an integer",
        DimacsTokenKind.EndOfInput => "end of input",
        _ => kind.ToString()
    };
}
