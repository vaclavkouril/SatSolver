namespace SatSolver.IO.Dimacs.Parsing;

internal enum DimacsTokenKind
{
    Word,
    Integer,
    EndMarker,
    EndOfInput
}

internal sealed record DimacsToken(DimacsTokenKind Kind, string Text, int Line, int Column)
{
    public string DisplayName => Kind switch
    {
        DimacsTokenKind.Word => $"'{Text}'",
        DimacsTokenKind.Integer => $"integer '{Text}'",
        DimacsTokenKind.EndMarker => "end marker '%'",
        DimacsTokenKind.EndOfInput => "end of input",
        _ => throw new InvalidOperationException("Unknown token kind.")
    };
}
