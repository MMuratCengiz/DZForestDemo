using System.Buffers.Binary;
using System.Text;

namespace NiziKit.Build;

public static class NiziPackBuilder
{
    private const uint MagicNumber = 0x4E5A504B;
    private const uint Version = 1;
    private const int HeaderSize = 64;
    private const int Alignment = 8;

    private static readonly HashSet<string> AssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dztex", ".nizimesh", ".ozzskel", ".ozzanim"
    };

    public static void Build(string assetsRoot, string outputPath)
    {
        var files = ScanAssetFiles(assetsRoot);

        using var output = File.Create(outputPath);
        WritePackFile(output, files, assetsRoot);

        Console.WriteLine($"  Built: {Path.GetFileName(outputPath)} ({files.Count} files, {output.Length:N0} bytes)");
    }

    private static List<(string path, string fullPath)> ScanAssetFiles(string assetsRoot)
    {
        var files = new List<(string path, string fullPath)>();

        foreach (var file in Directory.EnumerateFiles(assetsRoot, "*.*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(file);
            if (!AssetExtensions.Contains(ext))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(assetsRoot, file).Replace('\\', '/');
            files.Add((relativePath, file));
        }

        return files;
    }

    private static void WritePackFile(FileStream output, List<(string path, string fullPath)> files, string assetsRoot)
    {
        var indexEntries = new List<(string path, byte[] pathBytes, long size)>();
        long totalDataSize = 0;

        foreach (var (path, fullPath) in files)
        {
            var pathBytes = Encoding.UTF8.GetBytes(path);
            var fileSize = new FileInfo(fullPath).Length;
            indexEntries.Add((path, pathBytes, fileSize));
            totalDataSize += AlignUp(fileSize, Alignment);
        }

        long indexSize = 0;
        foreach (var (_, pathBytes, _) in indexEntries)
        {
            indexSize += 2 + pathBytes.Length + 8 + 8;
        }

        long indexOffset = HeaderSize;
        long dataOffset = AlignUp(indexOffset + indexSize, Alignment);

        WriteHeader(output, (uint)files.Count, indexOffset, indexSize, dataOffset, totalDataSize);

        long currentDataOffset = 0;
        foreach (var (path, pathBytes, size) in indexEntries)
        {
            WriteIndexEntry(output, pathBytes, currentDataOffset, size);
            currentDataOffset += AlignUp(size, Alignment);
        }

        var paddingNeeded = dataOffset - output.Position;
        if (paddingNeeded > 0)
        {
            output.Write(new byte[paddingNeeded]);
        }

        foreach (var (path, fullPath) in files)
        {
            var data = File.ReadAllBytes(fullPath);
            output.Write(data);

            var padding = AlignUp(data.Length, Alignment) - data.Length;
            if (padding > 0)
            {
                output.Write(new byte[padding]);
            }
        }
    }

    private static void WriteHeader(FileStream output, uint entryCount, long indexOffset, long indexSize, long dataOffset, long dataSize)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        header.Clear();

        BinaryPrimitives.WriteUInt32LittleEndian(header, MagicNumber);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(4), Version);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(8), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header.Slice(12), entryCount);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(16), (ulong)indexOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(24), (ulong)indexSize);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(32), (ulong)dataOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(header.Slice(40), (ulong)dataSize);

        output.Write(header);
    }

    private static void WriteIndexEntry(FileStream output, byte[] pathBytes, long dataOffset, long size)
    {
        Span<byte> entry = stackalloc byte[2 + pathBytes.Length + 16];

        BinaryPrimitives.WriteUInt16LittleEndian(entry, (ushort)pathBytes.Length);
        pathBytes.CopyTo(entry.Slice(2));
        BinaryPrimitives.WriteUInt64LittleEndian(entry.Slice(2 + pathBytes.Length), (ulong)dataOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(entry.Slice(2 + pathBytes.Length + 8), (ulong)size);

        output.Write(entry);
    }

    private static long AlignUp(long value, int alignment)
    {
        return (value + alignment - 1) / alignment * alignment;
    }

    public static void BuildAll(string assetsDir, string outputDir)
    {
        Console.WriteLine($"Building NiziPacks from: {assetsDir}");

        if (!Directory.Exists(assetsDir))
        {
            Console.WriteLine($"Assets directory not found: {assetsDir}");
            return;
        }

        Directory.CreateDirectory(outputDir);

        var outputPath = Path.Combine(outputDir, "default.nizipack");

        try
        {
            Build(assetsDir, outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error building default pack: {ex.Message}");
        }
    }
}
