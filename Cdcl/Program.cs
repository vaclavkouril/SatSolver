using System.CommandLine;
using SatSolver.Core.Cnf;
using SatSolver.Core.Encoding;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Analysis;
using SatSolver.Core.Solving.Cdcl.Deletion;
using SatSolver.Core.Solving.Cdcl.Minimization;
using SatSolver.Core.Solving.Cdcl.Restarts;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Propagation;
using SatSolver.IO.Dimacs;
using SatSolver.IO.Formula;

namespace Cdcl;

internal static class Program
{
    public static int Main(string[] args)
    {
        var opts = new CdclCommandOptions();
        var command = new RootCommand("Configurable CDCL SAT solver.");
        opts.AddTo(command);
        command.SetAction(result => Run(opts, result));

        return command.Parse(args).Invoke();
    }

    private static int Run(CdclCommandOptions opts, ParseResult result)
    {
        try
        {
            var file = result.GetValue(opts.Input);
            using var input = OpenInput(file);
            var cnf = ReadFormula(input, file, result.GetValue(opts.Format));
            var solver = CreateSolver(opts, result);

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

    private static CdclSolver CreateSolver(CdclCommandOptions opts, ParseResult result)
    {
        var deletion = Name(result, opts.ClauseDeletion);

        return new CdclSolver(
            decisionHeuristic: CreateHeuristic(Name(result, opts.Heuristic), result.GetValue(opts.Seed)),
            propagator: CreatePropagator(Name(result, opts.Propagation)),
            conflictAnalyzer: CreateConflictAnalyzer(Name(result, opts.ConflictAnalysis)),
            minimizer: CreateMinimizer(Name(result, opts.Minimization)),
            restartPolicy: CreateRestartPolicy(
                Name(result, opts.Restart),
                result.GetValue(opts.RestartLimit),
                result.GetValue(opts.RestartGrowth),
                result.GetValue(opts.LubyUnit)),
            clauseDeletionPolicy: CreateClauseDeletionPolicy(
                deletion,
                result.GetValue(opts.KeepLbd),
                result.GetValue(opts.DeletionFraction)),
            deletionSchedule: new LearnedClauseDeletionSchedule(
                result.GetValue(opts.DeletionLimit),
                result.GetValue(opts.DeletionGrowth),
                isEnabled: deletion != "disabled"));
    }

    private static string Name(ParseResult result, Option<string> option) =>
        result.GetValue(option) ?? throw new InvalidOperationException($"Missing {option.Name} value.");

    private static IDecisionHeuristic CreateHeuristic(string name, int seed) =>
        name switch
        {
            "first" => new FirstUnassignedHeuristic(),
            "random" => new RandomDecisionHeuristic(seed),
            "jw" => new StaticJeroslowWangDecisionHeuristic(),
            "vsids" => new VsidsDecisionHeuristic(randomSeed: seed),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };

    private static IPropagationEngine CreatePropagator(string name) => name switch
    {
        "adjacency" => new AdjacencyListPropagator(),
        "watched" => new WatchedLiteralPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static IConflictAnalyzer CreateConflictAnalyzer(string name) => name switch
    {
        "first-uip" => new FirstUipConflictAnalyzer(),
        "decision" => new DecisionLiteralConflictAnalyzer(),
        "multiple" => new MultipleCutsConflictAnalyzer(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static ILearnedClauseMinimizer CreateMinimizer(string name) => name switch
    {
        "none" => new NoOpLearnedClauseMinimizer(),
        "recursive" => new RecursiveReasonLearnedClauseMinimizer(),
        "ssr" => new SelfSubsumingResolutionMinimizer(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static IRestartPolicy CreateRestartPolicy(string name, int limit, double growth, int unit) => name switch
    {
        "disabled" => new DisabledRestartPolicy(),
        "geometric" => new GeometricRestartPolicy(limit, growth),
        "luby" => new LubyRestartPolicy(unit),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static IClauseDeletionPolicy CreateClauseDeletionPolicy(
        string name,
        int keepLbd,
        double fraction) => name switch
    {
        "disabled" => new DisabledClauseDeletionPolicy(),
        "activity" => new ActivityClauseDeletionPolicy(keepLbd, fraction),
        "lbd" => new LbdClauseDeletionPolicy(keepLbd, fraction),
        "lbd-activity" => new LbdThenActivityClauseDeletionPolicy(keepLbd, fraction),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    private static TextReader OpenInput(FileInfo? file) =>
        file is null ? Console.In : file.OpenText();

    private static CnfFormula ReadFormula(TextReader input, FileInfo? file, InputFormat format)
    {
        if (format == InputFormat.Auto)
            format = DetectFormat(file);

        return format switch
        {
            InputFormat.Cnf => new DimacsReader().Read(input),
            InputFormat.Sat => new TseitinEncoder().Encode(new FormulaReader().Read(input)),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static InputFormat DetectFormat(FileInfo? file)
    {
        if (file is null)
            return InputFormat.Cnf;

        return file.Extension.ToLowerInvariant() switch
        {
            ".cnf" => InputFormat.Cnf,
            ".sat" => InputFormat.Sat,
            _ => throw new ArgumentException("Expected a .cnf or .sat input file, or pass --format.")
        };
    }
}
