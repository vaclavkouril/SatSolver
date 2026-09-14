namespace SatSolver.IO.Formula.Parsing;

internal sealed class FormulaLexer(string input)
{
    private int _idx;
    private int _line = 1;
    private int _col = 1;
    private bool _hadCr;

    public FormulaToken NextToken()
    {
        SkipWhitespace();
        if (IsEnd)
            return new FormulaToken(FormulaTokenKind.EndOfInput, string.Empty, _line, _col);

        return ReadToken();
    }

    private FormulaToken ReadToken()
    {
        var line = _line;
        var col = _col;
        return Current switch
        {
            '(' => ReadPunctuation(FormulaTokenKind.LeftParenthesis, line, col),
            ')' => ReadPunctuation(FormulaTokenKind.RightParenthesis, line, col),
            _ when char.IsLetter(Current) => ReadIdentifier(line, col),
            _ => throw Error($"Unexpected character '{Current}'.")
        };
    }

    private FormulaToken ReadPunctuation(FormulaTokenKind kind, int line, int col)
    {
        var text = Current.ToString();
        MoveNext();
        return new FormulaToken(kind, text, line, col);
    }

    private FormulaToken ReadIdentifier(int line, int col)
    {
        var start = _idx;
        do
        {
            MoveNext();
        } while (!IsEnd && char.IsLetterOrDigit(Current));

        return new FormulaToken(FormulaTokenKind.Identifier, input[start.._idx], line, col);
    }

    private void SkipWhitespace()
    {
        while (!IsEnd && char.IsWhiteSpace(Current))
            MoveNext();
    }

    private void MoveNext()
    {
        var ch = Current;
        _idx++;

        if (ch == '\r')
        {
            StartNewLine();
            _hadCr = true;
            return;
        }

        if (ch == '\n')
        {
            if (!_hadCr)
                StartNewLine();

            _hadCr = false;
            return;
        }

        _col++;
        _hadCr = false;
    }

    private void StartNewLine()
    {
        _line++;
        _col = 1;
    }

    private bool IsEnd => _idx >= input.Length;

    private char Current => input[_idx];

    private FormatException Error(string message) =>
        new($"Line {_line}, column {_col}: {message}");
}
