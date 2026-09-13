using System.CommandLine;
using System.CommandLine.Parsing;
using SatSolver.Core.Cnf;
using SatSolver.Core.Encoding;
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
        var propagationOption = new Option<string>("--propagation")
        {
            Description = "Unit-propagation structure: adjacency or watched.",
            DefaultValueFactory = _ => "adjacency",
            CustomParser = ParsePropagation
        };
        var inputArgument = new Argument<FileInfo?>("input")
        {
            Description = "DIMACS (.cnf) or simplified SMT-LIB (.sat) input file.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var command = new RootCommand("DPLL SAT solver.")
        {
            propagationOption,
            inputArgument
        };

        command.SetAction(parseResult => Execute(
            ToPropagationMethod(parseResult.GetValue(propagationOption)!),
            parseResult.GetValue(inputArgument)));

        return command;
    }

    private static int Execute(PropagationMethod propagationMethod, FileInfo? inputFile)
    {
        try
        {
            using var input = inputFile is null ? Console.In : inputFile.OpenText();
            var formula = ReadFormula(input, inputFile?.FullName);
            var result = new DpllSolver(new FirstUnassignedHeuristic(), propagationMethod).Solve(formula);

            SolverResultWriter.Write(result, Console.Out);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                         FormatException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static CnfFormula ReadFormula(TextReader input, string? inputPath) =>
        Path.GetExtension(inputPath ?? ".cnf").ToLowerInvariant() switch
        {
            ".cnf" => new DimacsReader().Read(input),
            ".sat" => new TseitinEncoder().Encode(new FormulaReader().Read(input)),
            _ => throw new ArgumentException("Expected a .cnf or .sat input file.")
        };

    private static string ParsePropagation(ArgumentResult result)
    {
        if (result.Tokens.Count == 1)
        {
            var value = result.Tokens[0].Value.ToLowerInvariant();
            switch (value)
            {
                case "adjacency":
                case "watched":
                    return value;
            }
        }

        result.AddError("Expected 'adjacency' or 'watched' after --propagation.");
        return string.Empty;
    }

    private static PropagationMethod ToPropagationMethod(string value) =>
        value == "adjacency"
            ? PropagationMethod.AdjacencyLists
            : PropagationMethod.WatchedLiterals;
}
