using SatSolver.Core.Cnf;
using SatSolver.Core.Solving.Contracts;

namespace Dpll;

internal static class SolverResultWriter
{
    public static void Write(SolverResult result, TextWriter writer)
    {
        writer.WriteLine(result.Status == SolverStatus.SAT ? "SAT" : "UNSAT");

        if (result.Status == SolverStatus.SAT)
        {
            writer.Write("v");
            foreach (var literal in result.Model.OrderBy(literal => literal.Variable))
            {
                var value = literal.IsNegated ? -literal.Variable : literal.Variable;
                writer.Write($" {value}");
            }
            writer.WriteLine(" 0");
        }

        WriteStatistics(result.Statistics, writer);
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
