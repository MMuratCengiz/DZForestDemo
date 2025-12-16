using System.Runtime.CompilerServices;

namespace ECS;

public readonly struct ArchetypeId : IEquatable<ArchetypeId>
{
    public readonly int Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ArchetypeId(int id)
    {
        Id = id;
    }

    public static ArchetypeId Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(-1);
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ArchetypeId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArchetypeId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ArchetypeId left, ArchetypeId right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ArchetypeId left, ArchetypeId right)
    {
        return !left.Equals(right);
    }
}

public readonly struct ArchetypeSignature : IEquatable<ArchetypeSignature>
{
    private readonly ComponentId[] _componentIds;
    private readonly int _hashCode;

    public ReadOnlySpan<ComponentId> ComponentIds
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _componentIds;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _componentIds?.Length ?? 0;
    }

    public ArchetypeSignature(ReadOnlySpan<ComponentId> componentIds)
    {
        _componentIds = componentIds.ToArray();
        Array.Sort(_componentIds);
        _hashCode = ComputeHash(_componentIds);
    }

    public ArchetypeSignature(params ComponentId[] componentIds)
    {
        _componentIds = (ComponentId[])componentIds.Clone();
        Array.Sort(_componentIds);
        _hashCode = ComputeHash(_componentIds);
    }

    private static int ComputeHash(ComponentId[] ids)
    {
        var hash = new HashCode();
        foreach (var id in ids)
        {
            hash.Add(id.Id);
        }

        return hash.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ComponentId componentId)
    {
        return Array.BinarySearch(_componentIds, componentId) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsAll(ReadOnlySpan<ComponentId> componentIds)
    {
        foreach (var id in componentIds)
        {
            if (!Contains(id))
            {
                return false;
            }
        }

        return true;
    }

    public ArchetypeSignature With(ComponentId componentId)
    {
        if (Contains(componentId))
        {
            return this;
        }

        var newIds = new ComponentId[_componentIds.Length + 1];
        _componentIds.CopyTo(newIds, 0);
        newIds[^1] = componentId;
        return new ArchetypeSignature(newIds);
    }

    public ArchetypeSignature Without(ComponentId componentId)
    {
        var index = Array.BinarySearch(_componentIds, componentId);
        if (index < 0)
        {
            return this;
        }

        var newIds = new ComponentId[_componentIds.Length - 1];
        Array.Copy(_componentIds, 0, newIds, 0, index);
        Array.Copy(_componentIds, index + 1, newIds, index, _componentIds.Length - index - 1);
        return new ArchetypeSignature(newIds);
    }

    public bool Equals(ArchetypeSignature other)
    {
        if (_hashCode != other._hashCode || _componentIds.Length != other._componentIds.Length)
        {
            return false;
        }

        for (var i = 0; i < _componentIds.Length; i++)
            if (_componentIds[i] != other._componentIds[i])
            {
                return false;
            }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArchetypeSignature other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ArchetypeSignature left, ArchetypeSignature right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ArchetypeSignature left, ArchetypeSignature right)
    {
        return !left.Equals(right);
    }
}