namespace SatSolver.IO.Formula.Parsing;

internal sealed class FormulaLexer(string input)
{
    private int _position;
    private int _line = 1;
    private int _column = 1;

    public FormulaToken NextToken()
    {
        SkipWhitespace();
        if (IsEnd)
            return new FormulaToken(FormulaTokenKind.EndOfInput, string.Empty, _line, _column);

        var line = _line;
        var column = _column;
        return CurrentCharacter switch
        {
            '(' => ReadSingleCharacterToken(FormulaTokenKind.LeftParenthesis, line, column),
            ')' => ReadSingleCharacterToken(FormulaTokenKind.RightParenthesis, line, column),
            _ when char.IsLetter(CurrentCharacter) => ReadIdentifier(line, column),
            _ => throw Error($"Unexpected character '{CurrentCharacter}'.")
        };
    }

    private FormulaToken ReadSingleCharacterToken(FormulaTokenKind kind, int line, int column)
    {
        var text = CurrentCharacter.ToString();
        MoveNext();
        return new FormulaToken(kind, text, line, column);
    }

    private FormulaToken ReadIdentifier(int line, int column)
    {
        var start = _position;
        do
        {
            MoveNext();
        } while (!IsEnd && char.IsLetterOrDigit(CurrentCharacter));

        return new FormulaToken(FormulaTokenKind.Identifier, input[start.._position], line, column);
    }

    private void SkipWhitespace()
    {
        while (!IsEnd && char.IsWhiteSpace(CurrentCharacter))
            MoveNext();
    }

    private void MoveNext()
    {
        if (CurrentCharacter == '\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }

        _position++;
    }

    private bool IsEnd => _position >= input.Length;

    private char CurrentCharacter => input[_position];

    private FormatException Error(string message) =>
        new($"Line {_line}, column {_column}: {message}");
}
