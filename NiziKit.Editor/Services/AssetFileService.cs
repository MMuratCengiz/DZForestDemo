using NiziKit.ContentPipeline;

namespace NiziKit.Editor.Services;

public enum AssetFileType
{
    All,
    Model,
    Texture,
    Scene,
    Folder,
    Other
}

public class FileEntry
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required string RelativePath { get; init; }
    public required bool IsDirectory { get; init; }
    public required AssetFileType Type { get; init; }

    public string IconData => Type switch
    {
        AssetFileType.Folder => "M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z",
        AssetFileType.Model => "M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5",
        AssetFileType.Texture => "M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z",
        AssetFileType.Scene => "M12 5.69l5 4.5V18h-2v-6H9v6H7v-7.81l5-4.5M12 3L2 12h3v8h6v-6h2v6h6v-8h3L12 3z",
        _ => "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zM6 20V4h7v5h5v11H6z"
    };
}

public class AssetFileService(string assetsPath)
{
    private static readonly Dictionary<string, AssetFileType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".fbx", AssetFileType.Model },
        { ".glb", AssetFileType.Model },
        { ".gltf", AssetFileType.Model },
        { ".obj", AssetFileType.Model },
        { ".dae", AssetFileType.Model },
        { ".png", AssetFileType.Texture },
        { ".jpg", AssetFileType.Texture },
        { ".jpeg", AssetFileType.Texture },
        { ".tga", AssetFileType.Texture },
        { ".bmp", AssetFileType.Texture },
        { ".dds", AssetFileType.Texture },
        { ".dztex", AssetFileType.Texture },
    };

    public AssetFileService() : this(Content.ResolvePath(""))
    {
    }

    public string AssetsPath => assetsPath;

    public IReadOnlyList<FileEntry> GetEntries(string directory, AssetFileType filter = AssetFileType.All)
    {
        var entries = new List<FileEntry>();

        if (!Directory.Exists(directory))
        {
            return entries;
        }

        // Add directories first
        foreach (var dir in Directory.GetDirectories(directory))
        {
            var dirInfo = new DirectoryInfo(dir);
            entries.Add(new FileEntry
            {
                Name = dirInfo.Name,
                FullPath = dir,
                RelativePath = GetRelativePath(dir),
                IsDirectory = true,
                Type = AssetFileType.Folder
            });
        }

        // Add files
        foreach (var file in Directory.GetFiles(directory))
        {
            var fileType = GetFileType(file);

            // Apply filter
            if (filter != AssetFileType.All && fileType != filter)
            {
                continue;
            }

            var fileInfo = new FileInfo(file);
            entries.Add(new FileEntry
            {
                Name = fileInfo.Name,
                FullPath = file,
                RelativePath = GetRelativePath(file),
                IsDirectory = false,
                Type = fileType
            });
        }

        return entries;
    }

    public AssetFileType GetFileType(string path)
    {
        if (Directory.Exists(path))
        {
            return AssetFileType.Folder;
        }

        var extension = Path.GetExtension(path);

        // Check for compound extensions first
        if (path.EndsWith(".niziscene.json", StringComparison.OrdinalIgnoreCase))
        {
            return AssetFileType.Scene;
        }

        return ExtensionMap.GetValueOrDefault(extension, AssetFileType.Other);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool Exists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    private string GetRelativePath(string fullPath)
    {
        if (string.IsNullOrEmpty(assetsPath))
        {
            return fullPath;
        }

        return Path.GetRelativePath(assetsPath, fullPath);
    }
}
