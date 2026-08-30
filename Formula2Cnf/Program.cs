using SatSolver.Core.Encoding;
using SatSolver.IO.Dimacs;
using SatSolver.IO.Formula;

namespace Formula2Cnf;

internal static class Program
{
    public static int Main(string[] args)
    {
        var paths = new List<string>();
        var encoding = TseitinEncoding.Implications;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "-e" or "--equivalences":
                    encoding = TseitinEncoding.Equivalences;
                    break;
                case "--implications":
                    encoding = TseitinEncoding.Implications;
                    break;
                case "--encoding":
                    if (++index >= args.Length || !TryParseEncoding(args[index], out encoding))
                    {
                        Console.Error.WriteLine("Expected 'equivalences' or 'implications' after --encoding.");
                        return 2;
                    }
                    break;
                case "-h" or "--help":
                    PrintUsage(Console.Out);
                    return 0;
                case var _ when argument.StartsWith('-'):
                    Console.Error.WriteLine($"Unknown option: {argument}");
                    PrintUsage(Console.Error);
                    return 2;
                default:
                    paths.Add(argument);
                    break;
            }
        }

        if (paths.Count > 2)
        {
            Console.Error.WriteLine("Expected at most an input file and an output file.");
            PrintUsage(Console.Error);
            return 2;
        }

        try
        {
            using var input = paths.Count > 0 ? File.OpenText(paths[0]) : Console.In;
            using var output = paths.Count > 1 ? File.CreateText(paths[1]) : Console.Out;

            var formula = new FormulaReader().Read(input);
            var cnf = new TseitinEncoder().Encode(formula, encoding);
            new DimacsWriter().Write(cnf, output);
            return 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void PrintUsage(TextWriter writer) =>
        writer.WriteLine("Usage: formula2cnf [--encoding equivalences|implications] [input [output]]");

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
}
