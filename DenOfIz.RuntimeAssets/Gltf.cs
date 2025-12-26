using System.Buffers.Binary;
using System.Text.Json;
using RuntimeAssets.GltfModels;

namespace RuntimeAssets;

public static class Gltf
{
    private const uint GlbMagic = 0x46546C67; // "glTF"
    private const uint ChunkTypeJson = 0x4E4F534A; // "JSON"
    private const uint ChunkTypeBin = 0x004E4942; // "BIN\0"

    public static GltfDocument Load(string path, GltfDocumentDesc? options = null)
    {
        var document = new GltfDocument(path, options ?? new GltfDocumentDesc());

        if (!File.Exists(path))
        {
            document.AddError($"File not found: {path}");
            return document;
        }

        var bytes = File.ReadAllBytes(path);
        ParseDocument(document, bytes.AsSpan(), path);
        return document;
    }

    public static GltfDocument Load(ReadOnlySpan<byte> data, string basePath = "", GltfDocumentDesc? options = null)
    {
        var document = new GltfDocument(basePath, options ?? new GltfDocumentDesc());
        ParseDocument(document, data, basePath);
        return document;
    }

    public static async Task<GltfDocument> LoadAsync(string path, GltfDocumentDesc? options = null, CancellationToken ct = default)
    {
        var document = new GltfDocument(path, options ?? new GltfDocumentDesc());

        if (!File.Exists(path))
        {
            document.AddError($"File not found: {path}");
            return document;
        }

        var bytes = await File.ReadAllBytesAsync(path, ct);
        ParseDocument(document, bytes.AsSpan(), path);
        return document;
    }

    private static void ParseDocument(GltfDocument document, ReadOnlySpan<byte> data, string path)
    {
        if (data.Length < 4)
        {
            document.AddError("File too small to be valid GLTF");
            return;
        }

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (magic == GlbMagic)
        {
            ParseGlb(document, data);
        }
        else
        {
            ParseGltfJson(document, data);
        }
    }

    private static void ParseGlb(GltfDocument document, ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
        {
            document.AddError("Invalid GLB header");
            return;
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        var length = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);

        if (version != 2)
        {
            document.AddWarning($"Unsupported GLB version: {version}, attempting to parse anyway");
        }

        if (length > data.Length)
        {
            document.AddWarning($"GLB header claims {length} bytes but file only has {data.Length}");
        }

        var offset = 12;
        ReadOnlySpan<byte> jsonChunk = default;
        ReadOnlySpan<byte> binChunk = default;

        while (offset + 8 <= data.Length)
        {
            var chunkLength = BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            var chunkType = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 4)..]);
            offset += 8;

            if (offset + chunkLength > data.Length)
            {
                document.AddWarning($"Chunk extends beyond file boundary, truncating");
                chunkLength = (uint)(data.Length - offset);
            }

            var chunkData = data.Slice(offset, (int)chunkLength);
            offset += (int)chunkLength;

            if (chunkType == ChunkTypeJson)
            {
                jsonChunk = chunkData;
            }
            else if (chunkType == ChunkTypeBin)
            {
                binChunk = chunkData;
            }
        }

        if (jsonChunk.IsEmpty)
        {
            document.AddError("GLB file has no JSON chunk");
            return;
        }

        ParseGltfJson(document, jsonChunk);
        if (!binChunk.IsEmpty)
        {
            document.SetEmbeddedBinaryBuffer(binChunk.ToArray());
        }
    }

    private static void ParseGltfJson(GltfDocument document, ReadOnlySpan<byte> jsonData)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var root = JsonSerializer.Deserialize<GltfRoot>(jsonData, options);
            if (root == null)
            {
                document.AddError("Failed to parse GLTF JSON");
                return;
            }

            document.SetRoot(root);
        }
        catch (JsonException ex)
        {
            document.AddError($"JSON parse error: {ex.Message}");
        }
    }
}