using System.Numerics;

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

    private float _yaw;
    private float _pitch;

    public void SetAspectRatio(uint width, uint height)
    {
        if (height > 0)
        {
            AspectRatio = (float)width / height;
        }
    }

    public Vector3 Forward
    {
        get
        {
            var cosP = MathF.Cos(_pitch);
            return new Vector3(
                MathF.Sin(_yaw) * cosP,
                MathF.Sin(_pitch),
                MathF.Cos(_yaw) * cosP
            );
        }
    }

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

    public void SetYawPitch(float yaw, float pitch)
    {
        _yaw = yaw;
        _pitch = pitch;
        LocalRotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0);
    }

    public void LookAt(Vector3 target)
    {
        var direction = Vector3.Normalize(target - WorldPosition);

        _yaw = MathF.Atan2(direction.X, direction.Z);
        _pitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));

        LocalRotation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0);
    }
}
