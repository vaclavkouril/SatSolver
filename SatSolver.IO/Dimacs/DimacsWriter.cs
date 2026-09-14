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

        WriteComments(formula, writer);
        writer.WriteLine($"p cnf {formula.VariableCount} {formula.Clauses.Count}");

        foreach (var clause in formula.Clauses)
        {
            ArgumentNullException.ThrowIfNull(clause);
            WriteClause(clause, formula.VariableCount, writer);
        }
    }

    private static void WriteComments(CnfFormula formula, TextWriter writer)
    {
        WriteVariableSection("Original variables", CnfVariableKind.Original, formula.Variables, writer);
        WriteVariableSection("Auxiliary gate variables", CnfVariableKind.Auxiliary, formula.Variables, writer);

        if (formula.RootLiteral != 0)
            writer.WriteLine($"c Root formula literal: {formula.RootLiteral}");
    }

    private static void WriteVariableSection(
        string title,
        CnfVariableKind kind,
        IReadOnlyList<CnfVariable> variables,
        TextWriter writer)
    {
        var vars = variables
            .Where(v => v.Kind == kind)
            .OrderBy(v => v.Index)
            .ToArray();

        if (vars.Length == 0)
            return;

        writer.WriteLine($"c {title}:");
        foreach (var v in vars)
            writer.WriteLine($"c   {v.Index}: {v.Description}");
    }

    private static void WriteClause(Clause clause, int varCount, TextWriter writer)
    {
        var vals = new HashSet<int>();
        foreach (var lit in clause.Literals)
        {
            if (lit.Variable < 1 || lit.Variable > varCount)
                throw new ArgumentOutOfRangeException(nameof(clause), "Literal variable index is outside the formula's range.");

            var val = lit.IsNegated ? -lit.Variable : lit.Variable;
            if (!vals.Add(val))
                throw new ArgumentException("A clause cannot contain duplicate literals.", nameof(clause));
            if (vals.Contains(-val))
                throw new ArgumentException("A clause cannot contain opposite literals.", nameof(clause));

            writer.Write(val);
            writer.Write(' ');
        }

        writer.WriteLine('0');
    }
}
