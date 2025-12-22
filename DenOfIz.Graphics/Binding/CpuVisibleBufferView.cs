using Buffer = DenOfIz.Buffer;

namespace Graphics.Binding;

public struct CpuVisibleBufferView(IntPtr mappedMemory, Buffer buffer, ulong offset, ulong numBytes)
{
    public readonly IntPtr MappedMemory = mappedMemory;
    public readonly Buffer Buffer = buffer;
    public readonly ulong Offset = offset;
    public readonly ulong NumBytes = numBytes;
}