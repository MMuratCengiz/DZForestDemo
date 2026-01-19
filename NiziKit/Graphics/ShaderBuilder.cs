using System.Security.Cryptography;
using System.Text;
using DenOfIz;
using NiziKit.ContentPipeline;

namespace NiziKit.Graphics;

public class ShaderBuilder : IDisposable
{
    private readonly List<string> _includePaths = [];
    private readonly string _tempDir;
    private bool _disposed;

    public ShaderBuilder(params string[] additionalIncludePaths)
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NiziKit", "ShaderBuild", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);

        var shaderIncludesDir = Path.Combine(AppContext.BaseDirectory, "Shaders");
        if (Directory.Exists(shaderIncludesDir))
        {
            _includePaths.Add(shaderIncludesDir);
        }

        if (Content.IsInitialized)
        {
            var assetsShaders = Content.ResolvePath("Shaders");
            if (Directory.Exists(assetsShaders))
            {
                _includePaths.Add(assetsShaders);
            }
        }

        _includePaths.AddRange(additionalIncludePaths.Where(Directory.Exists));
    }

    public ShaderProgram CompileGraphics(
        string vertexPath,
        string pixelPath,
        string vsEntry = "VSMain",
        string psEntry = "PSMain",
        Dictionary<string, string?>? defines = null)
    {
        var preparedVs = PrepareShaderFile(vertexPath);
        var preparedPs = PrepareShaderFile(pixelPath);

        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Path = StringView.Create(preparedVs),
            EntryPoint = StringView.Create(vsEntry)
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create(preparedPs),
            EntryPoint = StringView.Create(psEntry)
        };

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            vsDesc.Defines = definesArray;
            psDesc.Defines = definesArray;
        }

        using var stagesArray = ShaderStageDescArray.Create([vsDesc, psDesc]);
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        };

        return new ShaderProgram(programDesc);
    }

    public ShaderProgram CompileCompute(
        string computePath,
        string csEntry = "CSMain",
        Dictionary<string, string?>? defines = null)
    {
        var preparedCs = PrepareShaderFile(computePath);

        var csDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Compute,
            Path = StringView.Create(preparedCs),
            EntryPoint = StringView.Create(csEntry)
        };

        if (defines is { Count: > 0 })
        {
            csDesc.Defines = CreateDefinesArray(defines);
        }

        using var stagesArray = ShaderStageDescArray.Create([csDesc]);
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        };

        return new ShaderProgram(programDesc);
    }

    public Task<ShaderProgram> CompileGraphicsAsync(
        string vertexPath,
        string pixelPath,
        string vsEntry = "VSMain",
        string psEntry = "PSMain",
        Dictionary<string, string?>? defines = null,
        CancellationToken ct = default)
    {
        return Task.Run(() => CompileGraphics(vertexPath, pixelPath, vsEntry, psEntry, defines), ct);
    }

    public Task<ShaderProgram> CompileComputeAsync(
        string computePath,
        string csEntry = "CSMain",
        Dictionary<string, string?>? defines = null,
        CancellationToken ct = default)
    {
        return Task.Run(() => CompileCompute(computePath, csEntry, defines), ct);
    }

    private string PrepareShaderFile(string shaderPath)
    {
        var fullPath = Path.IsPathRooted(shaderPath) ? shaderPath : Content.ResolvePath(shaderPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Shader file not found: {fullPath}");
        }

        string? relativePath = null;
        foreach (var includePath in _includePaths)
        {
            if (fullPath.StartsWith(includePath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = Path.GetRelativePath(includePath, fullPath);
                break;
            }
        }

        string destPath;
        if (relativePath != null)
        {
            destPath = Path.Combine(_tempDir, relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
        }
        else
        {
            destPath = Path.Combine(_tempDir, Path.GetFileName(fullPath));
        }

        File.Copy(fullPath, destPath, true);

        foreach (var includePath in _includePaths)
        {
            foreach (var subDir in Directory.GetDirectories(includePath))
            {
                var subDirName = Path.GetFileName(subDir);
                var linkPath = Path.Combine(_tempDir, subDirName);

                if (!Directory.Exists(linkPath) && !File.Exists(linkPath))
                {
                    CreateLinkOrCopy(subDir, linkPath);
                }
            }

            foreach (var file in Directory.GetFiles(includePath, "*.hlsl"))
            {
                var fileNameInclude = Path.GetFileName(file);
                var destFilePath = Path.Combine(_tempDir, fileNameInclude);
                if (!File.Exists(destFilePath))
                {
                    File.Copy(file, destFilePath, true);
                }
            }
        }

        return destPath;
    }

    private void CreateLinkOrCopy(string source, string dest)
    {
        try
        {
            Directory.CreateSymbolicLink(dest, source);
        }
        catch
        {
            CopyDirectory(source, dest);
        }
    }

    private static StringViewArray CreateDefinesArray(Dictionary<string, string?> defines)
    {
        var defineStrings = new List<StringView>();
        foreach (var (key, value) in defines)
        {
            var defineStr = value != null ? $"{key}={value}" : $"{key}=1";
            defineStrings.Add(StringView.Create(defineStr));
        }
        return StringViewArray.Create(defineStrings.ToArray());
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
        }
    }
}
