using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.Assets;

public static class GeometryMesh
{
    private const int GeometryVertexSize = 32;

    public static Mesh Box(float width, float height, float depth)
    {
        var desc = new BoxDesc
        {
            Width = width,
            Height = height,
            Depth = depth,
            BuildDesc = (uint)(BuildDesc.BuildNormal | BuildDesc.BuildTexcoord)
        };
        var geometry = Geometry.BuildBox(in desc);
        return FromGeometry(geometry, $"Box_{width}x{height}x{depth}");
    }

    public static Mesh Sphere(float diameter, uint tessellation = 16)
    {
        var desc = new SphereDesc
        {
            Diameter = diameter,
            Tessellation = tessellation,
            BuildDesc = (uint)(BuildDesc.BuildNormal | BuildDesc.BuildTexcoord)
        };
        var geometry = Geometry.BuildSphere(in desc);
        return FromGeometry(geometry, $"Sphere_{diameter}");
    }

    public static Mesh Quad(float width, float height)
    {
        var desc = new QuadDesc
        {
            Width = width,
            Height = height,
            BuildDesc = (uint)(BuildDesc.BuildNormal | BuildDesc.BuildTexcoord)
        };
        var geometry = Geometry.BuildQuadXY(in desc);
        return FromGeometry(geometry, $"Quad_{width}x{height}");
    }

    public static Mesh Cylinder(float diameter, float height, uint tessellation = 16)
    {
        var desc = new CylinderDesc
        {
            Diameter = diameter,
            Height = height,
            Tessellation = tessellation,
            BuildDesc = (uint)(BuildDesc.BuildNormal | BuildDesc.BuildTexcoord)
        };
        var geometry = Geometry.BuildCylinder(in desc);
        return FromGeometry(geometry, $"Cylinder_{diameter}x{height}");
    }

    public static Mesh Cone(float diameter, float height, uint tessellation = 16)
    {
        var desc = new ConeDesc
        {
            Diameter = diameter,
            Height = height,
            Tessellation = tessellation,
            BuildDesc = (uint)(BuildDesc.BuildNormal | BuildDesc.BuildTexcoord)
        };
        var geometry = Geometry.BuildCone(in desc);
        return FromGeometry(geometry, $"Cone_{diameter}x{height}");
    }

    public static Mesh Torus(float diameter, float thickness, uint tessellation = 16)
    {
        var desc = new TorusDesc
        {
            Diameter = diameter,
            Thickness = thickness,
            Tessellation = tessellation,
            BuildDesc = (uint)(BuildDesc.BuildNormal | BuildDesc.BuildTexcoord)
        };
        var geometry = Geometry.BuildTorus(in desc);
        return FromGeometry(geometry, $"Torus_{diameter}x{thickness}");
    }

    private static Mesh FromGeometry(GeometryData geometry, string name)
    {
        var vertexCount = (int)geometry.GetVertexCount();
        var indexCount = (int)geometry.GetIndexCount();

        var geometryVertexBytes = new byte[vertexCount * GeometryVertexSize];
        geometry.GetVertexData(geometryVertexBytes);

        var indexBytes = new byte[indexCount * sizeof(uint)];
        geometry.GetIndexData(indexBytes);
        var indices = new uint[indexCount];
        System.Buffer.BlockCopy(indexBytes, 0, indices, 0, indexBytes.Length);

        var format = VertexFormat.Static;
        var vertices = new byte[vertexCount * format.Stride];
        var vertSpan = vertices.AsSpan();
        var srcSpan = geometryVertexBytes.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var srcOffset = i * GeometryVertexSize;
            var dstOffset = i * format.Stride;

            var position = MemoryMarshal.Read<Vector3>(srcSpan[srcOffset..]);
            var normal = MemoryMarshal.Read<Vector3>(srcSpan[(srcOffset + 12)..]);
            var texCoord = MemoryMarshal.Read<Vector2>(srcSpan[(srcOffset + 24)..]);
            var tangent = ComputeTangent(normal);

            MemoryMarshal.Write(vertSpan[dstOffset..], in position);
            MemoryMarshal.Write(vertSpan[(dstOffset + 12)..], in normal);
            MemoryMarshal.Write(vertSpan[(dstOffset + 24)..], in texCoord);
            MemoryMarshal.Write(vertSpan[(dstOffset + 32)..], in tangent);
        }

        var bounds = ComputeBounds(vertices, format);

        return new Mesh
        {
            Name = name,
            Format = format,
            CpuVertices = vertices,
            CpuIndices = indices,
            Bounds = bounds
        };
    }

    private static Vector4 ComputeTangent(Vector3 normal)
    {
        var up = MathF.Abs(normal.Y) < 0.999f ? Vector3.UnitY : Vector3.UnitX;
        var tangent = Vector3.Normalize(Vector3.Cross(up, normal));
        return new Vector4(tangent, 1.0f);
    }

    private static BoundingBox ComputeBounds(byte[] vertices, VertexFormat format)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var vertexCount = vertices.Length / format.Stride;
        var span = vertices.AsSpan();

        for (var i = 0; i < vertexCount; i++)
        {
            var pos = MemoryMarshal.Read<Vector3>(span[(i * format.Stride)..]);
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        return new BoundingBox(min, max);
    }
}
