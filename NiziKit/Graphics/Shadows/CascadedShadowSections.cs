using System.Numerics;

namespace NiziKit.Graphics.Shadows;

public class CascadedShadowSections
{
    private const int NearIndex = 0;
    private const int FarIndex = 4;

    private float _zMultiplier = 0.0f;
    private Matrix4x4 _inverseViewProjection;
    private Vector3 _center = Vector3.Zero;
    private Vector3 _directionToLight = Vector3.UnitY;
    private Matrix4x4 _cascadedLightView = Matrix4x4.Identity;
    private readonly Vector3[] _corners = new Vector3[8];

    public CascadedShadowSections(Matrix4x4 viewProjection, Vector3 lightDirection, float zMultiplier = 10.0f)
    {
        Update(viewProjection, lightDirection, zMultiplier);
    }

    public void Update(Matrix4x4 viewProjection, Vector3 lightDirection, float zMultiplier = 10.0f)
    {
        _zMultiplier = zMultiplier;
        _directionToLight = -1 * lightDirection;
        Matrix4x4.Invert(viewProjection, out _inverseViewProjection);
        ComputePlanes();

        ComputeCorners();
        ComputeCenter();
    }

    void ComputePlanes()
    {
    }

    void ComputeCenter()
    {
        _center = Vector3.Zero;
        foreach (var corner in _corners)
        {
            _center += corner;
        }

        _center /= _corners.Length;
        _cascadedLightView = Matrix4x4.CreateLookAtLeftHanded(_center + _directionToLight, _center, Vector3.UnitY);
    }

    void ComputeCorners()
    {
        ComputeCorners(NearIndex, 0);
        ComputeCorners(FarIndex, 1);
    }

    public Matrix4x4 CreateSubsections(int amount)
    {
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;
        var minZ = float.MaxValue;
        var maxZ = float.MinValue;
        foreach (var corner in _corners)
        {
            var trf = Vector4.Transform(new Vector4(corner.X, corner.Y, corner.Z, 1.0f), _cascadedLightView);
            minX = Math.Min(minX, trf.X);
            maxX = Math.Max(maxX, trf.X);
            minY = Math.Min(minY, trf.Y);
            maxY = Math.Max(maxY, trf.Y);
            minZ = Math.Min(minZ, trf.Z);
            maxZ = Math.Max(maxZ, trf.Z);
        }

        if (minZ < 0.0f)
        {
            minZ *= _zMultiplier;
        }
        else
        {
            minZ /= _zMultiplier;
        }

        if (maxZ < 0.0f)
        {
            maxZ /= _zMultiplier;
        }
        else
        {
            maxZ *= _zMultiplier;
        }

        var lightProjection = new Matrix4x4(
            2f / (maxX - minX), 0, 0, 0,
            0, 2f / (maxY - minY), 0, 0,
            0, 0, 1f / (maxZ - minZ), 0,
            -(maxX + minX) / (maxX - minX), -(maxY + minY) / (maxY - minY), -minZ / (maxZ - minZ), 1
        );
        return _cascadedLightView * lightProjection;
    }

    private static Vector3 PerspectiveDivide(Vector4 v) => new Vector3(v.X, v.Y, v.Z) / v.W;

    private Vector3 ComputeCorner(float x, float y, float z) =>
        PerspectiveDivide(Vector4.Transform(new Vector4(x, y, z, 1.0f), _inverseViewProjection));

    private void ComputeCorners(int cornerOffset, float depth)
    {
        _corners[cornerOffset + 0] = ComputeCorner(-1, 1, depth);
        _corners[cornerOffset + 1] = ComputeCorner(1, 1, depth);
        _corners[cornerOffset + 2] = ComputeCorner(-1, -1, depth);
        _corners[cornerOffset + 3] = ComputeCorner(1, -1, depth);
    }
}
