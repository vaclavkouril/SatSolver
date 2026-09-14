using System.CommandLine;
using System.CommandLine.Parsing;
using SatSolver.Core.Cnf;
using SatSolver.Core.Encoding;
using SatSolver.Core.Formulas;
using SatSolver.IO.Dimacs;
using SatSolver.IO.Formula;

namespace Formula2Cnf;

internal static class Program
{
    public static int Main(string[] args) => CreateCommand().Parse(args).Invoke();

    private static RootCommand CreateCommand()
    {
        var input = new Argument<FileInfo?>("input")
        {
            Description = "Formula input file.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var output = new Argument<FileInfo?>("output")
        {
            Description = "DIMACS output file.",
            Arity = ArgumentArity.ZeroOrOne
        };
        var encoding = new Option<TseitinEncoding>("--encoding")
        {
            Description = "Tseitin encoding: equivalences or implications.",
            HelpName = "equivalences|implications",
            DefaultValueFactory = _ => TseitinEncoding.Implications,
            CustomParser = ParseEncoding
        };
        var command = new RootCommand("Convert a simplified SMT-LIB formula to DIMACS CNF.");
        command.Arguments.Add(input);
        command.Arguments.Add(output);
        command.Options.Add(encoding);
        command.SetAction(result => ConvertFormula(
            result.GetValue(input),
            result.GetValue(output),
            result.GetValue(encoding)));

        return command;
    }

    private static TseitinEncoding ParseEncoding(ArgumentResult result)
    {
        if (result.Tokens.Count == 1 && TryParseEncoding(result.Tokens[0].Value, out var encoding))
            return encoding;

        result.AddError("Expected 'equivalences' or 'implications' after --encoding.");
        return TseitinEncoding.Implications;
    }

    private static bool TryParseEncoding(string value, out TseitinEncoding encoding)
    {
        switch (value)
        {
            case "equivalences":
                encoding = TseitinEncoding.Equivalences;
                return true;
            case "implications":
                encoding = TseitinEncoding.Implications;
                return true;
            default:
                encoding = default;
                return false;
        }
    }

    private static int ConvertFormula(FileInfo? input, FileInfo? output, TseitinEncoding encoding)
    {
        try
        {
            var formula = ReadFormula(input);
            var cnf = new TseitinEncoder().Encode(formula, encoding);
            WriteFormula(cnf, output);
            return 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static Formula ReadFormula(FileInfo? file)
    {
        if (file is null)
            return new FormulaReader().Read(Console.In);

        using var reader = file.OpenText();
        return new FormulaReader().Read(reader);
    }

    private static void WriteFormula(CnfFormula cnf, FileInfo? file)
    {
        if (file is null)
        {
            new DimacsWriter().Write(cnf, Console.Out);
            return;
        }

        using var writer = file.CreateText();
        new DimacsWriter().Write(cnf, writer);
    }
}
