namespace SatSolver.Core.Solving.Cdcl.Restarts;

/// <summary>Provides terms of the one-based Luby restart sequence.</summary>
internal static class LubySequence
{
    public static int ValueAt(int runIndex)
    {
        if (runIndex < 1)
            throw new ArgumentOutOfRangeException(nameof(runIndex));

        var blockEnd = 1;
        var value = 1;
        while (blockEnd < runIndex)
        {
            blockEnd = (blockEnd << 1) + 1;
            value <<= 1;
        }

        return runIndex == blockEnd
            ? value
            : ValueAt(runIndex - (blockEnd / 2));
    }
}
