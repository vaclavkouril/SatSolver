using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace Dpll;

internal static class SolverResultWriter
{
    public static void Write(SolverResult result, TextWriter writer)
    {
        WriteStatus(result.Status, writer);

        if (result.Status == SolverStatus.SAT)
            WriteModel(result.Model, writer);

        WriteStatistics(result.Statistics, writer);
    }

    private static void WriteStatus(SolverStatus status, TextWriter writer) =>
        writer.WriteLine(status == SolverStatus.SAT ? "SAT" : "UNSAT");

    private static void WriteModel(IReadOnlyList<Literal> model, TextWriter writer)
    {
        writer.Write("v");
        foreach (var literal in model.OrderBy(literal => literal.Variable))
            writer.Write($" {(literal.IsNegated ? -literal.Variable : literal.Variable)}");

        writer.WriteLine(" 0");
    }

    private static void WriteStatistics(SolverStatistics statistics, TextWriter writer)
    {
        writer.WriteLine("Statistics:");
        writer.WriteLine($"CPU time: {statistics.CpuTime.TotalMilliseconds:F3} ms");
        writer.WriteLine($"Decisions: {statistics.Decisions}");
        writer.WriteLine($"Unit propagations: {statistics.UnitPropagations}");
        writer.WriteLine($"Propagation clause checks: {statistics.PropagationClauseChecks}");
    }
}
