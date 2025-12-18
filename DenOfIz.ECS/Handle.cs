using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS;

/// <summary>
/// A generational handle that provides type-safe, zero-allocation resource references.
/// The type parameter T is a tag struct used for type discrimination.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct Handle<T>(uint index, uint generation) : IEquatable<Handle<T>>
    where T : struct
{
    public readonly uint Index = index;
    public readonly uint Generation = generation;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static Handle<T> Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Handle<T> other)
    {
        return Index == other.Index && Generation == other.Generation;
    }

    public override bool Equals(object? obj)
    {
        return obj is Handle<T> other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Generation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Handle<T> left, Handle<T> right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Handle<T> left, Handle<T> right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"{typeof(T).Name}({Index}v{Generation})";
    }
}
