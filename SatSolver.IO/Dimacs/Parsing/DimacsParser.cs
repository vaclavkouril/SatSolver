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

        Expect(DimacsTokenKind.EndOfInput);
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
            clauses.Add(ReadClause(variableCount));

        return clauses;
    }

    private Clause ReadClause(int variableCount)
    {
        var literals = new List<Literal>();
        var values = new HashSet<int>();

        while (true)
        {
            var value = ReadInteger("a DIMACS literal or 0");
            if (value == 0)
                return new Clause(literals);

            if (value == int.MinValue || Math.Abs(value) > variableCount)
                throw Error($"Literal '{value}' is outside the range -{variableCount} to {variableCount}.");
            if (!values.Add(value))
                throw Error($"Clause contains duplicate literal '{value}'.");
            if (values.Contains(-value))
                throw Error($"Clause contains opposite literals '{value}' and '{-value}'.");

            literals.Add(new Literal(Math.Abs(value), value < 0));
        }
    }

    private int ReadNonNegativeInteger(string description)
    {
        var value = ReadInteger(description);
        if (value < 0)
            throw Error($"Expected a non-negative {description}.");

        return value;
    }

    private int ReadInteger(string expectation)
    {
        var token = Expect(DimacsTokenKind.Integer, expectation);
        return int.Parse(token.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    private void ExpectWord(string word)
    {
        if (_current.Kind != DimacsTokenKind.Word || _current.Text != word)
            throw Error($"Expected '{word}', found {_current.DisplayName}.");

        Advance();
    }

    private DimacsToken Expect(DimacsTokenKind kind, string? expectation = null)
    {
        if (_current.Kind != kind)
            throw Error($"Expected {expectation ?? Describe(kind)}, found {_current.DisplayName}.");

        var token = _current;
        Advance();
        return token;
    }

    private void Advance() => _current = _lexer.NextToken();

    private FormatException Error(string message) =>
        new($"Line {_current.Line}, column {_current.Column}: {message}");

    private static string Describe(DimacsTokenKind kind) => kind switch
    {
        DimacsTokenKind.Integer => "an integer",
        DimacsTokenKind.EndOfInput => "end of input",
        _ => kind.ToString()
    };
}
