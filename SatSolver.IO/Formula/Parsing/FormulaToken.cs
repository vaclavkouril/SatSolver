namespace SatSolver.IO.Formula.Parsing;

internal enum FormulaTokenKind
{
    LeftParenthesis,
    RightParenthesis,
    Identifier,
    EndOfInput
}

internal sealed record FormulaToken(FormulaTokenKind Kind, string Text, int Line, int Column)
{
    public string DisplayName => Kind switch
    {
        FormulaTokenKind.LeftParenthesis => "'('",
        FormulaTokenKind.RightParenthesis => "')'",
        FormulaTokenKind.Identifier => $"identifier '{Text}'",
        FormulaTokenKind.EndOfInput => "end of input",
        _ => throw new InvalidOperationException("Unknown token kind.")
    };
}
