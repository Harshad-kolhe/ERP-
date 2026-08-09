namespace Erp.ArchitectureTests;

/// <summary>
/// Locates the two roots these tests need, by walking up from the test binaries
/// looking for a file that only that root contains.
/// <para>
/// Anchored on marker files rather than a hard-coded path, or a counted number of
/// <c>..</c> hops, so that moving a folder inside the repository does not silently
/// break a scan — which is exactly what happened when the source scan was pinned
/// to <c>apps/api/src</c>.
/// </para>
/// </summary>
internal static class RepositoryPaths
{
    /// <summary>The directory holding <c>Erp.slnx</c> — <c>backend/</c>.</summary>
    public static DirectoryInfo SolutionRoot() =>
        FindAncestorContaining(directory => directory.GetFiles("*.slnx").Length != 0, "a *.slnx solution file");

    /// <summary>
    /// The repository root — the parent of <c>backend/</c>, <c>frontend/</c>, <c>db/</c>
    /// and <c>docs/</c>.
    /// </summary>
    public static DirectoryInfo RepositoryRoot() =>
        FindAncestorContaining(
            directory => directory.GetFiles("pnpm-workspace.yaml").Length != 0,
            "pnpm-workspace.yaml");

    private static DirectoryInfo FindAncestorContaining(Func<DirectoryInfo, bool> predicate, string marker)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !predicate(directory))
        {
            directory = directory.Parent;
        }

        return directory
            ?? throw new InvalidOperationException(
                $"Could not locate {marker} in any ancestor of {AppContext.BaseDirectory}");
    }
}
