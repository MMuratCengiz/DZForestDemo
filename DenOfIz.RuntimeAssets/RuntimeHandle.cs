using System.Runtime.CompilerServices;

namespace RuntimeAssets;

public readonly struct RuntimeHandle<T> : IEquatable<RuntimeHandle<T>> where T : struct
{
    public readonly uint Index;
    public readonly uint Generation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeHandle(uint index, uint generation)
    {
        Index = index;
        Generation = generation;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static RuntimeHandle<T> Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeHandle<T> other) => Index == other.Index && Generation == other.Generation;
    public override bool Equals(object? obj) => obj is RuntimeHandle<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Index, Generation);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeHandle<T> left, RuntimeHandle<T> right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeHandle<T> left, RuntimeHandle<T> right) => !left.Equals(right);
}

public struct RuntimeMeshTag;
public struct RuntimeTextureTag;
public struct RuntimeSkeletonTag;
public struct RuntimeAnimationTag;
public struct RuntimeGeometryTag;

public readonly struct RuntimeMeshHandle : IEquatable<RuntimeMeshHandle>
{
    private readonly RuntimeHandle<RuntimeMeshTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeMeshHandle(uint index, uint generation) => _handle = new(index, generation);

    public uint Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Index; }
    public uint Generation { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Generation; }
    public bool IsValid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.IsValid; }

    public static RuntimeMeshHandle Invalid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeMeshHandle other) => _handle.Equals(other._handle);
    public override bool Equals(object? obj) => obj is RuntimeMeshHandle other && Equals(other);
    public override int GetHashCode() => _handle.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeMeshHandle left, RuntimeMeshHandle right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeMeshHandle left, RuntimeMeshHandle right) => !left.Equals(right);
}

public readonly struct RuntimeTextureHandle : IEquatable<RuntimeTextureHandle>
{
    private readonly RuntimeHandle<RuntimeTextureTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeTextureHandle(uint index, uint generation) => _handle = new(index, generation);

    public uint Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Index; }
    public uint Generation { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Generation; }
    public bool IsValid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.IsValid; }

    public static RuntimeTextureHandle Invalid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeTextureHandle other) => _handle.Equals(other._handle);
    public override bool Equals(object? obj) => obj is RuntimeTextureHandle other && Equals(other);
    public override int GetHashCode() => _handle.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeTextureHandle left, RuntimeTextureHandle right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeTextureHandle left, RuntimeTextureHandle right) => !left.Equals(right);
}

public readonly struct RuntimeSkeletonHandle : IEquatable<RuntimeSkeletonHandle>
{
    private readonly RuntimeHandle<RuntimeSkeletonTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeSkeletonHandle(uint index, uint generation) => _handle = new(index, generation);

    public uint Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Index; }
    public uint Generation { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Generation; }
    public bool IsValid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.IsValid; }

    public static RuntimeSkeletonHandle Invalid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeSkeletonHandle other) => _handle.Equals(other._handle);
    public override bool Equals(object? obj) => obj is RuntimeSkeletonHandle other && Equals(other);
    public override int GetHashCode() => _handle.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeSkeletonHandle left, RuntimeSkeletonHandle right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeSkeletonHandle left, RuntimeSkeletonHandle right) => !left.Equals(right);
}

public readonly struct RuntimeAnimationHandle : IEquatable<RuntimeAnimationHandle>
{
    private readonly RuntimeHandle<RuntimeAnimationTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeAnimationHandle(uint index, uint generation) => _handle = new(index, generation);

    public uint Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Index; }
    public uint Generation { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Generation; }
    public bool IsValid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.IsValid; }

    public static RuntimeAnimationHandle Invalid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeAnimationHandle other) => _handle.Equals(other._handle);
    public override bool Equals(object? obj) => obj is RuntimeAnimationHandle other && Equals(other);
    public override int GetHashCode() => _handle.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeAnimationHandle left, RuntimeAnimationHandle right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeAnimationHandle left, RuntimeAnimationHandle right) => !left.Equals(right);
}

public readonly struct RuntimeGeometryHandle : IEquatable<RuntimeGeometryHandle>
{
    private readonly RuntimeHandle<RuntimeGeometryTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeGeometryHandle(uint index, uint generation) => _handle = new(index, generation);

    public uint Index { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Index; }
    public uint Generation { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.Generation; }
    public bool IsValid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _handle.IsValid; }

    public static RuntimeGeometryHandle Invalid { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => default; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeGeometryHandle other) => _handle.Equals(other._handle);
    public override bool Equals(object? obj) => obj is RuntimeGeometryHandle other && Equals(other);
    public override int GetHashCode() => _handle.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeGeometryHandle left, RuntimeGeometryHandle right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeGeometryHandle left, RuntimeGeometryHandle right) => !left.Equals(right);
}
