using System.Numerics;
using DenOfIz;
using NiziKit.Components;
using NiziKit.Physics;

namespace NiziKit.Core;

public class CameraObject(string name) : GameObject(name)
{
    public CameraObject() : this("Camera")
    {
    }

    public float FieldOfView { get; set; } = MathF.PI / 4f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000f;
    public float AspectRatio { get; set; } = 16f / 9f;

    private CameraController? _cachedController;

    public CameraController? Controller
    {
        get
        {
            _cachedController ??= GetComponent<CameraController>();
            return _cachedController;
        }
    }

    public void SetAspectRatio(uint width, uint height)
    {
        if (height > 0)
        {
            AspectRatio = (float)width / height;
        }
    }

    public Vector3 Forward => Controller!.Forward;

    public Vector3 Right
    {
        get
        {
            var right = Vector3.Transform(Vector3.UnitX, LocalRotation);
            return Vector3.Normalize(right);
        }
    }

    public Vector3 UpDirection
    {
        get
        {
            var up = Vector3.Transform(Vector3.UnitY, LocalRotation);
            return Vector3.Normalize(up);
        }
    }

    public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAtLeftHanded(WorldPosition, WorldPosition + Forward, UpDirection);

    public Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
        FieldOfView, AspectRatio, NearPlane, FarPlane);

    public Matrix4x4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

    public void Update(float deltaTime)
    {
        Controller?.Update(deltaTime);
    }

    public bool HandleEvent(in Event ev)
    {
        return Controller?.HandleEvent(in ev) ?? false;
    }

    public void LookAt(Vector3 target)
    {
        var direction = Vector3.Normalize(target - WorldPosition);
        var yaw = MathF.Atan2(direction.X, direction.Z);
        var pitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));
        LocalRotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0);
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

        return new Ray(nearWorld, farWorld - nearWorld);
    }

    public Vector3 ScreenToWorldPoint(float screenX, float screenY, float depth, float screenWidth, float screenHeight)
    {
        var ray = ScreenPointToRay(screenX, screenY, screenWidth, screenHeight);
        return ray.GetPoint(depth);
    }
}
