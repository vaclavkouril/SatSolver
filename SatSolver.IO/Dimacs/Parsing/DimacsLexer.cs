using System.Globalization;

namespace SatSolver.IO.Dimacs.Parsing;

internal sealed class DimacsLexer(string input)
{
    private int _position;
    private int _line = 1;
    private int _column = 1;
    private bool _isAtLineStart = true;

    public DimacsToken NextToken()
    {
        SkipWhitespace();
        if (IsEnd)
            return new DimacsToken(DimacsTokenKind.EndOfInput, string.Empty, _line, _column);

        var line = _line;
        var column = _column;
        if (_isAtLineStart && CurrentCharacter == 'c')
            return ReadComment(line, column);

        var text = ReadNonWhitespaceText();
        var kind = IsInteger(text) ? DimacsTokenKind.Integer : DimacsTokenKind.Word;
        return new DimacsToken(kind, text, line, column);
    }

    private DimacsToken ReadComment(int line, int column)
    {
        var start = _position;
        while (!IsEnd && CurrentCharacter is not '\r' and not '\n')
            MoveNext();

        return new DimacsToken(DimacsTokenKind.Comment, input[start.._position], line, column);
    }

    private string ReadNonWhitespaceText()
    {
        var start = _position;
        while (!IsEnd && !char.IsWhiteSpace(CurrentCharacter))
            MoveNext();

        return input[start.._position];
    }

    private void SkipWhitespace()
    {
        while (!IsEnd && char.IsWhiteSpace(CurrentCharacter))
            MoveNext();
    }

    private void MoveNext()
    {
        var character = CurrentCharacter;
        _position++;
        if (character == '\r')
            return;
        if (character == '\n')
        {
            _line++;
            _column = 1;
            _isAtLineStart = true;
            return;
        }

        _column++;
        _isAtLineStart = false;
    }

    private static bool IsInteger(string text) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);

    private bool IsEnd => _position >= input.Length;
    private char CurrentCharacter => input[_position];
}
