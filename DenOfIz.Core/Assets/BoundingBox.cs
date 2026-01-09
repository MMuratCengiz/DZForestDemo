using System.Numerics;

namespace DenOfIz.World.Assets;

public struct BoundingBox
{
    public Vector3 Min;
    public Vector3 Max;

    public readonly Vector3 Center => (Min + Max) * 0.5f;
    public readonly Vector3 Size => Max - Min;

    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public static BoundingBox FromVertices(ReadOnlySpan<Vertex> vertices)
    {
        if (vertices.Length == 0)
            return default;

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }

        return new BoundingBox(min, max);
    }
}
