using System.Globalization;

namespace Erp.ArchitectureTests;

/// <summary>
/// Replaces planned analyzer ERP0001.
/// <para>
/// The legacy codebase contained a single 10,444-line class (<c>BomBLL</c>), a
/// 5,345-line one, a 4,739-line one, and a controller of 4,686 lines holding 249
/// action methods. Files that large stop being reviewable, and because every
/// developer edits the same one, 329 of 1,169 commits were merges.
/// </para>
/// <para>
/// A length limit does not by itself produce good design — but it makes the bad
/// outcome impossible to reach silently.
/// </para>
/// </summary>
public sealed class SourceFileTests
{
    private const int MaxLines = 800;

    [Fact]
    public void No_source_file_exceeds_the_length_limit()
    {
        var offenders = SourceFiles()
            .Select(file => (Path: file, Lines: File.ReadAllLines(file).Length))
            .Where(entry => entry.Lines > MaxLines)
            .OrderByDescending(entry => entry.Lines)
            .Select(entry => string.Create(
                CultureInfo.InvariantCulture,
                $"{entry.Lines,6} lines  {Relative(entry.Path)}"))
            .ToList();

        offenders.ShouldBeEmpty(
            $"no source file may exceed {MaxLines} lines. Split these into feature slices:\n"
            + string.Join('\n', offenders));
    }

    /// <summary>
    /// Guards the guard: if the path walk broke, every length assertion above would
    /// pass over an empty set and report success.
    /// </summary>
    [Fact]
    public void Source_scan_actually_finds_files()
    {
        SourceFiles().Count.ShouldBeGreaterThan(20, "the source scan found almost nothing — the repository root walk is wrong.");
    }

    private static List<string> SourceFiles() =>
        [.. Directory
            .EnumerateFiles(Path.Combine(RepositoryPaths.SolutionRoot().FullName, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)

                // EF writes these; they are reviewed for schema correctness, not length.
                && !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];

    private static string Relative(string path) =>
        Path.GetRelativePath(RepositoryPaths.SolutionRoot().FullName, path);
}
