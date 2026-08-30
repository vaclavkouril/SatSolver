using SatSolver.Core.Cnf;
using SatSolver.Core.Encoding;
using SatSolver.Core.Formulas;

namespace SatSolver.Tests;

public class TseitinEncoderTests
{
    [Fact]
    public void Encode_UsesOnlyForwardImplicationsByDefault()
    {
        var formula = new Or(new Variable("a"), new Variable("b"));

        var cnf = new TseitinEncoder().Encode(formula);

        Assert.Equal(3, cnf.VariableCount);
        Assert.Collection(
            cnf.Clauses,
            clause => AssertClause(clause, new Literal(3, true), new Literal(1, false), new Literal(2, false)),
            clause => AssertClause(clause, new Literal(3, false)));
    }

    [Fact]
    public void Encode_WithEquivalencesAddsReverseImplications()
    {
        var formula = new And(new Variable("a"), new Variable("b"));

        var cnf = new TseitinEncoder().Encode(formula, TseitinEncoding.Equivalences);

        Assert.Collection(
            cnf.Clauses,
            clause => AssertClause(clause, new Literal(3, true), new Literal(1, false)),
            clause => AssertClause(clause, new Literal(3, true), new Literal(2, false)),
            clause => AssertClause(clause, new Literal(3, false), new Literal(1, true), new Literal(2, true)),
            clause => AssertClause(clause, new Literal(3, false)));
    }

    private static void AssertClause(Clause clause, params Literal[] literals) =>
        Assert.Equal(literals, clause.Literals);
}
