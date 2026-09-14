using System.CommandLine;
using System.CommandLine.Parsing;
using SatSolver.Core.Cnf;
using SatSolver.Core.Encoding;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Dpll;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Propagation;
using SatSolver.IO.Dimacs;
using SatSolver.IO.Formula;

namespace Dpll;

internal static class Program
{
    public static int Main(string[] args) => CreateCommand().Parse(args).Invoke();

    private static RootCommand CreateCommand()
    {
        var propagation = new Option<string>("--propagation")
        {
            Description = "Unit-propagation structure: adjacency or watched.",
            DefaultValueFactory = _ => "adjacency",
            CustomParser = ParsePropagation
        };
        var heuristic = new Option<string>("--heuristic")
        {
            Description = "Decision heuristic: first, random, or jw.",
            HelpName = "first|random|jw",
            DefaultValueFactory = _ => "first",
            CustomParser = ParseHeuristic
        };
        var seed = new Option<int>("--seed")
        {
            Description = "Seed used only by the random heuristic.",
            DefaultValueFactory = _ => 0
        };
        var input = new Argument<FileInfo?>("input")
        {
            Description = "DIMACS (.cnf) or simplified SMT-LIB (.sat) input file.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var command = new RootCommand("DPLL SAT solver.")
        {
            propagation,
            heuristic,
            seed,
            input
        };

        command.SetAction(result => Run(
            result.GetValue(propagation)
            ?? throw new InvalidOperationException("Missing --propagation value."),
            result.GetValue(heuristic)
            ?? throw new InvalidOperationException("Missing --heuristic value."),
            result.GetValue(seed),
            result.GetValue(input)));

        return command;
    }

    private static int Run(string propagation, string heuristic, int seed, FileInfo? file)
    {
        try
        {
            using var input = file is null ? Console.In : file.OpenText();
            var cnf = ReadCnf(input, file?.FullName);
            var solver = new DpllSolver(CreateHeuristic(heuristic, seed), CreatePropagator(propagation));

            SolverResultWriter.Write(solver.Solve(cnf), Console.Out);
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                         FormatException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }

    private static CnfFormula ReadCnf(TextReader input, string? inputPath) =>
        Path.GetExtension(inputPath ?? ".cnf").ToLowerInvariant() switch
        {
            ".cnf" => new DimacsReader().Read(input),
            ".sat" => new TseitinEncoder().Encode(new FormulaReader().Read(input)),
            _ => throw new ArgumentException("Expected a .cnf or .sat input file.")
        };

    private static string ParsePropagation(ArgumentResult result) =>
        ParseChoice(result, "Expected 'adjacency' or 'watched' after --propagation.", "adjacency", "watched");

    private static string ParseHeuristic(ArgumentResult result)
    {
        if (result.Tokens.Count == 1 && result.Tokens[0].Value.Equals("vsids", StringComparison.OrdinalIgnoreCase))
        {
            result.AddError("The 'vsids' heuristic is available only in cdcl.");
            return string.Empty;
        }

        return ParseChoice(result, "Expected 'first', 'random', or 'jw' after --heuristic.", "first", "random", "jw");
    }

    private static string ParseChoice(ArgumentResult result, string error, params string[] values)
    {
        if (result.Tokens.Count == 1)
        {
            var value = result.Tokens[0].Value.ToLowerInvariant();
            if (values.Contains(value))
                return value;
        }

        result.AddError(error);
        return string.Empty;
    }

    private static IDecisionHeuristic CreateHeuristic(string name, int seed) => name switch
    {
        "first" => new FirstUnassignedHeuristic(),
        "random" => new RandomDecisionHeuristic(seed),
        "jw" => new StaticJeroslowWangDecisionHeuristic(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static IPropagationEngine CreatePropagator(string name) => name switch
    {
        "adjacency" => new AdjacencyListPropagator(),
        "watched" => new WatchedLiteralPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };
}
