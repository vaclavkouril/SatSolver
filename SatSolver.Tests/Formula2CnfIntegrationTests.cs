using System.Diagnostics;
using SatSolver.IO.Dimacs;

namespace SatSolver.Tests;

public sealed class Formula2CnfIntegrationTests
{
    [Theory]
    [MemberData(nameof(TaskOneFormulaNames))]
    public async Task Main_ConvertsTaskOneFileFromTheWorkingDirectory(string formulaName)
    {
        var repositoryRoot = FindRepositoryRoot();
        var taskDirectory = Path.Combine(repositoryRoot, "task-1");
        var outputPath = Path.Combine(Path.GetTempPath(), $"formula2cnf-{Guid.NewGuid():N}.cnf");

        try
        {
            var result = await RunFormula2Cnf(taskDirectory, formulaName, outputPath);

            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(outputPath), result.StandardError);
            AssertValidDimacs(await File.ReadAllTextAsync(outputPath));
        }
        finally
        {
            File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task Main_ConvertsTaskOneFileFromStandardInput()
    {
        var repositoryRoot = FindRepositoryRoot();
        var input = await File.ReadAllTextAsync(Path.Combine(repositoryRoot, "task-1", "nested_8.sat"));

        var result = await RunFormula2Cnf(repositoryRoot, ["--encoding", "equivalences"], input);

        Assert.Equal(0, result.ExitCode);
        AssertValidDimacs(result.StandardOutput);
    }

    public static IEnumerable<object[]> TaskOneFormulaNames()
    {
        var taskDirectory = Path.Combine(FindRepositoryRoot(), "task-1");
        return Directory.EnumerateFiles(taskDirectory, "*.sat")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new object[] { Path.GetFileName(path) });
    }

    private static void AssertValidDimacs(string output)
    {
        var formula = new DimacsReader().Read(new StringReader(output));
        Assert.NotEmpty(formula.Clauses);
    }

    private static async Task<ProcessResult> RunFormula2Cnf(
        string workingDirectory,
        params string[] arguments) =>
        await RunFormula2Cnf(workingDirectory, arguments, standardInput: null);

    private static async Task<ProcessResult> RunFormula2Cnf(
        string workingDirectory,
        string[] arguments,
        string? standardInput)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(FindFormula2CnfAssembly());
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start formula2cnf.");

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (standardInput is not null)
        {
            await process.StandardInput.WriteAsync(standardInput);
            process.StandardInput.Close();
        }

        await process.WaitForExitAsync();
        return new ProcessResult(process.ExitCode, await standardOutput, await standardError);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SatSolver.slnx")))
                return directory.FullName;
        }

        throw new DirectoryNotFoundException("Could not find the repository root.");
    }

    private static string FindFormula2CnfAssembly()
    {
        var assembly = Directory.EnumerateFiles(
                Path.Combine(FindRepositoryRoot(), "Formula2Cnf", "bin"),
                "formula2cnf.dll",
                SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        return assembly ?? throw new FileNotFoundException("Could not find the formula2cnf assembly.");
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
