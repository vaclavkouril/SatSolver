using System.CommandLine;
using SatSolver.Core.Cnf;
using SatSolver.Core.Encoding;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Analysis;
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
            var solver = CreateSolver(opts, result);
            var file = result.GetValue(opts.Input);
            using var input = OpenInput(file);
            var cnf = ReadFormula(input, file, result.GetValue(opts.Format));

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
        return new CdclSolver(
            decisionHeuristic: CreateHeuristic(result.GetValue(opts.Heuristic), result.GetValue(opts.Seed)),
            propagator: CreatePropagator(result.GetValue(opts.Propagation)),
            conflictAnalyzer: CreateConflictAnalyzer(result.GetValue(opts.ConflictAnalysis)),
            minimizer: CreateMinimizer(result.GetValue(opts.Minimization)),
            restartPolicy: CreateRestartPolicy(
                result.GetValue(opts.Restart),
                result.GetValue(opts.RestartLimit),
                result.GetValue(opts.RestartGrowth),
                result.GetValue(opts.LubyUnit)),
            clauseDeletion: result.GetValue(opts.ClauseDeletion),
            deletionLimit: result.GetValue(opts.DeletionLimit),
            deletionGrowth: result.GetValue(opts.DeletionGrowth),
            keepLbd: result.GetValue(opts.KeepLbd),
            deletionFraction: result.GetValue(opts.DeletionFraction));
    }

    private static IDecisionHeuristic CreateHeuristic(HeuristicMethod method, int seed) =>
        method switch
        {
            HeuristicMethod.First => new FirstUnassignedHeuristic(),
            HeuristicMethod.Random => new RandomDecisionHeuristic(seed),
            HeuristicMethod.Jw => new StaticJeroslowWangDecisionHeuristic(),
            HeuristicMethod.Vsids => new VsidsDecisionHeuristic(randomSeed: seed),
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };

    private static IPropagationEngine CreatePropagator(PropagationMethod method) => method switch
    {
        PropagationMethod.Adjacency => new AdjacencyListPropagator(),
        PropagationMethod.Watched => new WatchedLiteralPropagator(),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static IConflictAnalyzer CreateConflictAnalyzer(ConflictAnalysisMethod method) => method switch
    {
        ConflictAnalysisMethod.FirstUip => new FirstUipConflictAnalyzer(),
        ConflictAnalysisMethod.Decision => new DecisionLiteralConflictAnalyzer(),
        ConflictAnalysisMethod.Multiple => new MultipleCutsConflictAnalyzer(),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static ILearnedClauseMinimizer CreateMinimizer(MinimizationMethod method) => method switch
    {
        MinimizationMethod.None => new NoOpLearnedClauseMinimizer(),
        MinimizationMethod.Recursive => new RecursiveReasonLearnedClauseMinimizer(),
        MinimizationMethod.Ssr => new SelfSubsumingResolutionMinimizer(),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static IRestartPolicy CreateRestartPolicy(RestartMethod method, int limit, double growth, int unit) => method switch
    {
        RestartMethod.Disabled => new DisabledRestartPolicy(),
        RestartMethod.Geometric => new GeometricRestartPolicy(limit, growth),
        RestartMethod.Luby => new LubyRestartPolicy(unit),
        _ => throw new ArgumentOutOfRangeException(nameof(method))
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
