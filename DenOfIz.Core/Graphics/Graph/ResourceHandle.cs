using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DenOfIz.World.Graphics.Graph;

/// <summary>
/// A render graph resource handle with generational index pattern.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct ResourceHandle(uint index, uint version) : IEquatable<ResourceHandle>
{
    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = index;

    public uint Version
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
    } = version;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Version != 0;
    }

    public static ResourceHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ResourceHandle other) => Index == other.Index && Version == other.Version;

    public override bool Equals(object? obj) => obj is ResourceHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => HashCode.Combine(Index, Version);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ResourceHandle left, ResourceHandle right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ResourceHandle left, ResourceHandle right) => !left.Equals(right);
}

[Flags]
public enum ResourceAccess
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}
