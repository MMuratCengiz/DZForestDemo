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

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id >= 0;
    }

    public static ComponentId Invalid => new(-1);

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

/// <summary>
/// Arch ECS-style static component type cache.
/// Accessing Component&lt;T&gt;.Id is a direct field read - no dictionary lookup!
/// </summary>
public static class Component<T> where T : struct
{
    /// <summary>
    /// The unique ID for this component type. Cached statically per-type.
    /// </summary>
    public static readonly ComponentId Id = ComponentRegistry.Register<T>();
}

public static class ComponentRegistry
{
    private static readonly Dictionary<Type, ComponentId> TypeToId = new();
    private static readonly List<ComponentInfo> Components = [];
    private static readonly object Lock = new();

    /// <summary>
    /// Maximum component ID currently registered. Used for sizing sparse arrays.
    /// </summary>
    public static int MaxId
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public static int Count => Components.Count;

    public static ComponentId Register<T>() where T : struct
    {
        var type = typeof(T);

        lock (Lock)
        {
            if (TypeToId.TryGetValue(type, out var existing))
            {
                return existing;
            }

            var id = new ComponentId(MaxId++);
            TypeToId[type] = id;
            Components.Add(new ComponentInfo(type, Unsafe.SizeOf<T>()));
            return id;
        }
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