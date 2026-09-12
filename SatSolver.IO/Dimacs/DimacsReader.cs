using SatSolver.Core.Cnf;
using SatSolver.IO.Dimacs.Parsing;

namespace SatSolver.IO.Dimacs;

public sealed class DimacsReader
{
    public CnfFormula Read(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return new DimacsParser(new DimacsLexer(reader.ReadToEnd())).Parse();
    }
}
