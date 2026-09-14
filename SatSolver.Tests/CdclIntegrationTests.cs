using System.Diagnostics;

namespace SatSolver.Tests;

public sealed class CdclIntegrationTests
{
    [Fact]
    public async Task Main_SatInput_WritesModelAndStatistics()
    {
        var repositoryRoot = FindRepositoryRoot();
        var inputPath = Path.Combine(repositoryRoot, "task-1", "toy_5.sat");

        var result = await RunCdcl(repositoryRoot, null, inputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("SAT" + Environment.NewLine, result.StandardOutput);
        Assert.Contains("v ", result.StandardOutput);
        Assert.Contains("CPU time:", result.StandardOutput);
        Assert.Contains("Conflicts:", result.StandardOutput);
    }

    [Theory]
    [InlineData("adjacency", "decision", "recursive", "geometric", "activity")]
    [InlineData("watched", "multiple", "ssr", "luby", "lbd")]
    public async Task Main_StrategyOptions_WritesSatResult(
        string propagation,
        string conflictAnalysis,
        string minimization,
        string restart,
        string clauseDeletion)
    {
        var repositoryRoot = FindRepositoryRoot();
        var inputPath = Path.Combine(repositoryRoot, "task-1", "toy_5.sat");

        var result = await RunCdcl(
            repositoryRoot,
            null,
            "--propagation", propagation,
            "--conflict-analysis", conflictAnalysis,
            "--minimization", minimization,
            "--restart", restart,
            "--clause-deletion", clauseDeletion,
            inputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("SAT" + Environment.NewLine, result.StandardOutput);
    }

    [Fact]
    public async Task Main_StandardInputAndTuning_WritesSatResult()
    {
        const string input = "(or a (and b c))";
        var result = await RunCdcl(
            FindRepositoryRoot(),
            input,
            "--format", "sat",
            "--restart", "geometric",
            "--restart-limit", "10",
            "--restart-growth", "1.25",
            "--luby-unit", "5",
            "--clause-deletion", "lbd-activity",
            "--deletion-limit", "10",
            "--deletion-growth", "1.25",
            "--keep-lbd", "2",
            "--deletion-fraction", "0.5");

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("SAT" + Environment.NewLine, result.StandardOutput);
    }

    [Theory]
    [InlineData("first")]
    [InlineData("random")]
    [InlineData("jw")]
    [InlineData("vsids")]
    public async Task Main_DecisionHeuristic_WritesSatResult(string heuristic)
    {
        var repositoryRoot = FindRepositoryRoot();
        var inputPath = Path.Combine(repositoryRoot, "task-1", "toy_5.sat");
        var arguments = new List<string> { "--heuristic", heuristic };

        if (heuristic is "random" or "vsids")
        {
            arguments.Add("--seed");
            arguments.Add("17");
        }

        arguments.Add(inputPath);
        var result = await RunCdcl(repositoryRoot, null, arguments.ToArray());

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("SAT" + Environment.NewLine, result.StandardOutput);
    }

    private static async Task<ProcessResult> RunCdcl(
        string workingDirectory,
        string? standardInput,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(FindCdclAssembly());
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start cdcl.");
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

    private static string FindCdclAssembly() =>
        Directory.EnumerateFiles(
                Path.Combine(FindRepositoryRoot(), "Cdcl", "bin"),
                "cdcl.dll",
                SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault()
        ?? throw new FileNotFoundException("Could not find the cdcl assembly.");

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
