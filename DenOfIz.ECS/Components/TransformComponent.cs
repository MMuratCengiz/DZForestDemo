using System.Numerics;
using System.Runtime.CompilerServices;

namespace ECS.Components;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct Transform(Vector3 position, Quaternion rotation, Vector3 scale)
{
    public Vector3 Position = position;
    public Quaternion Rotation = rotation;
    public Vector3 Scale = scale;

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
    public Transform(Vector3 position) : this(position, Quaternion.Identity, Vector3.One)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform(Vector3 position, Quaternion rotation) : this(position, rotation, Vector3.One)
    {
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

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct LocalToWorld(Matrix4x4 matrix)
{
    public Matrix4x4 Matrix = matrix;
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct Velocity(Vector3 linear, Vector3 angular)
{
    public Vector3 Linear = linear;
    public Vector3 Angular = angular;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Velocity(Vector3 linear) : this(linear, Vector3.Zero)
    {
    }
}