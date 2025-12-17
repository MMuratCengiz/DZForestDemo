using System.Text;
using System.Text.RegularExpressions;

namespace RuntimeAssets;

public sealed partial class ShaderLoader
{
    private readonly Dictionary<string, string> _defines = [];
    private readonly HashSet<string> _includedFiles = [];
    private readonly List<string> _includePaths = [];

    public ShaderLoader()
    {
        if (Directory.Exists(AssetPaths.Shaders))
        {
            _includePaths.Add(AssetPaths.Shaders);
        }
    }

    public ShaderLoader AddIncludePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!_includePaths.Contains(fullPath))
        {
            _includePaths.Add(fullPath);
        }

        return this;
    }

    public ShaderLoader AddDefine(string name, string? value = null)
    {
        _defines[name] = value ?? "1";
        return this;
    }

    public string Load(string shaderPath)
    {
        _includedFiles.Clear();

        var fullPath = AssetPaths.ResolveShader(shaderPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}", fullPath);
        }

        var baseDirectory = Path.GetDirectoryName(fullPath)!;
        var source = File.ReadAllText(fullPath);

        var processed = ProcessIncludes(source, baseDirectory, fullPath);
        return PrependDefines(processed);
    }

    public async Task<string> LoadAsync(string shaderPath, CancellationToken cancellationToken = default)
    {
        _includedFiles.Clear();

        var fullPath = AssetPaths.ResolveShader(shaderPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}", fullPath);
        }

        var baseDirectory = Path.GetDirectoryName(fullPath)!;
        var source = await File.ReadAllTextAsync(fullPath, cancellationToken);

        var processed = ProcessIncludes(source, baseDirectory, fullPath);
        return PrependDefines(processed);
    }

    public string ProcessSource(string source, string? baseDirectory = null)
    {
        _includedFiles.Clear();
        baseDirectory ??= AssetPaths.Shaders;
        var processed = ProcessIncludes(source, baseDirectory, null);
        return PrependDefines(processed);
    }

    private string ProcessIncludes(string source, string baseDirectory, string? currentFile)
    {
        var result = new StringBuilder();
        var lines = source.Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.TrimStart();
            var match = IncludeRegex().Match(trimmedLine);
            if (match.Success)
            {
                var includePath = match.Groups[1].Value;
                var resolvedPath = ResolveIncludePath(includePath, baseDirectory);

                if (resolvedPath == null)
                {
                    result.AppendLine(line);
                    continue;
                }

                var normalizedPath = Path.GetFullPath(resolvedPath);
                if (_includedFiles.Contains(normalizedPath))
                {
                    result.AppendLine($"// Already included: {includePath}");
                    continue;
                }

                _includedFiles.Add(normalizedPath);
                result.AppendLine($"// Begin include: {includePath}");

                var includeSource = File.ReadAllText(resolvedPath);
                var includeDirectory = Path.GetDirectoryName(resolvedPath)!;
                var processedInclude = ProcessIncludes(includeSource, includeDirectory, resolvedPath);
                result.Append(processedInclude);

                result.AppendLine($"// End include: {includePath}");
            }
            else
            {
                result.AppendLine(line);
            }
        }

        return result.ToString();
    }

    private string? ResolveIncludePath(string includePath, string baseDirectory)
    {
        includePath = includePath.Replace('/', Path.DirectorySeparatorChar);
        var relativePath = Path.Combine(baseDirectory, includePath);
        if (File.Exists(relativePath))
        {
            return relativePath;
        }

        foreach (var includeDir in _includePaths)
        {
            var fullPath = Path.Combine(includeDir, includePath);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    private string PrependDefines(string source)
    {
        if (_defines.Count == 0)
        {
            return source;
        }

        var sb = new StringBuilder();
        sb.AppendLine("// User defines");
        foreach (var (name, value) in _defines)
        {
            sb.AppendLine($"#define {name} {value}");
        }

        sb.AppendLine("// End user defines");
        sb.AppendLine();
        sb.Append(source);
        return sb.ToString();
    }

    [GeneratedRegex(@"^#include\s+[""<](.+?)["">]")]
    private static partial Regex IncludeRegex();
}

public static class ShaderLoaderExtensions
{
    private static ShaderLoader? _defaultLoader;
    private static readonly Lock Lock = new();

    public static ShaderLoader Default
    {
        get
        {
            if (_defaultLoader != null)
            {
                return _defaultLoader;
            }

            lock (Lock)
            {
                _defaultLoader ??= new ShaderLoader();
                return _defaultLoader;
            }
        }
    }

    public static string LoadShader(string shaderPath)
    {
        return Default.Load(shaderPath);
    }

    public static Task<string> LoadShaderAsync(string shaderPath, CancellationToken cancellationToken = default)
    {
        return Default.LoadAsync(shaderPath, cancellationToken);
    }
}