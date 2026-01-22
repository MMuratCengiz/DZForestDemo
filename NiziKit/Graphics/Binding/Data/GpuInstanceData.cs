using System.Numerics;
using System.Runtime.InteropServices;

namespace NiziKit.Graphics.Binding.Data;

/// <summary>
/// Per-instance data matching HLSL InstanceData struct.
/// Padded to 80 bytes for proper cbuffer array alignment (16-byte boundary).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GpuInstanceData
{
    public Matrix4x4 Model;   // 64 bytes
    public uint BoneOffset;   // 4 bytes
    private uint _pad0;       // 4 bytes
    private uint _pad1;       // 4 bytes
    private uint _pad2;       // 4 bytes
}                             // Total: 80 bytes
