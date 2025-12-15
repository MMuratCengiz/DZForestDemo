using System.Numerics;
using System.Runtime.CompilerServices;

namespace ECS.Components;

public struct Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static Transform Identity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new()
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform(Vector3 position)
    {
        Position = position;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform(Vector3 position, Quaternion rotation)
    {
        Position = position;
        Rotation = rotation;
        Scale = Vector3.One;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public Matrix4x4 Matrix
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Matrix4x4.CreateScale(Scale) *
               Matrix4x4.CreateFromQuaternion(Rotation) *
               Matrix4x4.CreateTranslation(Position);
    }

    public Vector3 Forward
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Vector3.Transform(-Vector3.UnitZ, Rotation);
    }

    public Vector3 Right
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Vector3.Transform(Vector3.UnitX, Rotation);
    }

    public Vector3 Up
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Vector3.Transform(Vector3.UnitY, Rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Translate(Vector3 offset)
    {
        Position += offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Rotate(Quaternion rotation)
    {
        Rotation = rotation * Rotation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LookAt(Vector3 target, Vector3 up)
    {
        var direction = Vector3.Normalize(target - Position);
        Rotation = QuaternionFromDirection(direction, up);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Quaternion QuaternionFromDirection(Vector3 forward, Vector3 up)
    {
        var matrix = Matrix4x4.CreateWorld(Vector3.Zero, forward, up);
        return Quaternion.CreateFromRotationMatrix(matrix);
    }
}

public struct LocalToWorld
{
    public Matrix4x4 Matrix;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LocalToWorld(Matrix4x4 matrix)
    {
        Matrix = matrix;
    }
}

public struct Velocity
{
    public Vector3 Linear;
    public Vector3 Angular;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Velocity(Vector3 linear)
    {
        Linear = linear;
        Angular = Vector3.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Velocity(Vector3 linear, Vector3 angular)
    {
        Linear = linear;
        Angular = angular;
    }
}
