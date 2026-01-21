using System.Numerics;
using System.Runtime.InteropServices;
using NiziKit.Assets;

namespace NiziKit.Editor.Gizmos;

[StructLayout(LayoutKind.Sequential)]
public struct GizmoVertex
{
    public Vector3 Position;
    public Vector4 Color;

    public GizmoVertex(Vector3 position, Vector4 color)
    {
        Position = position;
        Color = color;
    }
}

public static class GizmoGeometry
{
    public static readonly Vector4 AxisColorX = new(0.9f, 0.2f, 0.2f, 1f);
    public static readonly Vector4 AxisColorY = new(0.2f, 0.9f, 0.2f, 1f);
    public static readonly Vector4 AxisColorZ = new(0.2f, 0.4f, 0.9f, 1f);
    public static readonly Vector4 AxisColorXY = new(0.9f, 0.9f, 0.2f, 0.6f);
    public static readonly Vector4 AxisColorXZ = new(0.9f, 0.2f, 0.9f, 0.6f);
    public static readonly Vector4 AxisColorYZ = new(0.2f, 0.9f, 0.9f, 0.6f);
    public static readonly Vector4 HighlightColor = new(1f, 1f, 0f, 1f);
    public static readonly Vector4 SelectionBoxColor = new(1f, 0.6f, 0.1f, 1f);
    public static readonly Vector4 WhiteColor = new(0.9f, 0.9f, 0.9f, 1f);

    private const float AxisLength = 1.5f;
    private const float ArrowHeadLength = 0.2f;
    private const float ArrowHeadRadius = 0.08f;
    private const float PlaneSize = 0.4f;
    private const float PlaneOffset = 0.3f;
    private const float RingRadius = 1.2f;
    private const int RingSegments = 64;
    private const float ScaleBoxSize = 0.15f;
    private const float CenterBoxSize = 0.2f;

    private static readonly int[] BoxEdges =
    [
        0, 1, 1, 2, 2, 3, 3, 0,
        4, 5, 5, 6, 6, 7, 7, 4,
        0, 4, 1, 5, 2, 6, 3, 7
    ];

    public static Vector4 GetAxisColor(GizmoAxis axis, GizmoAxis hoveredAxis, GizmoAxis activeAxis)
    {
        var isActive = axis == activeAxis && activeAxis != GizmoAxis.None;
        var isHovered = axis == hoveredAxis && hoveredAxis != GizmoAxis.None && activeAxis == GizmoAxis.None;

        if (isActive || isHovered)
        {
            return HighlightColor;
        }

        return axis switch
        {
            GizmoAxis.X => AxisColorX,
            GizmoAxis.Y => AxisColorY,
            GizmoAxis.Z => AxisColorZ,
            GizmoAxis.XY => AxisColorXY,
            GizmoAxis.XZ => AxisColorXZ,
            GizmoAxis.YZ => AxisColorYZ,
            GizmoAxis.All => WhiteColor,
            _ => WhiteColor
        };
    }

    public static void BuildTranslateGizmo(
        List<GizmoVertex> vertices,
        Vector3 origin,
        Quaternion rotation,
        float scale,
        GizmoAxis hoveredAxis,
        GizmoAxis activeAxis)
    {
        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        BuildArrow(vertices, origin, axisX, scale, GetAxisColor(GizmoAxis.X, hoveredAxis, activeAxis));
        BuildArrow(vertices, origin, axisY, scale, GetAxisColor(GizmoAxis.Y, hoveredAxis, activeAxis));
        BuildArrow(vertices, origin, axisZ, scale, GetAxisColor(GizmoAxis.Z, hoveredAxis, activeAxis));

        BuildPlaneQuad(vertices, origin, axisX, axisY, scale, GetAxisColor(GizmoAxis.XY, hoveredAxis, activeAxis));
        BuildPlaneQuad(vertices, origin, axisX, axisZ, scale, GetAxisColor(GizmoAxis.XZ, hoveredAxis, activeAxis));
        BuildPlaneQuad(vertices, origin, axisY, axisZ, scale, GetAxisColor(GizmoAxis.YZ, hoveredAxis, activeAxis));
    }

    public static void BuildRotateGizmo(
        List<GizmoVertex> vertices,
        Vector3 origin,
        Quaternion rotation,
        float scale,
        GizmoAxis hoveredAxis,
        GizmoAxis activeAxis)
    {
        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        BuildRing(vertices, origin, axisX, scale, GetAxisColor(GizmoAxis.X, hoveredAxis, activeAxis));
        BuildRing(vertices, origin, axisY, scale, GetAxisColor(GizmoAxis.Y, hoveredAxis, activeAxis));
        BuildRing(vertices, origin, axisZ, scale, GetAxisColor(GizmoAxis.Z, hoveredAxis, activeAxis));
    }

    public static void BuildScaleGizmo(
        List<GizmoVertex> vertices,
        Vector3 origin,
        Quaternion rotation,
        float scale,
        GizmoAxis hoveredAxis,
        GizmoAxis activeAxis)
    {
        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        BuildScaleAxis(vertices, origin, axisX, scale, GetAxisColor(GizmoAxis.X, hoveredAxis, activeAxis));
        BuildScaleAxis(vertices, origin, axisY, scale, GetAxisColor(GizmoAxis.Y, hoveredAxis, activeAxis));
        BuildScaleAxis(vertices, origin, axisZ, scale, GetAxisColor(GizmoAxis.Z, hoveredAxis, activeAxis));

        BuildBox(vertices, origin, CenterBoxSize * scale, GetAxisColor(GizmoAxis.All, hoveredAxis, activeAxis));
    }

    private static void BuildArrow(List<GizmoVertex> vertices, Vector3 origin, Vector3 direction, float scale, Vector4 color)
    {
        var end = origin + direction * AxisLength * scale;
        var arrowBase = origin + direction * (AxisLength - ArrowHeadLength) * scale;

        vertices.Add(new GizmoVertex(origin, color));
        vertices.Add(new GizmoVertex(end, color));

        var perpendicular = GetPerpendicular(direction);
        var perpendicular2 = Vector3.Cross(direction, perpendicular);

        var coneSegments = 8;
        for (var i = 0; i < coneSegments; i++)
        {
            var angle1 = (float)i / coneSegments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / coneSegments * MathF.PI * 2;

            var offset1 = (MathF.Cos(angle1) * perpendicular + MathF.Sin(angle1) * perpendicular2) * ArrowHeadRadius * scale;
            var offset2 = (MathF.Cos(angle2) * perpendicular + MathF.Sin(angle2) * perpendicular2) * ArrowHeadRadius * scale;

            vertices.Add(new GizmoVertex(end, color));
            vertices.Add(new GizmoVertex(arrowBase + offset1, color));

            vertices.Add(new GizmoVertex(arrowBase + offset1, color));
            vertices.Add(new GizmoVertex(arrowBase + offset2, color));

            vertices.Add(new GizmoVertex(arrowBase + offset1, color));
            vertices.Add(new GizmoVertex(arrowBase, color));
        }
    }

    private static void BuildPlaneQuad(List<GizmoVertex> vertices, Vector3 origin, Vector3 axis1, Vector3 axis2, float scale, Vector4 color)
    {
        var offset = PlaneOffset * scale;
        var size = PlaneSize * scale;

        var p0 = origin + axis1 * offset + axis2 * offset;
        var p1 = origin + axis1 * (offset + size) + axis2 * offset;
        var p2 = origin + axis1 * (offset + size) + axis2 * (offset + size);
        var p3 = origin + axis1 * offset + axis2 * (offset + size);

        vertices.Add(new GizmoVertex(p0, color));
        vertices.Add(new GizmoVertex(p1, color));
        vertices.Add(new GizmoVertex(p1, color));
        vertices.Add(new GizmoVertex(p2, color));
        vertices.Add(new GizmoVertex(p2, color));
        vertices.Add(new GizmoVertex(p3, color));
        vertices.Add(new GizmoVertex(p3, color));
        vertices.Add(new GizmoVertex(p0, color));

        vertices.Add(new GizmoVertex(p0, color));
        vertices.Add(new GizmoVertex(p2, color));
    }

    private static void BuildRing(List<GizmoVertex> vertices, Vector3 origin, Vector3 normal, float scale, Vector4 color)
    {
        var radius = RingRadius * scale;

        var tangent = GetPerpendicular(normal);
        var bitangent = Vector3.Cross(normal, tangent);

        for (var i = 0; i < RingSegments; i++)
        {
            var angle1 = (float)i / RingSegments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / RingSegments * MathF.PI * 2;

            var p1 = origin + (MathF.Cos(angle1) * tangent + MathF.Sin(angle1) * bitangent) * radius;
            var p2 = origin + (MathF.Cos(angle2) * tangent + MathF.Sin(angle2) * bitangent) * radius;

            vertices.Add(new GizmoVertex(p1, color));
            vertices.Add(new GizmoVertex(p2, color));
        }
    }

    private static void BuildScaleAxis(List<GizmoVertex> vertices, Vector3 origin, Vector3 direction, float scale, Vector4 color)
    {
        var end = origin + direction * AxisLength * scale;

        vertices.Add(new GizmoVertex(origin, color));
        vertices.Add(new GizmoVertex(end, color));

        BuildBox(vertices, end, ScaleBoxSize * scale, color);
    }

    private static void BuildBox(List<GizmoVertex> vertices, Vector3 center, float size, Vector4 color)
    {
        var halfSize = size * 0.5f;

        Vector3[] corners =
        [
            center + new Vector3(-halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, halfSize, halfSize),
            center + new Vector3(-halfSize, halfSize, halfSize)
        ];

        for (var i = 0; i < BoxEdges.Length; i += 2)
        {
            vertices.Add(new GizmoVertex(corners[BoxEdges[i]], color));
            vertices.Add(new GizmoVertex(corners[BoxEdges[i + 1]], color));
        }
    }

    public static GizmoVertex[] BuildSelectionBox(BoundingBox bounds, Matrix4x4 worldMatrix, Vector4 color)
    {
        var min = bounds.Min;
        var max = bounds.Max;

        Vector3[] corners =
        [
            new Vector3(min.X, min.Y, min.Z),
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, max.Z),
            new Vector3(max.X, max.Y, max.Z),
            new Vector3(min.X, max.Y, max.Z)
        ];

        for (var i = 0; i < 8; i++)
        {
            corners[i] = Vector3.Transform(corners[i], worldMatrix);
        }

        var verts = new GizmoVertex[24];
        for (var i = 0; i < 24; i++)
        {
            verts[i] = new GizmoVertex(corners[BoxEdges[i]], color);
        }

        return verts;
    }

    private static Vector3 GetPerpendicular(Vector3 v)
    {
        if (MathF.Abs(v.X) < 0.9f)
        {
            return Vector3.Normalize(Vector3.Cross(v, Vector3.UnitX));
        }
        return Vector3.Normalize(Vector3.Cross(v, Vector3.UnitY));
    }
}
