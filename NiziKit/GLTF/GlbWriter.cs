using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.GLTF;

/// <summary>
/// Minimal GLB serializer that round-trips a modified GltfDocument back to bytes.
/// </summary>
public static class GlbWriter
{
    private const uint GlbMagic = 0x46546C67;
    private const uint GlbVersion = 2;
    private const uint ChunkJson = 0x4E4F534A;
    private const uint ChunkBin = 0x004E4942;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a <see cref="GltfDocument"/> to GLB binary format.
    /// </summary>
    public static byte[] WriteGlb(GltfDocument document)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(document.Root, JsonOptions);
        var jsonPadded = PadToAlignment(jsonBytes, (byte)' ');
        var binData = document.Buffers.Length > 0 ? document.Buffers[0] : [];

        if (document.Root.Buffers is { Count: > 0 })
        {
            document.Root.Buffers[0].ByteLength = binData.Length;
            jsonBytes = JsonSerializer.SerializeToUtf8Bytes(document.Root, JsonOptions);
            jsonPadded = PadToAlignment(jsonBytes, (byte)' ');
        }

        var binPadded = PadToAlignment(binData, 0x00);

        var totalLength = 12 + 8 + jsonPadded.Length;
        if (binPadded.Length > 0)
        {
            totalLength += 8 + binPadded.Length;
        }

        var result = new byte[totalLength];
        var offset = 0;

        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), GlbMagic);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), GlbVersion);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), (uint)totalLength);
        offset += 4;

        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), (uint)jsonPadded.Length);
        offset += 4;
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), ChunkJson);
        offset += 4;
        jsonPadded.CopyTo(result.AsSpan(offset));
        offset += jsonPadded.Length;

        if (binPadded.Length > 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), (uint)binPadded.Length);
            offset += 4;
            BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(offset), ChunkBin);
            offset += 4;
            binPadded.CopyTo(result.AsSpan(offset));
        }

        return result;
    }

    private static byte[] PadToAlignment(byte[] data, byte padByte)
    {
        var remainder = data.Length % 4;
        if (remainder == 0)
        {
            return data;
        }

        var padCount = 4 - remainder;
        var padded = new byte[data.Length + padCount];
        data.CopyTo(padded, 0);
        for (var i = data.Length; i < padded.Length; i++)
        {
            padded[i] = padByte;
        }

        return padded;
    }
}
