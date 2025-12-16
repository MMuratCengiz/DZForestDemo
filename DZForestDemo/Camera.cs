using System.Numerics;
using DenOfIz;

namespace DZForestDemo;

public class Camera(Vector3 position, Vector3 target)
{
    private bool _isDragging;
    private int _lastMouseX;
    private int _lastMouseY;

    public Vector3 Position { get; set; } = position;
    public Vector3 Target { get; set; } = target;
    public Vector3 Up { get; set; } = Vector3.UnitY;

    public float FieldOfView { get; set; } = MathF.PI / 4f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000f;
    public float AspectRatio { get; set; } = 16f / 9f;

    public float OrbitSensitivity { get; set; } = 0.005f;
    public float ZoomSensitivity { get; set; } = 0.5f;
    public float MinDistance { get; set; } = 1f;

    public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAtLeftHanded(Position, Target, Up);

    public Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
        FieldOfView, AspectRatio, NearPlane, FarPlane);

    public Matrix4x4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

    public void SetAspectRatio(uint width, uint height)
    {
        if (height > 0)
        {
            AspectRatio = (float)width / height;
        }
    }

    public bool HandleEvent(in Event ev)
    {
        switch (ev.Type)
        {
            case EventType.MouseButtonDown:
                if (ev.MouseButton.Button == MouseButton.Right)
                {
                    _isDragging = true;
                    _lastMouseX = (int)ev.MouseButton.X;
                    _lastMouseY = (int)ev.MouseButton.Y;
                    return true;
                }

                break;

            case EventType.MouseButtonUp:
                if (ev.MouseButton.Button == MouseButton.Right)
                {
                    _isDragging = false;
                    return true;
                }

                break;

            case EventType.MouseMotion:
                if (_isDragging)
                {
                    var deltaX = ev.MouseMotion.X - _lastMouseX;
                    var deltaY = ev.MouseMotion.Y - _lastMouseY;
                    _lastMouseX = (int)ev.MouseMotion.X;
                    _lastMouseY = (int)ev.MouseMotion.Y;

                    OrbitAroundTarget(-deltaX * OrbitSensitivity, -deltaY * OrbitSensitivity);
                    return true;
                }

                break;

            case EventType.MouseWheel:
                Zoom(ev.MouseWheel.Y * ZoomSensitivity);
                return true;
        }

        return false;
    }

    public void OrbitAroundTarget(float yawDelta, float pitchDelta)
    {
        var direction = Position - Target;
        var distance = direction.Length();
        var horizontalDist = MathF.Sqrt(direction.X * direction.X + direction.Z * direction.Z);
        var pitch = MathF.Atan2(direction.Y, horizontalDist);
        var yaw = MathF.Atan2(direction.X, direction.Z);
        yaw += yawDelta;
        pitch = Math.Clamp(pitch + pitchDelta, -MathF.PI / 2f + 0.1f, MathF.PI / 2f - 0.1f);
        var newHorizontalDist = distance * MathF.Cos(pitch);
        Position = new Vector3(
            Target.X + newHorizontalDist * MathF.Sin(yaw),
            Target.Y + distance * MathF.Sin(pitch),
            Target.Z + newHorizontalDist * MathF.Cos(yaw)
        );
    }

    public void Zoom(float delta)
    {
        var direction = Position - Target;
        var distance = direction.Length();
        var newDistance = Math.Max(MinDistance, distance - delta);
        Position = Target + Vector3.Normalize(direction) * newDistance;
    }
}