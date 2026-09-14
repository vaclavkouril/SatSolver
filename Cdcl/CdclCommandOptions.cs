using System.CommandLine;
using System.Globalization;
using SatSolver.Core.Solving.Cdcl;

namespace Cdcl;

internal enum PropagationMethod { Adjacency, Watched }
internal enum HeuristicMethod { First, Random, Jw, Vsids }
internal enum ConflictAnalysisMethod { FirstUip, Decision, Multiple }
internal enum MinimizationMethod { None, Recursive, Ssr }
internal enum RestartMethod { Disabled, Geometric, Luby }

internal sealed class CdclCommandOptions
{
    public Argument<FileInfo?> Input { get; } = new("input")
    {
        Description = "DIMACS (.cnf) or simplified SMT-LIB (.sat) input file.",
        Arity = ArgumentArity.ZeroOrOne
    };

    public Option<InputFormat> Format { get; } = Choice(
        "--format", InputFormat.Auto, "Input format: auto, cnf, or sat.");
    public Option<PropagationMethod> Propagation { get; } = Choice(
        "--propagation", PropagationMethod.Watched, "Unit propagation: adjacency or watched.");
    public Option<HeuristicMethod> Heuristic { get; } = Choice(
        "--heuristic", HeuristicMethod.First, "Decision heuristic: first, random, jw, or vsids.");
    public Option<int> Seed { get; } = Value(
        "--seed", 0, "Seed used by random branching and VSIDS tie breaks.");
    public Option<ConflictAnalysisMethod> ConflictAnalysis { get; } = Choice(
        "--conflict-analysis", ConflictAnalysisMethod.FirstUip, "Conflict cut: first-uip, decision, or multiple.");
    public Option<MinimizationMethod> Minimization { get; } = Choice(
        "--minimization", MinimizationMethod.None, "Clause minimization: none, recursive, or ssr.");
    public Option<RestartMethod> Restart { get; } = Choice(
        "--restart", RestartMethod.Geometric, "Restart schedule: disabled, geometric, or luby.");
    public Option<ClauseDeletionMethod> ClauseDeletion { get; } = Choice(
        "--clause-deletion", ClauseDeletionMethod.LbdActivity, "Clause deletion: disabled, activity, lbd, or lbd-activity.");
    public Option<int> RestartLimit { get; } = Value(
        "--restart-limit", 100, "Initial conflict limit before a restart.");
    public Option<double> RestartGrowth { get; } = Value(
        "--restart-growth", 1.5, "Geometric restart growth factor.");
    public Option<int> LubyUnit { get; } = Value(
        "--luby-unit", 100, "Conflict interval represented by one Luby unit.");
    public Option<int> DeletionLimit { get; } = Value(
        "--deletion-limit", 2_000, "Initial learned-clause limit before deletion.");
    public Option<double> DeletionGrowth { get; } = Value(
        "--deletion-growth", 1.5, "Learned-clause limit growth factor.");
    public Option<int> KeepLbd { get; } = Value(
        "--keep-lbd", 2, "Do not delete clauses with LBD at or below this value.");
    public Option<double> DeletionFraction { get; } = Value(
        "--deletion-fraction", 0.5, "Fraction of eligible learned clauses to delete.");

    public void AddTo(RootCommand command)
    {
        command.Arguments.Add(Input);
        command.Options.Add(Format);
        command.Options.Add(Propagation);
        command.Options.Add(Heuristic);
        command.Options.Add(Seed);
        command.Options.Add(ConflictAnalysis);
        command.Options.Add(Minimization);
        command.Options.Add(Restart);
        command.Options.Add(ClauseDeletion);
        command.Options.Add(RestartLimit);
        command.Options.Add(RestartGrowth);
        command.Options.Add(LubyUnit);
        command.Options.Add(DeletionLimit);
        command.Options.Add(DeletionGrowth);
        command.Options.Add(KeepLbd);
        command.Options.Add(DeletionFraction);
    }

    private static Option<T> Value<T>(string name, T defaultValue, string description) => new(name)
    {
        Description = description,
        DefaultValueFactory = _ => defaultValue
    };

    private static Option<double> Value(string name, double defaultValue, string description)
    {
        var option = Value<double>(name, defaultValue, description);
        option.CustomParser = result =>
        {
            // Decimal CLI arguments use a dot regardless of the system locale.
            var text = result.Tokens.Count == 1 ? result.Tokens[0].Value : "";
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return value;

            result.AddError($"Expected a number after {name}.");
            return defaultValue;
        };
        return option;
    }

    private static Option<T> Choice<T>(string name, T defaultValue, string description) where T : struct, Enum
    {
        var option = Value(name, defaultValue, description);
        option.CustomParser = result =>
        {
            // Keep CLI spellings such as first-uip and lbd-activity.
            var text = result.Tokens.Count == 1 ? result.Tokens[0].Value.Replace("-", "") : "";
            foreach (var value in Enum.GetValues<T>())
            {
                if (value.ToString().Equals(text, StringComparison.OrdinalIgnoreCase))
                    return value;
            }

            result.AddError($"Invalid value for {name}. {description}");
            return defaultValue;
        };
        return option;
    }
}
