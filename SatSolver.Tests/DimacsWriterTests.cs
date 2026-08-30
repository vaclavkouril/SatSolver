using SatSolver.Core.Cnf;
using SatSolver.IO.Dimacs;

namespace SatSolver.Tests;

public class DimacsWriterTests
{
    [Fact]
    public void Write_EmitsDimacsHeaderAndClauses()
    {
        var formula = new CnfFormula(
            5,
            [
                new Clause([new Literal(1, false), new Literal(5, true), new Literal(4, false)]),
                new Clause([new Literal(1, true), new Literal(5, false), new Literal(3, false), new Literal(4, false)]),
                new Clause([new Literal(3, true), new Literal(4, true)])
            ]);
        using var output = new StringWriter();

        new DimacsWriter().Write(formula, output);

        Assert.Equal(
            "p cnf 5 3" + Environment.NewLine +
            "1 -5 4 0" + Environment.NewLine +
            "-1 5 3 4 0" + Environment.NewLine +
            "-3 -4 0" + Environment.NewLine,
            output.ToString());
    }

    [Fact]
    public void Write_RejectsClausesWithDuplicateOrOppositeLiterals()
    {
        var duplicate = new CnfFormula(1, [new Clause([new Literal(1, false), new Literal(1, false)])]);
        var opposite = new CnfFormula(1, [new Clause([new Literal(1, false), new Literal(1, true)])]);

        Assert.Throws<ArgumentException>(() => new DimacsWriter().Write(duplicate, new StringWriter()));
        Assert.Throws<ArgumentException>(() => new DimacsWriter().Write(opposite, new StringWriter()));
    }
}
