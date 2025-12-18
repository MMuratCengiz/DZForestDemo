using System.Runtime.CompilerServices;
using ECS;

namespace RuntimeAssets;

/// <summary>
/// A mesh resource handle.
/// </summary>
public readonly struct RuntimeMeshHandle : IEquatable<RuntimeMeshHandle>
{
    private readonly Handle<MeshTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeMeshHandle(uint index, uint generation)
    {
        _handle = new Handle<MeshTag>(index, generation);
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

    public static RuntimeMeshHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeMeshHandle other) => _handle.Equals(other._handle);

    public override bool Equals(object? obj) => obj is RuntimeMeshHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _handle.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeMeshHandle left, RuntimeMeshHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeMeshHandle left, RuntimeMeshHandle right) => !left.Equals(right);
}

/// <summary>
/// A texture resource handle.
/// </summary>
public readonly struct RuntimeTextureHandle : IEquatable<RuntimeTextureHandle>
{
    private readonly Handle<TextureTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeTextureHandle(uint index, uint generation)
    {
        _handle = new Handle<TextureTag>(index, generation);
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

    public static RuntimeTextureHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeTextureHandle other) => _handle.Equals(other._handle);

    public override bool Equals(object? obj) => obj is RuntimeTextureHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _handle.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeTextureHandle left, RuntimeTextureHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeTextureHandle left, RuntimeTextureHandle right) => !left.Equals(right);
}

/// <summary>
/// A skeleton resource handle.
/// </summary>
public readonly struct RuntimeSkeletonHandle : IEquatable<RuntimeSkeletonHandle>
{
    private readonly Handle<SkeletonTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeSkeletonHandle(uint index, uint generation)
    {
        _handle = new Handle<SkeletonTag>(index, generation);
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

    public static RuntimeSkeletonHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeSkeletonHandle other) => _handle.Equals(other._handle);

    public override bool Equals(object? obj) => obj is RuntimeSkeletonHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _handle.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeSkeletonHandle left, RuntimeSkeletonHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeSkeletonHandle left, RuntimeSkeletonHandle right) => !left.Equals(right);
}

/// <summary>
/// An animation resource handle.
/// </summary>
public readonly struct RuntimeAnimationHandle : IEquatable<RuntimeAnimationHandle>
{
    private readonly Handle<AnimationTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeAnimationHandle(uint index, uint generation)
    {
        _handle = new Handle<AnimationTag>(index, generation);
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

    public static RuntimeAnimationHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeAnimationHandle other) => _handle.Equals(other._handle);

    public override bool Equals(object? obj) => obj is RuntimeAnimationHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _handle.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeAnimationHandle left, RuntimeAnimationHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeAnimationHandle left, RuntimeAnimationHandle right) => !left.Equals(right);
}

/// <summary>
/// A geometry resource handle.
/// </summary>
public readonly struct RuntimeGeometryHandle : IEquatable<RuntimeGeometryHandle>
{
    private readonly Handle<GeometryTag> _handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeGeometryHandle(uint index, uint generation)
    {
        _handle = new Handle<GeometryTag>(index, generation);
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

    public static RuntimeGeometryHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(RuntimeGeometryHandle other) => _handle.Equals(other._handle);

    public override bool Equals(object? obj) => obj is RuntimeGeometryHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _handle.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(RuntimeGeometryHandle left, RuntimeGeometryHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(RuntimeGeometryHandle left, RuntimeGeometryHandle right) => !left.Equals(right);
}
