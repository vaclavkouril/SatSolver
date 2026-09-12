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
        var matchingVariables = variables
            .Where(variable => variable.Kind == kind)
            .OrderBy(variable => variable.Index)
            .ToArray();

        if (matchingVariables.Length == 0)
            return;

        writer.WriteLine($"c {title}:");
        foreach (var variable in matchingVariables)
            writer.WriteLine($"c   {variable.Index}: {variable.Description}");
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
