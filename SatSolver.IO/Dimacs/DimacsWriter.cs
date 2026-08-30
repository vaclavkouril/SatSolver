using SatSolver.Core.Cnf;

namespace SatSolver.IO.Dimacs;

public class DimacsWriter
{
    public void Write(CnfFormula formula, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(formula);
        ArgumentNullException.ThrowIfNull(writer);

        if (formula.VariableCount < 0)
            throw new ArgumentOutOfRangeException(nameof(formula), "Variable count cannot be negative.");

        WriteHeader(formula.VariableCount, formula.Clauses.Count, writer);
        
        foreach (var clause in formula.Clauses)
        {
            ArgumentNullException.ThrowIfNull(clause);
            WriteClause(clause, formula.VariableCount, writer);
        }
    }

    private static void WriteHeader(int variableCount, int clauseCount, TextWriter writer)
    {
        writer.WriteLine($"p cnf {variableCount} {clauseCount}");
    }

    private static void WriteClause(Clause clause, int variableCount, TextWriter writer)
    {
        var literals = new HashSet<int>();
        foreach (var literal in clause.Literals)
        {
            if (literal.Variable < 1 || literal.Variable > variableCount)
                throw new ArgumentOutOfRangeException(nameof(clause), "Literal variable index is outside the formula's range.");

            var value = literal.IsNegated ? -literal.Variable : literal.Variable;
            if (!literals.Add(value))
                throw new ArgumentException("A clause cannot contain duplicate literals.", nameof(clause));
            if (literals.Contains(-value))
                throw new ArgumentException("A clause cannot contain opposite literals.", nameof(clause));

            writer.Write(value);
            writer.Write(' ');
        }

        writer.WriteLine('0');
    }
}
