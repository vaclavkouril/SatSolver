using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace Cdcl;

internal static class SolverResultWriter
{
    public static void Write(SolverResult result, TextWriter writer)
    {
        writer.WriteLine(result.Status == SolverStatus.SAT ? "SAT" : "UNSAT");

        if (result.Status == SolverStatus.SAT)
            WriteModel(result.Model, writer);

        WriteStatistics(result.Statistics, writer);
    }

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
        writer.WriteLine($"  CPU time: {statistics.CpuTime.TotalMilliseconds:F3} ms");
        writer.WriteLine($"  Decisions: {statistics.Decisions}");
        writer.WriteLine($"  Unit propagations: {statistics.UnitPropagations}");
        writer.WriteLine($"  Propagation clause checks: {statistics.PropagationClauseChecks}");
        writer.WriteLine($"  Conflicts: {statistics.Conflicts}");
        writer.WriteLine($"  Backjumps: {statistics.Backjumps}");
        writer.WriteLine($"  Restarts: {statistics.Restarts}");
        writer.WriteLine($"  Learned clauses: {statistics.LearnedClauses}");
        writer.WriteLine($"  Deleted learned clauses: {statistics.DeletedLearnedClauses}");
        writer.WriteLine($"  Mean learned-clause length: {statistics.AverageLearnedClauseLength:F2}");
        writer.WriteLine($"  Mean learned-clause LBD: {statistics.AverageLearnedClauseLbd:F2}");
    }
}
