using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DenOfIz;
using NiziKit.Graphics;
using BinaryReader = DenOfIz.BinaryReader;
using BinaryWriter = DenOfIz.BinaryWriter;

namespace NiziKit.Assets.Store;

public class ShaderStore : IDisposable
{
    private readonly ConcurrentDictionary<string, GpuShader> _shaderCache = new();
    private readonly ConcurrentDictionary<string, ShaderProgram> _programCache = new();
    private readonly string _diskCacheDir;

    public ShaderStore(string? diskCacheDirectory = null)
    {
        _diskCacheDir = diskCacheDirectory ?? Path.Combine(AppContext.BaseDirectory, "ShaderCache");
        Directory.CreateDirectory(_diskCacheDir);
    }

    public GpuShader? this[string key] => _shaderCache.GetValueOrDefault(key);

    public void Register(string key, GpuShader shader)
    {
        _shaderCache.TryAdd(key, shader);
    }

    public GpuShader? Get(string baseName, IReadOnlyDictionary<string, string?>? variants)
    {
        var fullName = ShaderVariants.EncodeName(baseName, variants);
        return _shaderCache.GetValueOrDefault(fullName);
    }

    public bool Contains(string baseName, IReadOnlyDictionary<string, string?>? variants)
    {
        var fullName = ShaderVariants.EncodeName(baseName, variants);
        return _shaderCache.ContainsKey(fullName);
    }

    public ShaderProgram? GetProgram(string key)
    {
        return _programCache.GetValueOrDefault(key);
    }

    public void RegisterProgram(string key, ShaderProgram program)
    {
        _programCache.TryAdd(key, program);
    }

    public ShaderProgram? TryLoadFromDisk(string cacheKey)
    {
        var cachePath = GetDiskCachePath(cacheKey);
        if (!File.Exists(cachePath))
        {
            return null;
        }

        try
        {
            var bytes = File.ReadAllBytes(cachePath);
            return LoadProgramFromBytes(bytes);
        }
        catch
        {
            try { File.Delete(cachePath); } catch { }
            return null;
        }
    }

    public void SaveToDisk(string cacheKey, ShaderProgram program)
    {
        var cachePath = GetDiskCachePath(cacheKey);
        var cachePathDir = Path.GetDirectoryName(cachePath);
        if (!string.IsNullOrEmpty(cachePathDir))
        {
            Directory.CreateDirectory(cachePathDir);
        }

        try
        {
            var compiledShaders = program.CompiledShaders();
            var reflection = program.Reflect();

            var compiledShader = new CompiledShader
            {
                Stages = compiledShaders,
                ReflectDesc = reflection,
                RayTracing = new ShaderRayTracingDesc()
            };

            var asset = ShaderAssetWriter.CreateFromCompiledShader(compiledShader);
            asset.SetPath(StringView.Create(cachePath));

            var binaryWriter = BinaryWriter.CreateFromFile(StringView.Create(cachePath));
            var assetWriterDesc = new ShaderAssetWriterDesc
            {
                Writer = binaryWriter
            };
            var assetWriter = new ShaderAssetWriter(assetWriterDesc);
            assetWriter.Write(asset);
            assetWriter.End();
            assetWriter.Dispose();
            binaryWriter.Dispose();
        }
        catch
        {
        }
    }

    public string ComputeCacheKey(string[] shaderPaths, Dictionary<string, string?>? defines)
    {
        using var sha256 = SHA256.Create();
        var sb = new StringBuilder();

        foreach (var path in shaderPaths.OrderBy(p => p))
        {
            sb.Append(path);
            sb.Append(':');

            if (File.Exists(path))
            {
                var content = File.ReadAllText(path);
                sb.Append(content);
                CollectIncludes(path, sb, []);
            }

            sb.Append('|');
        }

        if (defines != null)
        {
            foreach (var (key, value) in defines.OrderBy(kv => kv.Key))
            {
                sb.Append(key);
                sb.Append('=');
                sb.Append(value ?? "1");
                sb.Append(';');
            }
        }

        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..16];
    }

    public void ClearDiskCache()
    {
        if (Directory.Exists(_diskCacheDir))
        {
            foreach (var file in Directory.GetFiles(_diskCacheDir, "*", SearchOption.AllDirectories))
            {
                try { File.Delete(file); } catch { }
            }
        }
    }

    private void CollectIncludes(string shaderPath, StringBuilder sb, HashSet<string> visited)
    {
        if (!File.Exists(shaderPath) || !visited.Add(shaderPath))
        {
            return;
        }

        var content = File.ReadAllText(shaderPath);
        var shaderDir = Path.GetDirectoryName(shaderPath) ?? "";

        var includePattern = new Regex(@"#include\s+""([^""]+)""");
        var matches = includePattern.Matches(content);

        foreach (Match match in matches)
        {
            var includePath = match.Groups[1].Value;
            var resolvedPath = ResolveIncludePath(includePath, shaderDir);

            if (resolvedPath != null && File.Exists(resolvedPath))
            {
                sb.Append(resolvedPath);
                sb.Append(':');
                sb.Append(File.ReadAllText(resolvedPath));
                sb.Append('|');

                CollectIncludes(resolvedPath, sb, visited);
            }
        }
    }

    private static string? ResolveIncludePath(string includePath, string shaderDir)
    {
        var relative = Path.Combine(shaderDir, includePath);
        if (File.Exists(relative))
        {
            return Path.GetFullPath(relative);
        }

        var shaderIncludes = Path.Combine(AppContext.BaseDirectory, "Shaders", includePath);
        if (File.Exists(shaderIncludes))
        {
            return Path.GetFullPath(shaderIncludes);
        }

        return null;
    }

    private string GetDiskCachePath(string cacheKey)
    {
        return Path.Combine(_diskCacheDir, $"{cacheKey}.dzshader");
    }

    private static ShaderProgram LoadProgramFromBytes(byte[] bytes)
    {
        using var container = new BinaryContainer();
        var writer = BinaryWriter.CreateFromContainer(container);
        writer.Write(ByteArrayView.Create(bytes), 0, (uint)bytes.Length);
        writer.Dispose();

        var readerDesc = new BinaryReaderDesc
        {
            NumBytes = 0
        };
        var reader = BinaryReader.CreateFromContainer(container, readerDesc);

        var assetReaderDesc = new ShaderAssetReaderDesc
        {
            Reader = reader
        };
        var assetReader = new ShaderAssetReader(assetReaderDesc);

        var program = ShaderProgram.CreateFromAsset(assetReader);

        assetReader.Dispose();
        reader.Dispose();

        return program;
    }

    public void Dispose()
    {
        foreach (var shader in _shaderCache.Values)
        {
            shader.Dispose();
        }
        _shaderCache.Clear();

        foreach (var program in _programCache.Values)
        {
            program.Dispose();
        }
        _programCache.Clear();
    }
}
