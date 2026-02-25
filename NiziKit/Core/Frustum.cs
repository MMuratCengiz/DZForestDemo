using System.Numerics;

namespace NiziKit.Core;

public class Frustum
{
    public struct FrustumFace
    {
        public Vector3 TopLeft;
        public Vector3 TopRight;
        public Vector3 BottomLeft;
        public Vector3 BottomRight;
    }

    private Matrix4x4 _viewProjection;
    private Matrix4x4 _inverseViewProjection;

    private Plane _top;
    private Plane _bottom;
    private Plane _left;
    private Plane _right;
    private Plane _near;
    private Plane _far;

    public FrustumFace Near { get; private set; }
    public FrustumFace Far { get; private set; }

    public Frustum(Matrix4x4 viewProjection)
    {
        Update(viewProjection);
    }

    public void Update(Matrix4x4 viewProjection)
    {
        _viewProjection = viewProjection;
        Matrix4x4.Invert(viewProjection, out _inverseViewProjection);
        ComputePlanes();
        ComputeCorners();
    }

    void ComputePlanes()
    {
    }

    void ComputeCorners()
    {
        Near = ComputeCorners(0);
        Far = ComputeCorners(1);
    }

    private static Vector3 PerspectiveDivide(Vector4 v) => new Vector3(v.X, v.Y, v.Z) / v.W;

    private Vector3 ComputeCorner(float x, float y, float z) =>
        PerspectiveDivide(Vector4.Transform(new Vector4(x, y, z, 1.0f), _inverseViewProjection));

    private FrustumFace ComputeCorners(float depth)
    {
        var quad = new FrustumFace
        {
            TopLeft = ComputeCorner(-1, 1, depth),
            TopRight = ComputeCorner(1, 1, depth),
            BottomLeft = ComputeCorner(-1, -1, depth),
            BottomRight = ComputeCorner(1, -1, depth)
        };
        return quad;
    }
}
