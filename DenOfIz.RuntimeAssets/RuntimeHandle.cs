using System.Runtime.CompilerServices;

namespace RuntimeAssets;

/// <summary>
/// A mesh resource handle.
/// </summary>
public readonly struct RuntimeMeshHandle(uint index, uint generation) : IEquatable<RuntimeMeshHandle>
{
    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = index;

    public uint Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = generation;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static RuntimeMeshHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeMeshHandle other) => Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is RuntimeMeshHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeMeshHandle left, RuntimeMeshHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeMeshHandle left, RuntimeMeshHandle right) => !left.Equals(right);
}

/// <summary>
/// A texture resource handle.
/// </summary>
public readonly struct RuntimeTextureHandle(uint index, uint generation) : IEquatable<RuntimeTextureHandle>
{
    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = index;

    public uint Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = generation;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static RuntimeTextureHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeTextureHandle other) => Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is RuntimeTextureHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeTextureHandle left, RuntimeTextureHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeTextureHandle left, RuntimeTextureHandle right) => !left.Equals(right);
}

/// <summary>
/// A skeleton resource handle.
/// </summary>
public readonly struct RuntimeSkeletonHandle(uint index, uint generation) : IEquatable<RuntimeSkeletonHandle>
{
    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = index;

    public uint Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = generation;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static RuntimeSkeletonHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeSkeletonHandle other) => Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is RuntimeSkeletonHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeSkeletonHandle left, RuntimeSkeletonHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeSkeletonHandle left, RuntimeSkeletonHandle right) => !left.Equals(right);
}

/// <summary>
/// An animation resource handle.
/// </summary>
public readonly struct RuntimeAnimationHandle(uint index, uint generation) : IEquatable<RuntimeAnimationHandle>
{
    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = index;

    public uint Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = generation;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static RuntimeAnimationHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeAnimationHandle other) => Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is RuntimeAnimationHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeAnimationHandle left, RuntimeAnimationHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeAnimationHandle left, RuntimeAnimationHandle right) => !left.Equals(right);
}

/// <summary>
/// A geometry resource handle.
/// </summary>
public readonly struct RuntimeGeometryHandle(uint index, uint generation) : IEquatable<RuntimeGeometryHandle>
{
    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = index;

    public uint Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = generation;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Generation != 0;
    }

    public static RuntimeGeometryHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeGeometryHandle other) => Index == other.Index && Generation == other.Generation;

    public override bool Equals(object? obj) => obj is RuntimeGeometryHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Index, Generation);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeGeometryHandle left, RuntimeGeometryHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeGeometryHandle left, RuntimeGeometryHandle right) => !left.Equals(right);
}
