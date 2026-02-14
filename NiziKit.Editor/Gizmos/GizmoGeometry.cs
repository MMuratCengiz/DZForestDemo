using System.Numerics;
using System.Runtime.InteropServices;
using NiziKit.Animation;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Editor.Gizmos;

[StructLayout(LayoutKind.Sequential)]
public struct GizmoVertex(Vector3 position, Vector4 color)
{
    public Vector3 Position = position;
    public Vector4 Color = color;
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

    private static void BuildArrow(List<GizmoVertex> vertices, Vector3 origin, Vector3 direction, float scale,
        Vector4 color)
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

    private static void BuildPlaneQuad(List<GizmoVertex> vertices, Vector3 origin, Vector3 axis1, Vector3 axis2,
        float scale, Vector4 color)
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

    private static void BuildRing(List<GizmoVertex> vertices, Vector3 origin, Vector3 normal, float scale,
        Vector4 color)
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

    private static void BuildScaleAxis(List<GizmoVertex> vertices, Vector3 origin, Vector3 direction, float scale,
        Vector4 color)
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

    private static void AddQuad(List<GizmoVertex> vertices, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
        Vector4 color)
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

    // Camera/Light icon colors
    public static readonly Vector4 CameraIconColor = new(0.3f, 0.5f, 0.9f, 1f);
    public static readonly Vector4 DirectionalLightIconColor = new(1.0f, 0.95f, 0.4f, 1f);
    public static readonly Vector4 PointLightIconColor = new(1.0f, 0.7f, 0.2f, 1f);
    public static readonly Vector4 SpotLightIconColor = new(0.9f, 0.6f, 0.2f, 1f);

    public static void BuildCameraIcon(List<GizmoVertex> vertices, Vector3 position, Quaternion rotation, float scale,
        Vector4 color)
    {
        var forward = Vector3.Transform(Vector3.UnitZ, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        var right = Vector3.Transform(Vector3.UnitX, rotation);

        var size = 0.3f * scale;
        var depth = 0.4f * scale;
        var aspect = 1.4f;

        // Camera body (box)
        var bodySize = size * 0.35f;
        var bodyDepth = size * 0.5f;
        var bodyCenter = position - forward * bodyDepth * 0.5f;

        // Body corners
        var b0 = bodyCenter + (-right * bodySize * aspect) + (up * bodySize) + (-forward * bodyDepth * 0.5f);
        var b1 = bodyCenter + (right * bodySize * aspect) + (up * bodySize) + (-forward * bodyDepth * 0.5f);
        var b2 = bodyCenter + (right * bodySize * aspect) + (-up * bodySize) + (-forward * bodyDepth * 0.5f);
        var b3 = bodyCenter + (-right * bodySize * aspect) + (-up * bodySize) + (-forward * bodyDepth * 0.5f);
        var b4 = bodyCenter + (-right * bodySize * aspect) + (up * bodySize) + (forward * bodyDepth * 0.5f);
        var b5 = bodyCenter + (right * bodySize * aspect) + (up * bodySize) + (forward * bodyDepth * 0.5f);
        var b6 = bodyCenter + (right * bodySize * aspect) + (-up * bodySize) + (forward * bodyDepth * 0.5f);
        var b7 = bodyCenter + (-right * bodySize * aspect) + (-up * bodySize) + (forward * bodyDepth * 0.5f);

        // Body faces
        AddQuad(vertices, b0, b1, b2, b3, color); // Back
        AddQuad(vertices, b5, b4, b7, b6, color); // Front
        AddQuad(vertices, b4, b0, b3, b7, color); // Left
        AddQuad(vertices, b1, b5, b6, b2, color); // Right
        AddQuad(vertices, b4, b5, b1, b0, color); // Top
        AddQuad(vertices, b3, b2, b6, b7, color); // Bottom

        // Lens (frustum extending forward)
        var lensStart = position + forward * size * 0.1f;
        var lensEnd = position + forward * depth;
        var lensStartSize = size * 0.25f;
        var lensEndSize = size * 0.6f;

        // Lens corners at start
        var l0 = lensStart + (-right * lensStartSize * aspect) + (up * lensStartSize);
        var l1 = lensStart + (right * lensStartSize * aspect) + (up * lensStartSize);
        var l2 = lensStart + (right * lensStartSize * aspect) + (-up * lensStartSize);
        var l3 = lensStart + (-right * lensStartSize * aspect) + (-up * lensStartSize);

        // Lens corners at end
        var f0 = lensEnd + (-right * lensEndSize * aspect) + (up * lensEndSize);
        var f1 = lensEnd + (right * lensEndSize * aspect) + (up * lensEndSize);
        var f2 = lensEnd + (right * lensEndSize * aspect) + (-up * lensEndSize);
        var f3 = lensEnd + (-right * lensEndSize * aspect) + (-up * lensEndSize);

        // Lens faces (frustum sides)
        AddQuad(vertices, l0, l1, f1, f0, color); // Top
        AddQuad(vertices, l2, l3, f3, f2, color); // Bottom
        AddQuad(vertices, l3, l0, f0, f3, color); // Left
        AddQuad(vertices, l1, l2, f2, f1, color); // Right
        AddQuad(vertices, f0, f1, f2, f3, color); // Far cap
    }

    public static void BuildDirectionalLightIcon(List<GizmoVertex> vertices, Vector3 position, Vector3 direction,
        float scale, Vector4 color)
    {
        var size = 0.25f * scale;
        var arrowLength = 0.5f * scale;

        var normal = Vector3.Normalize(direction);
        var tangent = GetPerpendicular(normal);
        var bitangent = Vector3.Cross(normal, tangent);

        // Filled disc (sun)
        const int segments = 16;
        for (var i = 0; i < segments; i++)
        {
            var angle1 = (float)i / segments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / segments * MathF.PI * 2;

            var p1 = position + (MathF.Cos(angle1) * tangent + MathF.Sin(angle1) * bitangent) * size;
            var p2 = position + (MathF.Cos(angle2) * tangent + MathF.Sin(angle2) * bitangent) * size;

            // Triangle fan for disc
            vertices.Add(new GizmoVertex(position, color));
            vertices.Add(new GizmoVertex(p1, color));
            vertices.Add(new GizmoVertex(p2, color));
        }

        // Arrow shaft (cylinder)
        var arrowStart = position + normal * size * 0.5f;
        var arrowEnd = position + normal * arrowLength;
        var shaftRadius = size * 0.15f;

        for (var i = 0; i < 8; i++)
        {
            var angle1 = (float)i / 8 * MathF.PI * 2;
            var angle2 = (float)(i + 1) / 8 * MathF.PI * 2;

            var offset1 = (MathF.Cos(angle1) * tangent + MathF.Sin(angle1) * bitangent) * shaftRadius;
            var offset2 = (MathF.Cos(angle2) * tangent + MathF.Sin(angle2) * bitangent) * shaftRadius;

            var s1 = arrowStart + offset1;
            var s2 = arrowStart + offset2;
            var e1 = arrowEnd + offset1;
            var e2 = arrowEnd + offset2;

            vertices.Add(new GizmoVertex(s1, color));
            vertices.Add(new GizmoVertex(s2, color));
            vertices.Add(new GizmoVertex(e1, color));

            vertices.Add(new GizmoVertex(s2, color));
            vertices.Add(new GizmoVertex(e2, color));
            vertices.Add(new GizmoVertex(e1, color));
        }

        // Arrow head (cone)
        var coneBase = arrowEnd;
        var coneTip = arrowEnd + normal * size * 0.6f;
        var coneRadius = size * 0.35f;

        for (var i = 0; i < 8; i++)
        {
            var angle1 = (float)i / 8 * MathF.PI * 2;
            var angle2 = (float)(i + 1) / 8 * MathF.PI * 2;

            var offset1 = (MathF.Cos(angle1) * tangent + MathF.Sin(angle1) * bitangent) * coneRadius;
            var offset2 = (MathF.Cos(angle2) * tangent + MathF.Sin(angle2) * bitangent) * coneRadius;

            var b1 = coneBase + offset1;
            var b2 = coneBase + offset2;

            // Cone side
            vertices.Add(new GizmoVertex(coneTip, color));
            vertices.Add(new GizmoVertex(b1, color));
            vertices.Add(new GizmoVertex(b2, color));

            // Base cap
            vertices.Add(new GizmoVertex(coneBase, color));
            vertices.Add(new GizmoVertex(b2, color));
            vertices.Add(new GizmoVertex(b1, color));
        }
    }

    public static void BuildPointLightIcon(List<GizmoVertex> vertices, Vector3 position, float scale, Vector4 color)
    {
        var size = 0.2f * scale;

        // Build an octahedron (diamond shape) for point light
        var top = position + Vector3.UnitY * size;
        var bottom = position - Vector3.UnitY * size;
        var front = position + Vector3.UnitZ * size;
        var back = position - Vector3.UnitZ * size;
        var left = position - Vector3.UnitX * size;
        var right = position + Vector3.UnitX * size;

        // Top pyramid (4 faces)
        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(front, color));
        vertices.Add(new GizmoVertex(right, color));

        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(right, color));
        vertices.Add(new GizmoVertex(back, color));

        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(back, color));
        vertices.Add(new GizmoVertex(left, color));

        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(left, color));
        vertices.Add(new GizmoVertex(front, color));

        // Bottom pyramid (4 faces)
        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(right, color));
        vertices.Add(new GizmoVertex(front, color));

        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(back, color));
        vertices.Add(new GizmoVertex(right, color));

        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(left, color));
        vertices.Add(new GizmoVertex(back, color));

        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(front, color));
        vertices.Add(new GizmoVertex(left, color));
    }

    public static void BuildSpotLightIcon(List<GizmoVertex> vertices, Vector3 position, Vector3 direction,
        float coneAngle, float scale, Vector4 color)
    {
        var depth = 0.5f * scale;
        var normal = Vector3.Normalize(direction);
        var tangent = GetPerpendicular(normal);
        var bitangent = Vector3.Cross(normal, tangent);

        // Cone radius at the far end based on angle (clamp to reasonable range)
        var effectiveAngle = Math.Clamp(coneAngle, 0.1f, 0.8f);
        var coneRadius = MathF.Tan(effectiveAngle) * depth;

        // Cone tip at position
        var coneEnd = position + normal * depth;

        // Build filled cone
        const int segments = 12;
        for (var i = 0; i < segments; i++)
        {
            var angle1 = (float)i / segments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / segments * MathF.PI * 2;

            var offset1 = (MathF.Cos(angle1) * tangent + MathF.Sin(angle1) * bitangent) * coneRadius;
            var offset2 = (MathF.Cos(angle2) * tangent + MathF.Sin(angle2) * bitangent) * coneRadius;

            var b1 = coneEnd + offset1;
            var b2 = coneEnd + offset2;

            // Cone side triangle
            vertices.Add(new GizmoVertex(position, color));
            vertices.Add(new GizmoVertex(b1, color));
            vertices.Add(new GizmoVertex(b2, color));

            // Base cap triangle (pointing inward)
            vertices.Add(new GizmoVertex(coneEnd, color));
            vertices.Add(new GizmoVertex(b2, color));
            vertices.Add(new GizmoVertex(b1, color));
        }

        // Small sphere at the tip
        var tipSize = 0.08f * scale;
        var tipTop = position - normal * tipSize;
        var tipFront = position + tangent * tipSize;
        var tipBack = position - tangent * tipSize;
        var tipLeft = position + bitangent * tipSize;
        var tipRight = position - bitangent * tipSize;

        // Mini octahedron at tip
        vertices.Add(new GizmoVertex(tipTop, color));
        vertices.Add(new GizmoVertex(tipFront, color));
        vertices.Add(new GizmoVertex(tipLeft, color));

        vertices.Add(new GizmoVertex(tipTop, color));
        vertices.Add(new GizmoVertex(tipLeft, color));
        vertices.Add(new GizmoVertex(tipBack, color));

        vertices.Add(new GizmoVertex(tipTop, color));
        vertices.Add(new GizmoVertex(tipBack, color));
        vertices.Add(new GizmoVertex(tipRight, color));

        vertices.Add(new GizmoVertex(tipTop, color));
        vertices.Add(new GizmoVertex(tipRight, color));
        vertices.Add(new GizmoVertex(tipFront, color));
    }

    public static readonly Vector4 ColliderWireframeColor = new(0.4f, 1.0f, 0.4f, 0.9f);
    private const int WireframeCircleSegments = 32;

    public static void BuildColliderWireframes(List<GizmoVertex> vertices, GameObject obj)
    {
        var collider = obj.GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        Matrix4x4.Decompose(obj.WorldMatrix, out _, out var rotation, out var translation);
        var poseMatrix = Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
        BuildColliderWireframe(vertices, collider, poseMatrix);
    }

    private static void BuildColliderWireframe(List<GizmoVertex> vertices, Collider collider, Matrix4x4 worldMatrix)
    {
        switch (collider)
        {
            case BoxCollider box:
                BuildBoxWireframe(vertices, box.Center, box.Size, worldMatrix, ColliderWireframeColor);
                break;
            case SphereCollider sphere:
                BuildSphereWireframe(vertices, sphere.Center, sphere.Radius, worldMatrix, ColliderWireframeColor);
                break;
            case CapsuleCollider capsule:
                BuildCapsuleWireframe(vertices, capsule.Center, capsule.Radius, capsule.Height, capsule.Direction,
                    worldMatrix, ColliderWireframeColor);
                break;
            case CylinderCollider cylinder:
                BuildCylinderWireframe(vertices, cylinder.Center, cylinder.Radius, cylinder.Height, cylinder.Direction,
                    worldMatrix, ColliderWireframeColor);
                break;
        }
    }

    public static void BuildBoxWireframe(List<GizmoVertex> vertices, Vector3 center, Vector3 size,
        Matrix4x4 worldMatrix, Vector4 color)
    {
        var h = size * 0.5f;

        Vector3[] corners =
        [
            new(-h.X, -h.Y, -h.Z),
            new(h.X, -h.Y, -h.Z),
            new(h.X, h.Y, -h.Z),
            new(-h.X, h.Y, -h.Z),
            new(-h.X, -h.Y, h.Z),
            new(h.X, -h.Y, h.Z),
            new(h.X, h.Y, h.Z),
            new(-h.X, h.Y, h.Z)
        ];

        for (var i = 0; i < 8; i++)
        {
            corners[i] = Vector3.Transform(corners[i] + center, worldMatrix);
        }

        int[] edges = [0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7];
        for (var i = 0; i < edges.Length; i++)
        {
            vertices.Add(new GizmoVertex(corners[edges[i]], color));
        }
    }

    public static void BuildSphereWireframe(List<GizmoVertex> vertices, Vector3 center, float radius,
        Matrix4x4 worldMatrix, Vector4 color)
    {
        BuildWireframeCircle(vertices, center, Vector3.UnitX, Vector3.UnitY, radius, worldMatrix, color);
        BuildWireframeCircle(vertices, center, Vector3.UnitX, Vector3.UnitZ, radius, worldMatrix, color);
        BuildWireframeCircle(vertices, center, Vector3.UnitY, Vector3.UnitZ, radius, worldMatrix, color);
    }

    public static void BuildCapsuleWireframe(List<GizmoVertex> vertices, Vector3 center, float radius, float height,
        ColliderDirection direction, Matrix4x4 worldMatrix, Vector4 color)
    {
        GetDirectionAxes(direction, out var up, out var right, out var forward);
        var cylinderHalf = MathF.Max(0, (height - radius * 2f) * 0.5f);

        var topCenter = center + up * cylinderHalf;
        var bottomCenter = center - up * cylinderHalf;

        BuildWireframeCircle(vertices, topCenter, right, forward, radius, worldMatrix, color);
        BuildWireframeCircle(vertices, bottomCenter, right, forward, radius, worldMatrix, color);

        const int lines = 4;
        for (var i = 0; i < lines; i++)
        {
            var angle = (float)i / lines * MathF.PI * 2;
            var offset = (MathF.Cos(angle) * right + MathF.Sin(angle) * forward) * radius;
            var p1 = Vector3.Transform(topCenter + offset, worldMatrix);
            var p2 = Vector3.Transform(bottomCenter + offset, worldMatrix);
            vertices.Add(new GizmoVertex(p1, color));
            vertices.Add(new GizmoVertex(p2, color));
        }

        BuildWireframeHalfCircle(vertices, topCenter, right, up, radius, worldMatrix, color);
        BuildWireframeHalfCircle(vertices, topCenter, forward, up, radius, worldMatrix, color);
        BuildWireframeHalfCircle(vertices, bottomCenter, right, -up, radius, worldMatrix, color);
        BuildWireframeHalfCircle(vertices, bottomCenter, forward, -up, radius, worldMatrix, color);
    }

    public static void BuildCylinderWireframe(List<GizmoVertex> vertices, Vector3 center, float radius, float height,
        ColliderDirection direction, Matrix4x4 worldMatrix, Vector4 color)
    {
        GetDirectionAxes(direction, out var up, out var right, out var forward);
        var halfHeight = height * 0.5f;

        var topCenter = center + up * halfHeight;
        var bottomCenter = center - up * halfHeight;

        BuildWireframeCircle(vertices, topCenter, right, forward, radius, worldMatrix, color);
        BuildWireframeCircle(vertices, bottomCenter, right, forward, radius, worldMatrix, color);

        const int lines = 8;
        for (var i = 0; i < lines; i++)
        {
            var angle = (float)i / lines * MathF.PI * 2;
            var offset = (MathF.Cos(angle) * right + MathF.Sin(angle) * forward) * radius;
            var p1 = Vector3.Transform(topCenter + offset, worldMatrix);
            var p2 = Vector3.Transform(bottomCenter + offset, worldMatrix);
            vertices.Add(new GizmoVertex(p1, color));
            vertices.Add(new GizmoVertex(p2, color));
        }
    }

    private static void BuildWireframeCircle(List<GizmoVertex> vertices, Vector3 center, Vector3 axis1, Vector3 axis2,
        float radius, Matrix4x4 worldMatrix, Vector4 color)
    {
        for (var i = 0; i < WireframeCircleSegments; i++)
        {
            var a1 = (float)i / WireframeCircleSegments * MathF.PI * 2;
            var a2 = (float)(i + 1) / WireframeCircleSegments * MathF.PI * 2;
            var p1 = center + (axis1 * MathF.Cos(a1) + axis2 * MathF.Sin(a1)) * radius;
            var p2 = center + (axis1 * MathF.Cos(a2) + axis2 * MathF.Sin(a2)) * radius;
            vertices.Add(new GizmoVertex(Vector3.Transform(p1, worldMatrix), color));
            vertices.Add(new GizmoVertex(Vector3.Transform(p2, worldMatrix), color));
        }
    }

    private static void BuildWireframeHalfCircle(List<GizmoVertex> vertices, Vector3 center, Vector3 tangent,
        Vector3 up, float radius, Matrix4x4 worldMatrix, Vector4 color)
    {
        var halfSegments = WireframeCircleSegments / 2;
        for (var i = 0; i < halfSegments; i++)
        {
            var a1 = (float)i / halfSegments * MathF.PI;
            var a2 = (float)(i + 1) / halfSegments * MathF.PI;
            var p1 = center + (tangent * MathF.Cos(a1) + up * MathF.Sin(a1)) * radius;
            var p2 = center + (tangent * MathF.Cos(a2) + up * MathF.Sin(a2)) * radius;
            vertices.Add(new GizmoVertex(Vector3.Transform(p1, worldMatrix), color));
            vertices.Add(new GizmoVertex(Vector3.Transform(p2, worldMatrix), color));
        }
    }

    private static void GetDirectionAxes(ColliderDirection direction, out Vector3 up, out Vector3 right,
        out Vector3 forward)
    {
        switch (direction)
        {
            case ColliderDirection.X:
                up = Vector3.UnitX;
                right = Vector3.UnitY;
                forward = Vector3.UnitZ;
                break;
            case ColliderDirection.Z:
                up = Vector3.UnitZ;
                right = Vector3.UnitX;
                forward = Vector3.UnitY;
                break;
            default:
                up = Vector3.UnitY;
                right = Vector3.UnitX;
                forward = Vector3.UnitZ;
                break;
        }
    }

    public static readonly Vector4 BoneColor = new(0.5f, 0.9f, 0.2f, 1f);
    public static readonly Vector4 JointColor = new(0.2f, 0.6f, 0.95f, 1f);
    public static readonly Vector4 RootJointColor = new(0.95f, 0.3f, 0.3f, 1f);
    private const float JointOctahedronSize = 0.015f;
    private const int BonePyramidSegments = 4;

    public static void BuildSkeletonOverlay(List<GizmoVertex> vertices, GameObject obj)
    {
        var animator = obj.GetComponent<Animator>();
        var skeleton = animator?.Skeleton;
        if (skeleton == null || skeleton.JointCount == 0)
        {
            return;
        }

        var worldMatrix = obj.WorldMatrix;
        Matrix4x4[] modelTransforms;
        if (animator is { IsInitialized: true, BoneCount: > 0 } &&
            (animator.IsPlaying || animator.IsPaused))
        {
            modelTransforms = animator.ModelSpaceTransforms.ToArray();
        }
        else
        {
            modelTransforms = skeleton.ComputeRestPose();
        }

        var jointCount = Math.Min(skeleton.JointCount, modelTransforms.Length);

        for (var i = 0; i < jointCount; i++)
        {
            var joint = skeleton.Joints[i];
            var jointPos = Vector3.Transform(modelTransforms[i].Translation, worldMatrix);
            var isRoot = joint.ParentIndex < 0;
            var jointColor = isRoot ? RootJointColor : JointColor;

            BuildOctahedron(vertices, jointPos, JointOctahedronSize, jointColor);
            if (joint.ParentIndex >= 0 && joint.ParentIndex < jointCount)
            {
                var parentPos = Vector3.Transform(modelTransforms[joint.ParentIndex].Translation, worldMatrix);
                BuildBonePyramid(vertices, parentPos, jointPos, BoneColor);
            }
        }
    }

    private static void BuildOctahedron(List<GizmoVertex> vertices, Vector3 center, float size, Vector4 color)
    {
        var top = center + Vector3.UnitY * size;
        var bottom = center - Vector3.UnitY * size;
        var front = center + Vector3.UnitZ * size;
        var back = center - Vector3.UnitZ * size;
        var left = center - Vector3.UnitX * size;
        var right = center + Vector3.UnitX * size;

        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(front, color));
        vertices.Add(new GizmoVertex(right, color));

        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(right, color));
        vertices.Add(new GizmoVertex(back, color));

        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(back, color));
        vertices.Add(new GizmoVertex(left, color));

        vertices.Add(new GizmoVertex(top, color));
        vertices.Add(new GizmoVertex(left, color));
        vertices.Add(new GizmoVertex(front, color));

        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(right, color));
        vertices.Add(new GizmoVertex(front, color));

        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(back, color));
        vertices.Add(new GizmoVertex(right, color));

        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(left, color));
        vertices.Add(new GizmoVertex(back, color));

        vertices.Add(new GizmoVertex(bottom, color));
        vertices.Add(new GizmoVertex(front, color));
        vertices.Add(new GizmoVertex(left, color));
    }

    private static void BuildBonePyramid(List<GizmoVertex> vertices, Vector3 from, Vector3 to, Vector4 color)
    {
        var dir = to - from;
        var length = dir.Length();
        if (length < 0.0001f)
        {
            return;
        }

        dir /= length;
        var width = length * 0.08f;
        var midPoint = from + dir * (length * 0.15f);

        var perp = GetPerpendicular(dir);
        var perp2 = Vector3.Cross(dir, perp);

        for (var i = 0; i < BonePyramidSegments; i++)
        {
            var angle1 = (float)i / BonePyramidSegments * MathF.PI * 2;
            var angle2 = (float)(i + 1) / BonePyramidSegments * MathF.PI * 2;

            var offset1 = (MathF.Cos(angle1) * perp + MathF.Sin(angle1) * perp2) * width;
            var offset2 = (MathF.Cos(angle2) * perp + MathF.Sin(angle2) * perp2) * width;

            var mid1 = midPoint + offset1;
            var mid2 = midPoint + offset2;

            vertices.Add(new GizmoVertex(from, color));
            vertices.Add(new GizmoVertex(mid1, color));
            vertices.Add(new GizmoVertex(mid2, color));

            vertices.Add(new GizmoVertex(to, color));
            vertices.Add(new GizmoVertex(mid2, color));
            vertices.Add(new GizmoVertex(mid1, color));
        }
    }
}
