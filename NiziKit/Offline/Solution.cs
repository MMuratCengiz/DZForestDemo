using System.Text.RegularExpressions;

namespace NiziKit.Offline;

public static partial class Solution
{
    private static readonly Lock Lock = new();
    private static string? _solutionPath;
    private static string? _solutionDir;
    private static Dictionary<string, string>? _projectPaths;

    public static string SolutionPath
    {
        get
        {
            EnsureInitialized();
            return _solutionPath!;
        }
    }

    public static string SolutionDir
    {
        get
        {
            EnsureInitialized();
            return _solutionDir!;
        }
    }

    public static string Project(string name)
    {
        EnsureInitialized();
        if (_projectPaths!.TryGetValue(name, out var path))
        {
            return path;
        }
        throw new InvalidOperationException($"Project '{name}' not found in solution. Available: {string.Join(", ", _projectPaths.Keys)}");
    }

    public static string ProjectAssets(string name) => Path.Combine(Project(name), "Assets");

    public static bool HasProject(string name)
    {
        EnsureInitialized();
        return _projectPaths!.ContainsKey(name);
    }

    public static IReadOnlyList<string> ProjectNames
    {
        get
        {
            EnsureInitialized();
            return _projectPaths!.Keys.ToList();
        }
    }

    private static void EnsureInitialized()
    {
        if (_projectPaths != null)
        {
            return;
        }

        lock (Lock)
        {
            if (_projectPaths != null)
            {
                return;
            }

            var slnPath = FindSolutionFile();
            if (slnPath == null)
            {
                throw new InvalidOperationException("Could not find .sln file. Ensure you're running from within a solution directory.");
            }

            _solutionPath = Path.GetFullPath(slnPath);
            _solutionDir = Path.GetDirectoryName(_solutionPath)!;
            _projectPaths = ParseSolutionProjects(_solutionPath, _solutionDir);
        }
    }

    private static string? FindSolutionFile()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var slnFiles = Directory.GetFiles(dir, "*.sln");
            if (slnFiles.Length > 0)
            {
                return slnFiles[0];
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }

    private static Dictionary<string, string> ParseSolutionProjects(string slnPath, string slnDir)
    {
        var projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var content = File.ReadAllText(slnPath);

        var matches = ProjectLineRegex().Matches(content);
        foreach (Match match in matches)
        {
            var projectName = match.Groups[1].Value;
            var relativePath = match.Groups[2].Value.Replace('\\', Path.DirectorySeparatorChar);

            if (relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase) ||
                relativePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
            {
                var projectDir = Path.GetFullPath(Path.Combine(slnDir, Path.GetDirectoryName(relativePath) ?? ""));
                projects[projectName] = projectDir;
            }
        }

        return projects;
    }

    [GeneratedRegex(@"Project\(""\{[^}]+\}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""")]
    private static partial Regex ProjectLineRegex();
}
