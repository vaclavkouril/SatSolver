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
        SkipInitialComments();
        ExpectWord("p");
        ExpectWord("cnf");

        var variableCount = ReadNonNegativeInteger("variable count");
        var clauseCount = ReadNonNegativeInteger("clause count");
        var clauses = ReadClauses(variableCount, clauseCount);

        ConsumeTrailingInput();
        return new CnfFormula(variableCount, clauses);
    }

    private void SkipInitialComments()
    {
        while (_current.Kind == DimacsTokenKind.Comment)
            Advance();
    }

    private IReadOnlyList<Clause> ReadClauses(int variableCount, int clauseCount)
    {
        var clauses = new List<Clause>(clauseCount);
        for (var index = 0; index < clauseCount; index++)
        {
            var clause = ReadClause(variableCount);
            if (clause is not null)
                clauses.Add(clause);
        }

        return clauses;
    }

    private void ConsumeTrailingInput()
    {
        while (_current.Kind == DimacsTokenKind.Comment)
            Advance();

        if (_current.Kind == DimacsTokenKind.EndMarker)
        {
            // SATLIB '%' terminator
            return;
        }

        Expect(DimacsTokenKind.EndOfInput);
    }

    private Clause? ReadClause(int variableCount)
    {
        var literals = new List<Literal>();
        var values = new HashSet<int>();
        var isTautology = false;

        while (true)
        {
            var token = ReadIntegerToken("a DIMACS literal or 0");
            var value = ParseInteger(token);
            if (value == 0)
                return isTautology ? null : new Clause(literals);

            if (value == int.MinValue || Math.Abs(value) > variableCount)
                throw Error(token, $"Literal '{value}' is outside the range -{variableCount} to {variableCount}.");
            if (values.Contains(-value))
            {
                isTautology = true;
                continue;
            }
            if (!values.Add(value))
                continue;

            literals.Add(new Literal(Math.Abs(value), value < 0));
        }
    }

    private int ReadNonNegativeInteger(string description)
    {
        var token = ReadIntegerToken(description);
        var value = ParseInteger(token);
        if (value < 0)
            throw Error(token, $"Expected a non-negative {description}.");

        return value;
    }

    private DimacsToken ReadIntegerToken(string expectation) =>
        Expect(DimacsTokenKind.Integer, expectation);

    private static int ParseInteger(DimacsToken token) =>
        int.Parse(token.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

    private DimacsToken Expect(DimacsTokenKind kind, string? expectation = null)
    {
        if (_current.Kind != kind)
            throw Error($"Expected {expectation ?? Describe(kind)}, found {_current.DisplayName}.");

        var token = _current;
        Advance();
        return token;
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

    private static FormatException Error(DimacsToken token, string message) =>
        new($"Line {token.Line}, column {token.Column}: {message}");

    private static string Describe(DimacsTokenKind kind) => kind switch
    {
        DimacsTokenKind.Integer => "an integer",
        DimacsTokenKind.EndOfInput => "end of input",
        _ => kind.ToString()
    };
}
