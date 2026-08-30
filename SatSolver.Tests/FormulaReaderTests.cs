using SatSolver.Core.Formulas;
using SatSolver.IO.Formula;

namespace SatSolver.Tests;

public class FormulaReaderTests
{
    [Fact]
    public void Read_ParsesNestedFormulaWithWhitespace()
    {
        const string input = "\n  (and\talpha1\n(or (not beta2) gamma3))  ";

        var formula = new FormulaReader().Read(new StringReader(input));

        Assert.Equal(
            new And(
                new Variable("alpha1"),
                new Or(new Not(new Variable("beta2")), new Variable("gamma3"))),
            formula);
    }

    [Theory]
    [InlineData("(not (or a b))")]
    [InlineData("(and a)")]
    [InlineData("(or a b) trailing")]
    [InlineData("1variable")]
    public void Read_RejectsInvalidInput(string input)
    {
        var reader = new FormulaReader();

        Assert.Throws<FormatException>(() => reader.Read(new StringReader(input)));
    }
}
