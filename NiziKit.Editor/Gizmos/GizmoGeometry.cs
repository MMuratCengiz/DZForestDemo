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

    private const float AxisLength = 1.2f;
    private const float ArrowHeadLength = 0.18f;
    private const float ArrowHeadRadius = 0.06f;
    private const float ShaftRadius = 0.015f;
    private const float PlaneSize = 0.3f;
    private const float PlaneOffset = 0.25f;
    private const float RingRadius = 1.0f;
    private const float RingThickness = 0.02f;
    private const int RingSegments = 48;
    private const int ConeSegments = 10;
    private const int TubeSegments = 6;
    private const float ScaleBoxSize = 0.08f;
    private const float CenterBoxSize = 0.12f;

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

    public static void BuildTranslateGizmoLocal(
        List<GizmoVertex> vertices,
        GizmoAxis hoveredAxis,
        GizmoAxis activeAxis)
    {
        BuildTranslateGizmo(vertices, Vector3.Zero, Quaternion.Identity, 1.0f, hoveredAxis, activeAxis);
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

    public static void BuildRotateGizmoLocal(
        List<GizmoVertex> vertices,
        GizmoAxis hoveredAxis,
        GizmoAxis activeAxis)
    {
        BuildRotateGizmo(vertices, Vector3.Zero, Quaternion.Identity, 1.0f, hoveredAxis, activeAxis);
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

        BuildFilledBox(vertices, origin, CenterBoxSize * scale, GetAxisColor(GizmoAxis.All, hoveredAxis, activeAxis));
    }

    public static void BuildScaleGizmoLocal(
        List<GizmoVertex> vertices,
        GizmoAxis hoveredAxis,
        GizmoAxis activeAxis)
    {
        BuildScaleGizmo(vertices, Vector3.Zero, Quaternion.Identity, 1.0f, hoveredAxis, activeAxis);
    }

    private static void BuildArrow(List<GizmoVertex> vertices, Vector3 origin, Vector3 direction, float scale, Vector4 color)
    {
        var end = origin + direction * AxisLength * scale;
        var arrowBase = origin + direction * (AxisLength - ArrowHeadLength) * scale;

        var perpendicular = GetPerpendicular(direction);
        var perpendicular2 = Vector3.Cross(direction, perpendicular);

        // Build shaft (cylinder)
        BuildCylinder(vertices, origin, arrowBase, ShaftRadius * scale, perpendicular, perpendicular2, color);

        // Build cone head
        BuildCone(vertices, arrowBase, end, ArrowHeadRadius * scale, perpendicular, perpendicular2, color);
    }

    private static void BuildCylinder(List<GizmoVertex> vertices, Vector3 start, Vector3 end, float radius,
        Vector3 perpendicular, Vector3 perpendicular2, Vector4 color)
    {
        for (var i = 0; i < ConeSegments; i++)
        {
            var angle1 = (float)i / ConeSegments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / ConeSegments * MathF.PI * 2;

            var offset1 = (MathF.Cos(angle1) * perpendicular + MathF.Sin(angle1) * perpendicular2) * radius;
            var offset2 = (MathF.Cos(angle2) * perpendicular + MathF.Sin(angle2) * perpendicular2) * radius;

            var p1 = start + offset1;
            var p2 = start + offset2;
            var p3 = end + offset1;
            var p4 = end + offset2;

            // Two triangles per quad
            vertices.Add(new GizmoVertex(p1, color));
            vertices.Add(new GizmoVertex(p2, color));
            vertices.Add(new GizmoVertex(p3, color));

            vertices.Add(new GizmoVertex(p2, color));
            vertices.Add(new GizmoVertex(p4, color));
            vertices.Add(new GizmoVertex(p3, color));
        }
    }

    private static void BuildCone(List<GizmoVertex> vertices, Vector3 baseCenter, Vector3 tip, float radius,
        Vector3 perpendicular, Vector3 perpendicular2, Vector4 color)
    {
        for (var i = 0; i < ConeSegments; i++)
        {
            var angle1 = (float)i / ConeSegments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / ConeSegments * MathF.PI * 2;

            var offset1 = (MathF.Cos(angle1) * perpendicular + MathF.Sin(angle1) * perpendicular2) * radius;
            var offset2 = (MathF.Cos(angle2) * perpendicular + MathF.Sin(angle2) * perpendicular2) * radius;

            var p1 = baseCenter + offset1;
            var p2 = baseCenter + offset2;

            // Cone side
            vertices.Add(new GizmoVertex(tip, color));
            vertices.Add(new GizmoVertex(p1, color));
            vertices.Add(new GizmoVertex(p2, color));

            // Base cap
            vertices.Add(new GizmoVertex(baseCenter, color));
            vertices.Add(new GizmoVertex(p2, color));
            vertices.Add(new GizmoVertex(p1, color));
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

        // Two triangles for the filled quad
        vertices.Add(new GizmoVertex(p0, color));
        vertices.Add(new GizmoVertex(p1, color));
        vertices.Add(new GizmoVertex(p2, color));

        vertices.Add(new GizmoVertex(p0, color));
        vertices.Add(new GizmoVertex(p2, color));
        vertices.Add(new GizmoVertex(p3, color));
    }

    private static void BuildRing(List<GizmoVertex> vertices, Vector3 origin, Vector3 normal, float scale, Vector4 color)
    {
        var radius = RingRadius * scale;
        var tubeRadius = RingThickness * scale;

        var tangent = GetPerpendicular(normal);
        var bitangent = Vector3.Cross(normal, tangent);

        for (var i = 0; i < RingSegments; i++)
        {
            var angle1 = (float)i / RingSegments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / RingSegments * MathF.PI * 2;

            var center1 = origin + (MathF.Cos(angle1) * tangent + MathF.Sin(angle1) * bitangent) * radius;
            var center2 = origin + (MathF.Cos(angle2) * tangent + MathF.Sin(angle2) * bitangent) * radius;

            var toCenter1 = Vector3.Normalize(center1 - origin);
            var toCenter2 = Vector3.Normalize(center2 - origin);

            for (var j = 0; j < TubeSegments; j++)
            {
                var tubeAngle1 = (float)j / TubeSegments * MathF.PI * 2;
                var tubeAngle2 = (float)(j + 1) / TubeSegments * MathF.PI * 2;

                var offset1a = (MathF.Cos(tubeAngle1) * toCenter1 + MathF.Sin(tubeAngle1) * normal) * tubeRadius;
                var offset1b = (MathF.Cos(tubeAngle2) * toCenter1 + MathF.Sin(tubeAngle2) * normal) * tubeRadius;
                var offset2a = (MathF.Cos(tubeAngle1) * toCenter2 + MathF.Sin(tubeAngle1) * normal) * tubeRadius;
                var offset2b = (MathF.Cos(tubeAngle2) * toCenter2 + MathF.Sin(tubeAngle2) * normal) * tubeRadius;

                var p1 = center1 + offset1a;
                var p2 = center1 + offset1b;
                var p3 = center2 + offset2a;
                var p4 = center2 + offset2b;

                vertices.Add(new GizmoVertex(p1, color));
                vertices.Add(new GizmoVertex(p2, color));
                vertices.Add(new GizmoVertex(p3, color));

                vertices.Add(new GizmoVertex(p2, color));
                vertices.Add(new GizmoVertex(p4, color));
                vertices.Add(new GizmoVertex(p3, color));
            }
        }
    }

    private static void BuildScaleAxis(List<GizmoVertex> vertices, Vector3 origin, Vector3 direction, float scale, Vector4 color)
    {
        var end = origin + direction * AxisLength * scale;

        var perpendicular = GetPerpendicular(direction);
        var perpendicular2 = Vector3.Cross(direction, perpendicular);

        // Build shaft
        BuildCylinder(vertices, origin, end, ShaftRadius * scale, perpendicular, perpendicular2, color);

        // Build box at end
        BuildFilledBox(vertices, end, ScaleBoxSize * scale, color);
    }

    private static void BuildFilledBox(List<GizmoVertex> vertices, Vector3 center, float size, Vector4 color)
    {
        var h = size * 0.5f;

        Vector3[] corners =
        [
            center + new Vector3(-h, -h, -h),
            center + new Vector3(h, -h, -h),
            center + new Vector3(h, h, -h),
            center + new Vector3(-h, h, -h),
            center + new Vector3(-h, -h, h),
            center + new Vector3(h, -h, h),
            center + new Vector3(h, h, h),
            center + new Vector3(-h, h, h)
        ];

        // 6 faces, 2 triangles each
        // Front face (z-)
        AddQuad(vertices, corners[0], corners[1], corners[2], corners[3], color);
        // Back face (z+)
        AddQuad(vertices, corners[5], corners[4], corners[7], corners[6], color);
        // Left face (x-)
        AddQuad(vertices, corners[4], corners[0], corners[3], corners[7], color);
        // Right face (x+)
        AddQuad(vertices, corners[1], corners[5], corners[6], corners[2], color);
        // Bottom face (y-)
        AddQuad(vertices, corners[4], corners[5], corners[1], corners[0], color);
        // Top face (y+)
        AddQuad(vertices, corners[3], corners[2], corners[6], corners[7], color);
    }

    private static void AddQuad(List<GizmoVertex> vertices, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector4 color)
    {
        vertices.Add(new GizmoVertex(p0, color));
        vertices.Add(new GizmoVertex(p1, color));
        vertices.Add(new GizmoVertex(p2, color));

        vertices.Add(new GizmoVertex(p0, color));
        vertices.Add(new GizmoVertex(p2, color));
        vertices.Add(new GizmoVertex(p3, color));
    }

    private static readonly int[] BoxEdges =
    [
        0, 1, 1, 2, 2, 3, 3, 0,
        4, 5, 5, 6, 6, 7, 7, 4,
        0, 4, 1, 5, 2, 6, 3, 7
    ];

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
