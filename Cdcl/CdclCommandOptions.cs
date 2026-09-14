using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;

namespace Cdcl;

internal sealed class CdclCommandOptions
{
    public CdclCommandOptions()
    {
        Format = new Option<InputFormat>("--format")
        {
            Description = "Input format for standard input or extensionless files: auto, cnf, or sat.",
            HelpName = "auto|cnf|sat",
            DefaultValueFactory = _ => InputFormat.Auto,
            CustomParser = ParseFormat
        };
        Propagation = Choice(
            "--propagation",
            "Unit-propagation structure: adjacency or watched.",
            "adjacency|watched",
            "watched",
            "Expected 'adjacency' or 'watched' after --propagation.",
            "adjacency", "watched");
        Heuristic = Choice(
            "--heuristic",
            "Decision heuristic: first, random, jw, or vsids.",
            "first|random|jw|vsids",
            "first",
            "Expected 'first', 'random', 'jw', or 'vsids' after --heuristic.",
            "first", "random", "jw", "vsids");
        Seed = new Option<int>("--seed")
        {
            Description = "Seed used by random branching and VSIDS tie breaks.",
            DefaultValueFactory = _ => 0
        };
        ConflictAnalysis = Choice(
            "--conflict-analysis",
            "Conflict cut: first-uip, decision, or multiple.",
            "first-uip|decision|multiple",
            "first-uip",
            "Expected 'first-uip', 'decision', or 'multiple' after --conflict-analysis.",
            "first-uip", "decision", "multiple");
        Minimization = Choice(
            "--minimization",
            "Learned-clause minimization: none, recursive, or ssr.",
            "none|recursive|ssr",
            "none",
            "Expected 'none', 'recursive', or 'ssr' after --minimization.",
            "none", "recursive", "ssr");
        Restart = Choice(
            "--restart",
            "Restart schedule: disabled, geometric, or luby.",
            "disabled|geometric|luby",
            "geometric",
            "Expected 'disabled', 'geometric', or 'luby' after --restart.",
            "disabled", "geometric", "luby");
        ClauseDeletion = Choice(
            "--clause-deletion",
            "Learned-clause deletion: disabled, activity, lbd, or lbd-activity.",
            "disabled|activity|lbd|lbd-activity",
            "lbd-activity",
            "Expected 'disabled', 'activity', 'lbd', or 'lbd-activity' after --clause-deletion.",
            "disabled", "activity", "lbd", "lbd-activity");
        RestartLimit = PositiveInt("--restart-limit", "Initial conflict limit before a restart.", 100);
        RestartGrowth = Growth("--restart-growth", "Geometric restart growth factor.", 1.5);
        LubyUnit = PositiveInt("--luby-unit", "Conflict interval represented by one Luby unit.", 100);
        DeletionLimit = PositiveInt("--deletion-limit", "Initial learned-clause limit before deletion.", 2_000);
        DeletionGrowth = Growth("--deletion-growth", "Learned-clause limit growth factor.", 1.5);
        KeepLbd = NonNegativeInt("--keep-lbd", "Do not delete clauses with LBD at or below this value.", 2);
        DeletionFraction = Fraction("--deletion-fraction", "Fraction of eligible learned clauses to delete.", 0.5);
    }

    public Argument<FileInfo?> Input { get; } = new("input")
    {
        Description = "DIMACS (.cnf) or simplified SMT-LIB (.sat) input file.",
        Arity = ArgumentArity.ZeroOrOne
    };

    public Option<InputFormat> Format { get; }

    public Option<string> Propagation { get; }

    public Option<string> Heuristic { get; }

    public Option<int> Seed { get; }

    public Option<string> ConflictAnalysis { get; }

    public Option<string> Minimization { get; }

    public Option<string> Restart { get; }

    public Option<string> ClauseDeletion { get; }

    public Option<int> RestartLimit { get; }

    public Option<double> RestartGrowth { get; }

    public Option<int> LubyUnit { get; }

    public Option<int> DeletionLimit { get; }

    public Option<double> DeletionGrowth { get; }

    public Option<int> KeepLbd { get; }

    public Option<double> DeletionFraction { get; }

    public void AddTo(RootCommand cmd)
    {
        cmd.Arguments.Add(Input);
        cmd.Options.Add(Format);
        cmd.Options.Add(Propagation);
        cmd.Options.Add(Heuristic);
        cmd.Options.Add(Seed);
        cmd.Options.Add(ConflictAnalysis);
        cmd.Options.Add(Minimization);
        cmd.Options.Add(Restart);
        cmd.Options.Add(ClauseDeletion);
        cmd.Options.Add(RestartLimit);
        cmd.Options.Add(RestartGrowth);
        cmd.Options.Add(LubyUnit);
        cmd.Options.Add(DeletionLimit);
        cmd.Options.Add(DeletionGrowth);
        cmd.Options.Add(KeepLbd);
        cmd.Options.Add(DeletionFraction);
    }

    private static Option<string> Choice(
        string name,
        string description,
        string helpName,
        string defaultValue,
        string error,
        params string[] values) =>
        new(name)
        {
            Description = description,
            HelpName = helpName,
            DefaultValueFactory = _ => defaultValue,
            CustomParser = result => ParseChoice(result, error, values)
        };

    private static Option<int> PositiveInt(string name, string description, int defaultValue) =>
        IntOption(name, description, defaultValue, value => value > 0, "a positive integer");

    private static Option<int> NonNegativeInt(string name, string description, int defaultValue) =>
        IntOption(name, description, defaultValue, value => value >= 0, "a non-negative integer");

    private static Option<int> IntOption(
        string name,
        string description,
        int defaultValue,
        Func<int, bool> isValid,
        string expected) =>
        new(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue,
            CustomParser = result => ParseInt(result, name, isValid, expected)
        };

    private static Option<double> Growth(string name, string description, double defaultValue) =>
        DoubleOption(name, description, defaultValue, value => double.IsFinite(value) && value > 1, "a finite number greater than one");

    private static Option<double> Fraction(string name, string description, double defaultValue) =>
        DoubleOption(name, description, defaultValue, value => double.IsFinite(value) && value is > 0 and <= 1, "a finite number in (0, 1]");

    private static Option<double> DoubleOption(
        string name,
        string description,
        double defaultValue,
        Func<double, bool> isValid,
        string expected) =>
        new(name)
        {
            Description = description,
            DefaultValueFactory = _ => defaultValue,
            CustomParser = result => ParseDouble(result, name, isValid, expected)
        };

    private static InputFormat ParseFormat(ArgumentResult result)
    {
        if (result.Tokens.Count == 1)
        {
            switch (result.Tokens[0].Value.ToLowerInvariant())
            {
                case "auto":
                    return InputFormat.Auto;
                case "cnf":
                    return InputFormat.Cnf;
                case "sat":
                    return InputFormat.Sat;
            }
        }

        result.AddError("Expected 'auto', 'cnf', or 'sat' after --format.");
        return InputFormat.Auto;
    }

    private static string ParseChoice(ArgumentResult result, string error, string[] values)
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

    private static int ParseInt(ArgumentResult result, string name, Func<int, bool> isValid, string expected)
    {
        if (result.Tokens.Count == 1 &&
            int.TryParse(result.Tokens[0].Value, out var value) &&
            isValid(value))
        {
            return value;
        }

        result.AddError($"Expected {expected} after {name}.");
        return 0;
    }

    private static double ParseDouble(ArgumentResult result, string name, Func<double, bool> isValid, string expected)
    {
        if (result.Tokens.Count == 1 &&
            double.TryParse(result.Tokens[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
            isValid(value))
        {
            return value;
        }

        result.AddError($"Expected {expected} after {name}.");
        return 0;
    }
}
