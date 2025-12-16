using System.Runtime.InteropServices;

namespace Graphics.RenderGraph;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ResourceHandle(int index, int version) : IEquatable<ResourceHandle>
{
    public readonly int Index = index;
    public readonly int Version = version;

    public bool IsValid => Index >= 0;
    public static ResourceHandle Invalid => new(-1, 0);

    public bool Equals(ResourceHandle other)
    {
        return Index == other.Index && Version == other.Version;
    }

    public override bool Equals(object? obj)
    {
        return obj is ResourceHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Index, Version);
    }

    public static bool operator ==(ResourceHandle left, ResourceHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResourceHandle left, ResourceHandle right)
    {
        return !left.Equals(right);
    }
}

[Flags]
public enum ResourceAccess
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}