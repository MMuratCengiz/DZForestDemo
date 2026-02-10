using System.Numerics;
using System.Text;

namespace NiziKit.Assets.Serde;

public static class NiziMeshWriter
{
    private static readonly byte[] Magic = "NZMS"u8.ToArray();
    private const uint FormatVersion = 2;

    public static void Write(Stream stream, Mesh mesh)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        writer.Write(Magic);
        writer.Write(FormatVersion);

        WriteString(writer, mesh.Name);

        var bounds = mesh.Bounds;
        writer.Write(bounds.Min.X);
        writer.Write(bounds.Min.Y);
        writer.Write(bounds.Min.Z);
        writer.Write(bounds.Max.X);
        writer.Write(bounds.Max.Y);
        writer.Write(bounds.Max.Z);

        WriteMatrix(writer, mesh.NodeTransform);

        var hasIbm = mesh.InverseBindMatrices is { Length: > 0 };
        writer.Write(hasIbm);
        if (hasIbm)
        {
            writer.Write((uint)mesh.InverseBindMatrices!.Length);
            foreach (var mat in mesh.InverseBindMatrices)
            {
                WriteMatrix(writer, mat);
            }

            var hasJointNames = mesh.JointNames is { Length: > 0 };
            writer.Write(hasJointNames);
            if (hasJointNames)
            {
                writer.Write((uint)mesh.JointNames!.Length);
                foreach (var name in mesh.JointNames)
                {
                    WriteString(writer, name);
                }
            }
        }

        var attrs = mesh.SourceAttributes;
        if (attrs == null)
        {
            throw new InvalidOperationException("Mesh must have SourceAttributes to serialize to .nizimesh");
        }

        writer.Write((uint)attrs.Attributes.Count);
        foreach (var (key, attr) in attrs.Attributes)
        {
            WriteString(writer, attr.Name);
            writer.Write((byte)attr.Type);
            writer.Write((uint)attr.Data.Length);
            writer.Write(attr.Data);
        }

        writer.Write(attrs.VertexCount);
        writer.Write((uint)attrs.Indices.Length);
        foreach (var index in attrs.Indices)
        {
            writer.Write(index);
        }
    }

    public static byte[] WriteToBytes(Mesh mesh)
    {
        using var ms = new MemoryStream();
        Write(ms, mesh);
        return ms.ToArray();
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }

    private static void WriteMatrix(BinaryWriter writer, Matrix4x4 m)
    {
        writer.Write(m.M11); writer.Write(m.M12); writer.Write(m.M13); writer.Write(m.M14);
        writer.Write(m.M21); writer.Write(m.M22); writer.Write(m.M23); writer.Write(m.M24);
        writer.Write(m.M31); writer.Write(m.M32); writer.Write(m.M33); writer.Write(m.M34);
        writer.Write(m.M41); writer.Write(m.M42); writer.Write(m.M43); writer.Write(m.M44);
    }
}

public static class NiziMeshReader
{
    private static readonly byte[] ExpectedMagic = "NZMS"u8.ToArray();

    public static Mesh Read(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = reader.ReadBytes(4);
        if (!magic.AsSpan().SequenceEqual(ExpectedMagic))
        {
            throw new InvalidDataException("Invalid .nizimesh file: bad magic");
        }

        var version = reader.ReadUInt32();
        if (version != 2)
        {
            throw new InvalidDataException($"Unsupported .nizimesh version: {version} (expected 2, re-import required)");
        }

        var name = ReadString(reader);

        var bounds = new BoundingBox
        {
            Min = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
            Max = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
        };

        var nodeTransform = ReadMatrix(reader);

        Matrix4x4[]? inverseBindMatrices = null;
        string[]? jointNames = null;
        var hasIbm = reader.ReadBoolean();
        if (hasIbm)
        {
            var ibmCount = reader.ReadUInt32();
            inverseBindMatrices = new Matrix4x4[ibmCount];
            for (var i = 0; i < ibmCount; i++)
            {
                inverseBindMatrices[i] = ReadMatrix(reader);
            }

            var hasJointNames = reader.ReadBoolean();
            if (hasJointNames)
            {
                var jointCount = reader.ReadUInt32();
                jointNames = new string[jointCount];
                for (var i = 0; i < jointCount; i++)
                {
                    jointNames[i] = ReadString(reader);
                }
            }
        }

        var attrCount = reader.ReadUInt32();
        var attributes = new Dictionary<string, MeshAttributeData>((int)attrCount);
        for (var i = 0; i < attrCount; i++)
        {
            var attrName = ReadString(reader);
            var attrType = (VertexAttributeType)reader.ReadByte();
            var dataLen = reader.ReadUInt32();
            var data = reader.ReadBytes((int)dataLen);
            attributes[attrName] = new MeshAttributeData
            {
                Name = attrName,
                Type = attrType,
                Data = data
            };
        }

        var vertexCount = reader.ReadInt32();
        var indexCount = reader.ReadUInt32();
        var indices = new uint[indexCount];
        for (var i = 0; i < indexCount; i++)
        {
            indices[i] = reader.ReadUInt32();
        }

        var sourceAttributes = new MeshAttributeSet
        {
            Attributes = attributes,
            VertexCount = vertexCount,
            Indices = indices
        };

        return new Mesh
        {
            Name = name,
            Bounds = bounds,
            NodeTransform = nodeTransform,
            InverseBindMatrices = inverseBindMatrices,
            JointNames = jointNames,
            SourceAttributes = sourceAttributes
        };
    }

    public static Mesh ReadFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        return Read(ms);
    }

    private static string ReadString(BinaryReader reader)
    {
        var len = reader.ReadUInt16();
        var bytes = reader.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }

    private static Matrix4x4 ReadMatrix(BinaryReader reader)
    {
        return new Matrix4x4(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
        );
    }
}
