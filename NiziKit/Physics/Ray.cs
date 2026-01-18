using System.Numerics;

namespace NiziKit.Physics;

public readonly struct Ray(Vector3 origin, Vector3 direction)
{
    public Vector3 Origin { get; } = origin;
    public Vector3 Direction { get; } = Vector3.Normalize(direction);

    public Vector3 GetPoint(float distance) => Origin + Direction * distance;
}

public struct RaycastHit
{
    public Vector3 Point;
    public Vector3 Normal;
    public float Distance;
    public int GameObjectId;
    public bool IsStatic;
}
