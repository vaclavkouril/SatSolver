using System.CommandLine;
using System.CommandLine.Parsing;
using SatSolver.Core.Cnf;
using SatSolver.Core.Encoding;
using SatSolver.Core.Solving.Cdcl;
using SatSolver.Core.Solving.Cdcl.Configuration;
using SatSolver.Core.Solving.Contracts;
using SatSolver.Core.Solving.Heuristics;
using SatSolver.Core.Solving.Propagation;
using SatSolver.IO.Dimacs;
using SatSolver.IO.Formula;

namespace Cdcl;

internal static class Program
{
    private static readonly IReadOnlyDictionary<string, InputFormat> InputFormats =
        new Dictionary<string, InputFormat>(StringComparer.OrdinalIgnoreCase)
        {
            ["cnf"] = InputFormat.Cnf,
            ["sat"] = InputFormat.Sat
        };

    private static readonly IReadOnlyDictionary<string, PropagationMethod> PropagationMethods =
        new Dictionary<string, PropagationMethod>(StringComparer.OrdinalIgnoreCase)
        {
            ["adjacency"] = PropagationMethod.AdjacencyLists,
            ["watched"] = PropagationMethod.WatchedLiterals
        };

    private static readonly IReadOnlyDictionary<string, ConflictAnalysisMethod> ConflictAnalysisMethods =
        new Dictionary<string, ConflictAnalysisMethod>(StringComparer.OrdinalIgnoreCase)
        {
            ["first-uip"] = ConflictAnalysisMethod.FirstUip,
            ["decision"] = ConflictAnalysisMethod.DecisionLiteral,
            ["multiple"] = ConflictAnalysisMethod.MultipleCuts
        };

    private static readonly IReadOnlyDictionary<string, ClauseMinimizationMethod> MinimizationMethods =
        new Dictionary<string, ClauseMinimizationMethod>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = ClauseMinimizationMethod.None,
            ["recursive"] = ClauseMinimizationMethod.RecursiveReasons,
            ["ssr"] = ClauseMinimizationMethod.SelfSubsumingResolution
        };

    private static readonly IReadOnlyDictionary<string, RestartMethod> RestartMethods =
        new Dictionary<string, RestartMethod>(StringComparer.OrdinalIgnoreCase)
        {
            ["disabled"] = RestartMethod.Disabled,
            ["geometric"] = RestartMethod.Geometric,
            ["luby"] = RestartMethod.Luby
        };

    private static readonly IReadOnlyDictionary<string, ClauseDeletionMethod> ClauseDeletionMethods =
        new Dictionary<string, ClauseDeletionMethod>(StringComparer.OrdinalIgnoreCase)
        {
            ["disabled"] = ClauseDeletionMethod.Disabled,
            ["activity"] = ClauseDeletionMethod.Activity,
            ["lbd"] = ClauseDeletionMethod.Lbd,
            ["lbd-activity"] = ClauseDeletionMethod.LbdThenActivity
        };

    public static int Main(string[] args) => CreateCommand().Parse(args).Invoke();

    private static RootCommand CreateCommand()
    {
        var defaults = CdclSolverOptions.Default;
        var inputArgument = new Argument<FileInfo?>("input")
        {
            Description = "DIMACS (.cnf) or simplified SMT-LIB (.sat) input file.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var formatOption = CreateChoiceOption(
            "--format",
            "Input format for standard input or extensionless files: cnf or sat.",
            "auto",
            InputFormats);
        var propagationOption = CreateChoiceOption(
            "--propagation",
            "Unit-propagation structure: adjacency or watched.",
            GetChoiceName(defaults.Propagation, PropagationMethods),
            PropagationMethods);
        var conflictAnalysisOption = CreateChoiceOption(
            "--conflict-analysis",
            "Conflict cut: first-uip, decision, or multiple.",
            GetChoiceName(defaults.ConflictAnalysis, ConflictAnalysisMethods),
            ConflictAnalysisMethods);
        var minimizationOption = CreateChoiceOption(
            "--minimization",
            "Learned-clause minimization: none, recursive, or ssr.",
            GetChoiceName(defaults.Minimization, MinimizationMethods),
            MinimizationMethods);
        var restartOption = CreateChoiceOption(
            "--restart",
            "Restart schedule: disabled, geometric, or luby.",
            GetChoiceName(defaults.Restart.Method, RestartMethods),
            RestartMethods);
        var deletionOption = CreateChoiceOption(
            "--clause-deletion",
            "Learned-clause deletion: disabled, activity, lbd, or lbd-activity.",
            GetChoiceName(defaults.ClauseDeletion.Method, ClauseDeletionMethods),
            ClauseDeletionMethods);
        var restartLimitOption = CreateIntOption(
            "--restart-limit",
            "Initial conflict limit before a restart.",
            defaults.Restart.InitialConflictLimit);
        var restartGrowthOption = CreateDoubleOption(
            "--restart-growth",
            "Geometric restart growth factor.",
            defaults.Restart.GrowthFactor);
        var lubyUnitOption = CreateIntOption(
            "--luby-unit",
            "Conflict interval represented by one Luby unit.",
            defaults.Restart.LubyUnitRun);
        var deletionLimitOption = CreateIntOption(
            "--deletion-limit",
            "Initial learned-clause limit before deletion.",
            defaults.ClauseDeletion.InitialLearnedClauseLimit);
        var deletionGrowthOption = CreateDoubleOption(
            "--deletion-growth",
            "Learned-clause limit growth factor.",
            defaults.ClauseDeletion.LimitGrowthFactor);
        var keepLbdOption = CreateIntOption(
            "--keep-lbd",
            "Do not delete clauses with LBD at or below this value.",
            defaults.ClauseDeletion.PermanentLbdLimit);
        var deletionFractionOption = CreateDoubleOption(
            "--deletion-fraction",
            "Fraction of eligible learned clauses to delete.",
            defaults.ClauseDeletion.DeletionFraction);

        var command = new RootCommand("Configurable CDCL SAT solver.")
        {
            inputArgument,
            formatOption,
            propagationOption,
            conflictAnalysisOption,
            minimizationOption,
            restartOption,
            deletionOption,
            restartLimitOption,
            restartGrowthOption,
            lubyUnitOption,
            deletionLimitOption,
            deletionGrowthOption,
            keepLbdOption,
            deletionFractionOption
        };

        command.SetAction(parseResult => Execute(
            parseResult.GetValue(inputArgument),
            ToInputFormat(parseResult.GetValue(formatOption)!),
            PropagationMethods[parseResult.GetValue(propagationOption)!],
            ConflictAnalysisMethods[parseResult.GetValue(conflictAnalysisOption)!],
            MinimizationMethods[parseResult.GetValue(minimizationOption)!],
            RestartMethods[parseResult.GetValue(restartOption)!],
            ClauseDeletionMethods[parseResult.GetValue(deletionOption)!],
            parseResult.GetValue(restartLimitOption),
            parseResult.GetValue(restartGrowthOption),
            parseResult.GetValue(lubyUnitOption),
            parseResult.GetValue(deletionLimitOption),
            parseResult.GetValue(deletionGrowthOption),
            parseResult.GetValue(keepLbdOption),
            parseResult.GetValue(deletionFractionOption)));

        return command;
    }

    private static int Execute(
        FileInfo? inputFile,
        InputFormat inputFormat,
        PropagationMethod propagation,
        ConflictAnalysisMethod conflictAnalysis,
        ClauseMinimizationMethod minimization,
        RestartMethod restart,
        ClauseDeletionMethod clauseDeletion,
        int restartLimit,
        double restartGrowth,
        int lubyUnit,
        int deletionLimit,
        double deletionGrowth,
        int keepLbd,
        double deletionFraction)
    {
        try
        {
            ValidatePositive(restartLimit, "--restart-limit");
            ValidateGreaterThanOne(restartGrowth, "--restart-growth");
            ValidatePositive(lubyUnit, "--luby-unit");
            ValidatePositive(deletionLimit, "--deletion-limit");
            ValidateGreaterThanOne(deletionGrowth, "--deletion-growth");
            ValidateNonNegative(keepLbd, "--keep-lbd");
            ValidateFraction(deletionFraction, "--deletion-fraction");

            var defaults = CdclSolverOptions.Default;
            var options = defaults with
            {
                Propagation = propagation,
                ConflictAnalysis = conflictAnalysis,
                Minimization = minimization,
                Restart = defaults.Restart with
                {
                    Method = restart,
                    InitialConflictLimit = restartLimit,
                    GrowthFactor = restartGrowth,
                    LubyUnitRun = lubyUnit
                },
                ClauseDeletion = defaults.ClauseDeletion with
                {
                    Method = clauseDeletion,
                    InitialLearnedClauseLimit = deletionLimit,
                    LimitGrowthFactor = deletionGrowth,
                    PermanentLbdLimit = keepLbd,
                    DeletionFraction = deletionFraction
                }
            };

            using var input = inputFile is null ? Console.In : inputFile.OpenText();
            var formula = ReadFormula(input, inputFile?.FullName, inputFormat);
            var solver = new CdclSolver(new FirstUnassignedHeuristic(), options);
            WriteResult(solver.Solve(formula), Console.Out);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                         FormatException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
    }

    private static CnfFormula ReadFormula(TextReader input, string? inputPath, InputFormat inputFormat)
    {
        var format = inputFormat == InputFormat.Auto ? GetFormatFromPath(inputPath) : inputFormat;

        return format switch
        {
            InputFormat.Cnf => new DimacsReader().Read(input),
            InputFormat.Sat => new TseitinEncoder().Encode(new FormulaReader().Read(input)),
            _ => throw new ArgumentOutOfRangeException(nameof(inputFormat))
        };
    }

    private static InputFormat GetFormatFromPath(string? inputPath) =>
        inputPath is null
            ? InputFormat.Cnf
            : Path.GetExtension(inputPath).ToLowerInvariant() switch
            {
                ".cnf" => InputFormat.Cnf,
                ".sat" => InputFormat.Sat,
                _ => throw new ArgumentException("Expected a .cnf or .sat input file, or pass --format.")
            };

    private static Option<string> CreateChoiceOption<T>(
        string name,
        string description,
        string defaultValue,
        IReadOnlyDictionary<string, T> choices) where T : struct =>
        new(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue,
            CustomParser = result => ParseChoice(result, name, choices)
        };

    private static Option<int> CreateIntOption(string name, string description, int defaultValue) =>
        new(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue
        };

    private static Option<double> CreateDoubleOption(string name, string description, double defaultValue) =>
        new(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue
        };

    private static string ParseChoice<T>(
        ArgumentResult result,
        string optionName,
        IReadOnlyDictionary<string, T> choices) where T : struct
    {
        if (result.Tokens.Count == 1 && choices.ContainsKey(result.Tokens[0].Value))
            return result.Tokens[0].Value.ToLowerInvariant();

        result.AddError($"Invalid value for {optionName}.");
        return string.Empty;
    }

    private static string GetChoiceName<T>(
        T value,
        IReadOnlyDictionary<string, T> choices) where T : struct =>
        choices.Single(choice => EqualityComparer<T>.Default.Equals(choice.Value, value)).Key;

    private static InputFormat ToInputFormat(string value) =>
        value == "auto" ? InputFormat.Auto : InputFormats[value];

    private static void ValidatePositive(int value, string optionName)
    {
        if (value < 1)
            throw new ArgumentException($"Expected a positive integer after {optionName}.");
    }

    private static void ValidateNonNegative(int value, string optionName)
    {
        if (value < 0)
            throw new ArgumentException($"Expected a non-negative integer after {optionName}.");
    }

    private static void ValidateGreaterThanOne(double value, string optionName)
    {
        if (!double.IsFinite(value) || value <= 1)
            throw new ArgumentException($"Expected a finite number greater than one after {optionName}.");
    }

    private static void ValidateFraction(double value, string optionName)
    {
        if (!double.IsFinite(value) || value is <= 0 or > 1)
            throw new ArgumentException($"Expected a finite number in (0, 1] after {optionName}.");
    }

    private static void WriteResult(SolverResult result, TextWriter writer)
    {
        writer.WriteLine(result.Status == SolverStatus.SAT ? "SAT" : "UNSAT");

        if (result.Status == SolverStatus.SAT)
        {
            writer.Write("v");
            foreach (var literal in result.Model.OrderBy(literal => literal.Variable))
                writer.Write($" {(literal.IsNegated ? -literal.Variable : literal.Variable)}");

            writer.WriteLine(" 0");
        }

        var statistics = result.Statistics;
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

    private enum InputFormat
    {
        Auto,
        Cnf,
        Sat
    }
}
