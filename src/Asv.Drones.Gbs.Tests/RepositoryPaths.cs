namespace Asv.Drones.Gbs.Tests;

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string Get(params string[] parts)
    {
        return Path.Combine([Root, .. parts]);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (
                File.Exists(Path.Combine(directory.FullName, "README.md"))
                && File.Exists(Path.Combine(directory.FullName, "src", "Asv.Drones.Gbs.sln"))
            )
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Unable to locate repository root from '{AppContext.BaseDirectory}'."
        );
    }
}
