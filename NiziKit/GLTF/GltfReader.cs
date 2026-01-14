using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public static class GltfReader
{
    private const uint GlbMagic = 0x46546C67;
    private const uint ChunkJson = 0x4E4F534A;
    private const uint ChunkBin = 0x004E4942;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static GltfDocument ReadGlb(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
        {
            throw new InvalidDataException("GLB file too small");
        }

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(data);
        if (magic != GlbMagic)
        {
            throw new InvalidDataException("Invalid GLB magic number");
        }

        var version = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        if (version != 2)
        {
            throw new NotSupportedException($"GLB version {version} not supported");
        }

        var length = BinaryPrimitives.ReadUInt32LittleEndian(data[8..]);
        if (length > data.Length)
        {
            throw new InvalidDataException("GLB file truncated");
        }

        GltfRoot? root = null;
        byte[]? binChunk = null;
        var offset = 12;

        while (offset + 8 <= data.Length)
        {
            var chunkLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]);
            var chunkType = BinaryPrimitives.ReadUInt32LittleEndian(data[(offset + 4)..]);
            offset += 8;

            if (chunkLength == 0)
            {
                throw new InvalidDataException("GLB chunk has zero length");
            }

            if ((chunkLength & 3) != 0)
            {
                throw new InvalidDataException($"GLB chunk length {chunkLength} is not 4-byte aligned");
            }

            if (offset + chunkLength > data.Length)
            {
                break;
            }

            var chunkData = data.Slice(offset, chunkLength);

            if (chunkType == ChunkJson)
            {
                if (root != null)
                {
                    throw new InvalidDataException("GLB file has duplicate JSON chunk");
                }
                root = JsonSerializer.Deserialize<GltfRoot>(chunkData, JsonOptions);
            }
            else if (chunkType == ChunkBin)
            {
                if (binChunk != null)
                {
                    throw new InvalidDataException("GLB file has duplicate BIN chunk");
                }
                binChunk = chunkData.ToArray();
            }

            offset += chunkLength;
        }

        if (root == null)
        {
            throw new InvalidDataException("GLB file missing JSON chunk");
        }

        var buffers = binChunk != null ? new[] { binChunk } : Array.Empty<byte[]>();

        return new GltfDocument
        {
            Root = root,
            Buffers = buffers
        };
    }

    public static GltfDocument ReadGltf(ReadOnlySpan<byte> jsonData, Func<string, byte[]>? loadBuffer = null, string? basePath = null)
    {
        var root = JsonSerializer.Deserialize<GltfRoot>(jsonData, JsonOptions);
        if (root == null)
        {
            throw new InvalidDataException("Failed to parse GLTF JSON");
        }

        var buffers = new List<byte[]>();

        if (root.Buffers != null)
        {
            foreach (var buffer in root.Buffers)
            {
                if (buffer.Uri != null)
                {
                    if (buffer.Uri.StartsWith("data:"))
                    {
                        buffers.Add(DecodeDataUri(buffer.Uri));
                    }
                    else if (loadBuffer != null)
                    {
                        buffers.Add(loadBuffer(buffer.Uri));
                    }
                    else
                    {
                        buffers.Add(Array.Empty<byte>());
                    }
                }
                else
                {
                    buffers.Add(Array.Empty<byte>());
                }
            }
        }

        return new GltfDocument
        {
            Root = root,
            Buffers = buffers.ToArray(),
            BasePath = basePath
        };
    }

    public static GltfDocument Read(ReadOnlySpan<byte> data, Func<string, byte[]>? loadBuffer = null, string? basePath = null)
    {
        if (data.Length >= 4)
        {
            var magic = BinaryPrimitives.ReadUInt32LittleEndian(data);
            if (magic == GlbMagic)
            {
                return ReadGlb(data);
            }
        }

        return ReadGltf(data, loadBuffer, basePath);
    }

    private static byte[] DecodeDataUri(string uri)
    {
        var commaIndex = uri.IndexOf(',');
        if (commaIndex < 0)
        {
            return Array.Empty<byte>();
        }

        var header = uri.AsSpan(0, commaIndex);
        var data = uri.AsSpan(commaIndex + 1);

        if (header.Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.FromBase64String(data.ToString());
        }

        return Encoding.UTF8.GetBytes(Uri.UnescapeDataString(data.ToString()));
    }
}
