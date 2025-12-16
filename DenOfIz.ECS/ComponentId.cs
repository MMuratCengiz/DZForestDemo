using System.Runtime.CompilerServices;

namespace ECS;

public readonly struct ComponentId : IEquatable<ComponentId>, IComparable<ComponentId>
{
    public readonly int Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ComponentId(int id)
    {
        Id = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ComponentId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ComponentId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ComponentId other)
    {
        return Id.CompareTo(other.Id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ComponentId left, ComponentId right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ComponentId left, ComponentId right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(ComponentId left, ComponentId right)
    {
        return left.Id < right.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(ComponentId left, ComponentId right)
    {
        return left.Id > right.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(ComponentId left, ComponentId right)
    {
        return left.Id <= right.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(ComponentId left, ComponentId right)
    {
        return left.Id >= right.Id;
    }
}

public static class ComponentRegistry
{
    private static readonly Dictionary<Type, ComponentId> TypeToId = new();
    private static readonly List<ComponentInfo> Components = [];
    private static int _nextId;

    public static int Count => Components.Count;

    public static ComponentId Register<T>() where T : struct
    {
        var type = typeof(T);
        if (TypeToId.TryGetValue(type, out var existing))
        {
            return existing;
        }

        var id = new ComponentId(_nextId++);
        TypeToId[type] = id;
        Components.Add(new ComponentInfo(type, Unsafe.SizeOf<T>()));
        return id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentId GetId<T>() where T : struct
    {
        return TypeToId.TryGetValue(typeof(T), out var id) ? id : Register<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetId<T>(out ComponentId id) where T : struct
    {
        return TypeToId.TryGetValue(typeof(T), out id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInfo GetInfo(ComponentId id)
    {
        return Components[id.Id];
    }
}

public readonly struct ComponentInfo
{
    public readonly Type Type;
    public readonly int Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ComponentInfo(Type type, int size)
    {
        Type = type;
        Size = size;
    }
}