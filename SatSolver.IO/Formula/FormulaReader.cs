using FormulaNode = SatSolver.Core.Formulas.Formula;
using SatSolver.IO.Formula.Parsing;

namespace SatSolver.IO.Formula;

/// <summary>Reads one NNF formula from the simplified SMT-LIB input format.</summary>
public sealed class FormulaReader
{
    public FormulaNode Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return new FormulaParser(new FormulaLexer(reader.ReadToEnd())).Parse();
    }
}
