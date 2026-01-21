using System.Numerics;
using NiziKit.Core;
using NiziKit.Physics;

namespace NiziKit.Components;

[NiziComponent]
public partial class CameraComponent : ICameraProvider
{
    private float _aspectRatio = 16f / 9f;

    public partial ProjectionType ProjectionType { get; set; }
    public partial float FieldOfView { get; set; }
    public partial float OrthographicSize { get; set; }
    public partial float NearPlane { get; set; }
    public partial float FarPlane { get; set; }
    public partial int Priority { get; set; }
    public partial bool IsActiveCamera { get; set; }

    public CameraComponent()
    {
        ProjectionType = ProjectionType.Perspective;
        FieldOfView = MathF.PI / 4f;
        OrthographicSize = 5f;
        NearPlane = 0.1f;
        FarPlane = 1000f;
        Priority = 0;
        IsActiveCamera = true;
    }

    public float AspectRatio
    {
        get => _aspectRatio;
        set => _aspectRatio = value;
    }

    bool ICameraProvider.IsActive => IsActiveCamera && (Owner?.IsActive ?? false);

    public Vector3 WorldPosition => Owner?.WorldPosition ?? Vector3.Zero;

    public Vector3 Forward
    {
        get
        {
            if (Owner == null)
            {
                return Vector3.UnitZ;
            }

            var forward = Vector3.Transform(Vector3.UnitZ, Owner.LocalRotation);
            return Vector3.Normalize(forward);
        }
    }

    public Vector3 UpDirection
    {
        get
        {
            if (Owner == null)
            {
                return Vector3.UnitY;
            }

            var up = Vector3.Transform(Vector3.UnitY, Owner.LocalRotation);
            return Vector3.Normalize(up);
        }
    }

    public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAtLeftHanded(WorldPosition, WorldPosition + Forward, UpDirection);

    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            if (ProjectionType == ProjectionType.Orthographic)
            {
                var halfHeight = OrthographicSize;
                var halfWidth = halfHeight * AspectRatio;
                return Matrix4x4.CreateOrthographicOffCenterLeftHanded(
                    -halfWidth, halfWidth, -halfHeight, halfHeight, NearPlane, FarPlane);
            }
            return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(FieldOfView, AspectRatio, NearPlane, FarPlane);
        }
    }

    public Matrix4x4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

    public void SetAspectRatio(uint width, uint height)
    {
        if (height > 0)
        {
            _aspectRatio = (float)width / height;
        }
    }

    public Ray ScreenPointToRay(float screenX, float screenY, float screenWidth, float screenHeight)
    {
        var ndcX = (2f * screenX / screenWidth) - 1f;
        var ndcY = 1f - (2f * screenY / screenHeight);

        Matrix4x4.Invert(ViewProjectionMatrix, out var invViewProj);

        var nearPoint = Vector4.Transform(new Vector4(ndcX, ndcY, 0f, 1f), invViewProj);
        var farPoint = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), invViewProj);

        var nearWorld = new Vector3(nearPoint.X, nearPoint.Y, nearPoint.Z) / nearPoint.W;
        var farWorld = new Vector3(farPoint.X, farPoint.Y, farPoint.Z) / farPoint.W;

        return new Ray(nearWorld, Vector3.Normalize(farWorld - nearWorld));
    }

    public void Initialize()
    {
        Owner?.Scene?.RegisterCamera(this);
    }

    public void OnDestroy()
    {
        Owner?.Scene?.UnregisterCamera(this);
    }
}
