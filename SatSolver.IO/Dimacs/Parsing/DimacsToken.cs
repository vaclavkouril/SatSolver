namespace SatSolver.IO.Dimacs.Parsing;

internal enum DimacsTokenKind
{
    Comment,
    Word,
    Integer,
    EndOfInput
}

internal sealed record DimacsToken(DimacsTokenKind Kind, string Text, int Line, int Column)
{
    public string DisplayName => Kind switch
    {
        DimacsTokenKind.Comment => "a comment",
        DimacsTokenKind.Word => $"'{Text}'",
        DimacsTokenKind.Integer => $"integer '{Text}'",
        DimacsTokenKind.EndOfInput => "end of input",
        _ => throw new InvalidOperationException("Unknown token kind.")
    };
}
