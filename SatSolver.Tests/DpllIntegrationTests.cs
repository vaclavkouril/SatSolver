using System.Diagnostics;

namespace SatSolver.Tests;

/// <summary>Tests the <c>dpll</c> command-line application.</summary>
public sealed class DpllIntegrationTests
{
    /// <summary>Verifies simplified SMT-LIB input.</summary>
    [Fact]
    public async Task Main_SatInput_WritesSatModelAndStatistics()
    {
        var repositoryRoot = FindRepositoryRoot();
        var inputPath = Path.Combine(repositoryRoot, "task-1", "toy_5.sat");

        var result = await RunDpll(repositoryRoot, "--propagation", "adjacency", inputPath);

        Assert.Equal(0, result.ExitCode);
        Assert.StartsWith("SAT" + Environment.NewLine, result.StandardOutput);
        Assert.Contains("v ", result.StandardOutput);
        Assert.Contains("CPU time:", result.StandardOutput);
        Assert.Contains("Decisions:", result.StandardOutput);
        Assert.Contains("Unit propagations:", result.StandardOutput);
    }

    /// <summary>Verifies ordered DIMACS models.</summary>
    [Fact]
    public async Task Main_DimacsInput_WritesOrderedModel()
    {
        var inputPath = Path.Combine(Path.GetTempPath(), $"dpll-{Guid.NewGuid():N}.cnf");
        await File.WriteAllTextAsync(inputPath, "p cnf 3 1\n-3 0\n");

        try
        {
            var result = await RunDpll(FindRepositoryRoot(), inputPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("v 1 2 -3 0", result.StandardOutput);
        }
        finally
        {
            File.Delete(inputPath);
        }
    }

    private static async Task<ProcessResult> RunDpll(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(FindDpllAssembly());
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dpll.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

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

    private static string FindDpllAssembly() =>
        Directory.EnumerateFiles(
                Path.Combine(FindRepositoryRoot(), "Dpll", "bin"),
                "dpll.dll",
                SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault()
        ?? throw new FileNotFoundException("Could not find the dpll assembly.");

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
