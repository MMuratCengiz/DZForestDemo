using System.Runtime.InteropServices;
using DenOfIz;

namespace RuntimeAssets;

/// <summary>
/// Creates and manages a 1x1 purple placeholder texture commonly used as a fallback when textures are missing or not
/// yet loaded.
/// </summary>
public sealed class NullTexture : IDisposable
{
    private static readonly byte[] PurplePixel = [255, 0, 255, 255]; // RGBA

    private bool _disposed;

    public Texture Texture { get; }

    public NullTexture(LogicalDevice device)
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
            DebugName = StringView.Create("NullTexture_Purple")
        });
        UploadPixelData(device);
    }

    private void UploadPixelData(LogicalDevice device)
    {
        var stagingBuffer = device.CreateBuffer(new BufferDesc
        {
            NumBytes = 4, // 4 bytes for RGBA
            HeapType = HeapType.CpuGpu,
            Usage = (uint)BufferUsageFlagBits.CopySrc,
            DebugName = StringView.Create("NullTexture_Staging")
        });

        var mappedPtr = stagingBuffer.MapMemory();
        Marshal.Copy(PurplePixel, 0, mappedPtr, 4);
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
        commandQueue.ExecuteCommandLists(new ExecuteCommandListsDesc
        {
            CommandLists = commandLists
        });
        commandQueue.WaitIdle();
        commandListPool.Dispose();
        commandQueue.Dispose();
        stagingBuffer.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Texture.Dispose();
        GC.SuppressFinalize(this);
    }
}