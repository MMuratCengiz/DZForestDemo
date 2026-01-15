using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.Graphics.Binding;

public sealed class ColorTexture : IDisposable
{
    public Texture Texture { get; }

    public ColorTexture(LogicalDevice device, byte r, byte g, byte b, byte a, string debugName)
    {
        Texture = device.CreateTexture(new TextureDesc
        {
            Width = 1,
            Height = 1,
            Depth = 1,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8Unorm,
            Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create(debugName)
        });
        UploadPixelData(device, [r, g, b, a], debugName);
    }

    private void UploadPixelData(LogicalDevice device, byte[] pixelData, string debugName)
    {
        var stagingBuffer = device.CreateBuffer(new BufferDesc
        {
            NumBytes = 4, // 4 bytes for RGBA
            HeapType = HeapType.CpuGpu,
            Usage = (uint)BufferUsageFlagBits.CopySrc,
            DebugName = StringView.Create($"{debugName}_Staging")
        });

        var mappedPtr = stagingBuffer.MapMemory();
        Marshal.Copy(pixelData, 0, mappedPtr, 4);
        stagingBuffer.UnmapMemory();

        var commandQueue = device.CreateCommandQueue(new CommandQueueDesc
        {
            QueueType = QueueType.Graphics
        });

        var commandListPool = device.CreateCommandListPool(new CommandListPoolDesc
        {
            CommandQueue = commandQueue,
            NumCommandLists = 1
        });

        var commandLists = commandListPool.GetCommandLists();
        var commandList = commandLists.ToArray()[0];

        commandList.Begin();
        commandList.CopyBufferToTexture(new CopyBufferToTextureDesc
        {
            SrcBuffer = stagingBuffer,
            SrcOffset = 0,
            DstTexture = Texture,
            DstX = 0,
            DstY = 0,
            DstZ = 0,
            Format = Format.R8G8B8A8Unorm,
            MipLevel = 0,
            ArrayLayer = 0,
            RowPitch = 4,
            NumRows = 1
        });

        commandList.End();
        commandQueue.ExecuteCommandLists(commandLists);
        commandQueue.WaitIdle();
        commandListPool.Dispose();
        commandQueue.Dispose();
        stagingBuffer.Dispose();
    }

    public void Dispose()
    {
        Texture.Dispose();
    }
}