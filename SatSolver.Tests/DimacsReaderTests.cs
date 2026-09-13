using SatSolver.Core.Cnf;
using SatSolver.IO.Dimacs;

namespace SatSolver.Tests;

public class DimacsReaderTests
{
    [Fact]
    public void Read_ParsesCommentsAndClausesAcrossLineBreaks()
    {
        const string input = """
            c Formula2Cnf variable mapping
            c   1: a
            p cnf 5 3
            1 -5
            4 0 -1 5 3 4 0
            -3 -4 0
            """;

        var formula = new DimacsReader().Read(new StringReader(input));

        Assert.Equal(5, formula.VariableCount);
        Assert.Collection(
            formula.Clauses,
            clause => AssertClause(clause, new Literal(1, false), new Literal(5, true), new Literal(4, false)),
            clause => AssertClause(clause, new Literal(1, true), new Literal(5, false), new Literal(3, false), new Literal(4, false)),
            clause => AssertClause(clause, new Literal(3, true), new Literal(4, true)));
    }

    [Theory]
    [InlineData("p cnf 2 1\n3 0")]
    [InlineData("p cnf 2 2\n1 0")]
    [InlineData("p cnf 2 1\n1 0 2 0")]
    public void Read_RejectsInvalidDimacs(string input)
    {
        Assert.Throws<FormatException>(() => new DimacsReader().Read(new StringReader(input)));
    }

    [Fact]
    public void Read_NormalizesRepeatedAndTautologicalClauses()
    {
        const string input = "p cnf 3 3\n1 1 0\n2 -2 3 0\n-3 0";

        var formula = new DimacsReader().Read(new StringReader(input));

        Assert.Collection(
            formula.Clauses,
            clause => AssertClause(clause, new Literal(1, false)),
            clause => AssertClause(clause, new Literal(3, true)));
    }

    [Fact]
    public void Read_ReportsTheInvalidLiteralPosition()
    {
        const string input = "p cnf 2 1\n1\n3 0";

        var exception = Assert.Throws<FormatException>(() => new DimacsReader().Read(new StringReader(input)));

        Assert.Equal("Line 3, column 1: Literal '3' is outside the range -2 to 2.", exception.Message);
    }

    [Fact]
    public void Read_ReportsTheInvalidLiteralPositionAfterCarriageReturns()
    {
        const string input = "p cnf 2 1\r1\r3 0";

        var exception = Assert.Throws<FormatException>(() => new DimacsReader().Read(new StringReader(input)));

        Assert.Equal("Line 3, column 1: Literal '3' is outside the range -2 to 2.", exception.Message);
    }

    [Fact]
    public void Read_SatlibEndMarkerWithTrailingZero_StopsReadingTheInstance()
    {
        const string input = """
            p cnf 2 1
            1 -2 0
            c end of input
            %
            0
            """;

        var formula = new DimacsReader().Read(new StringReader(input));

        Assert.Equal(2, formula.VariableCount);
        Assert.Collection(
            formula.Clauses,
            clause => AssertClause(clause, new Literal(1, false), new Literal(2, true)));
    }

    private static void AssertClause(Clause clause, params Literal[] literals) =>
        Assert.Equal(literals, clause.Literals);
}
