using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ECS;

namespace Graphics.RenderGraph;

/// <summary>
/// A render graph resource handle.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct ResourceHandle(uint index, uint version) : IEquatable<ResourceHandle>
{
    private readonly Handle<ResourceTag> _handle = new(index, version);

    public uint Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.Index;
    }

    public uint Version
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.Generation;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _handle.IsValid;
    }

    public static ResourceHandle Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ResourceHandle other) => _handle.Equals(other._handle);

    public override bool Equals(object? obj) => obj is ResourceHandle other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => _handle.GetHashCode();

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
