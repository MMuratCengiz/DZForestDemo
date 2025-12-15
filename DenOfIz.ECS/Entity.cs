using System.Runtime.CompilerServices;

namespace ECS;

public readonly struct Entity : IEquatable<Entity>
{
    public readonly uint Index;
    public readonly uint Generation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Entity(uint index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static Entity Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Entity other) => Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

    public override string ToString() => $"Entity({Index}v{Generation})";
}
