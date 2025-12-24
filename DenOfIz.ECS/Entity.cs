using System.Runtime.CompilerServices;

namespace ECS;

public readonly struct Entity : IEquatable<Entity>
{
    private readonly Handle<EntityTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Entity(uint index, uint generation)
    {
        _handle = new Handle<EntityTag>(index, generation);
    }

    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.Index;
    }

    public uint Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.Generation;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsValid;
    }

    public static Entity Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Entity other)
    {
        return _handle.Equals(other._handle);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return _handle.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Entity left, Entity right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"Entity({Index}v{Generation})";
    }
}
