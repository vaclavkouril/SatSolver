using FormulaNode = SatSolver.Core.Formulas.Formula;
using SatSolver.IO.Formula.Parsing;

namespace SatSolver.IO.Formula;

public sealed class FormulaReader
{
    public FormulaNode Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return new FormulaParser(new FormulaLexer(reader.ReadToEnd())).Parse();
    }
}
