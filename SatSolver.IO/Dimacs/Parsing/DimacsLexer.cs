using System.Globalization;

namespace SatSolver.IO.Dimacs.Parsing;

internal sealed class DimacsLexer(string input)
{
    private int _idx;
    private int _line = 1;
    private int _col = 1;
    private bool _atLineStart = true;
    private bool _hadCr;

    public DimacsToken NextToken()
    {
        SkipTrivia();
        if (IsEnd)
            return new DimacsToken(DimacsTokenKind.EndOfInput, string.Empty, _line, _col);

        var line = _line;
        var col = _col;
        if (Current == '%')
            return ReadEndMarker(line, col);

        var text = ReadNonWhitespaceText();
        var kind = IsInteger(text) ? DimacsTokenKind.Integer : DimacsTokenKind.Word;
        return new DimacsToken(kind, text, line, col);
    }

    private void SkipTrivia()
    {
        while (true)
        {
            SkipWhitespace();
            if (IsEnd || !_atLineStart || Current != 'c')
                return;

            SkipComment();
        }
    }

    private void SkipComment()
    {
        while (!IsEnd && Current is not '\r' and not '\n')
            MoveNext();
    }

    private DimacsToken ReadEndMarker(int line, int col)
    {
        MoveNext();
        return new DimacsToken(DimacsTokenKind.EndMarker, "%", line, col);
    }

    private string ReadNonWhitespaceText()
    {
        var start = _idx;
        while (!IsEnd && !char.IsWhiteSpace(Current))
            MoveNext();

        return input[start.._idx];
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
            BeginNewLine();
            _hadCr = true;
            return;
        }
        if (ch == '\n')
        {
            if (!_hadCr)
                BeginNewLine();

            _hadCr = false;
            return;
        }

        _col++;
        _atLineStart = false;
        _hadCr = false;
    }

    private void BeginNewLine()
    {
        _line++;
        _col = 1;
        _atLineStart = true;
    }

    private static bool IsInteger(string text) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);

    private bool IsEnd => _idx >= input.Length;
    private char Current => input[_idx];
}
