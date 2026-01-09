using DenOfIz;

namespace NiziKit.Assets.Loaders;

internal static class TextureLoader
{
    public static Texture Load(string path, LogicalDevice device)
    {
        var resolvedPath = AssetPaths.ResolveTexture(path);

        if (resolvedPath.EndsWith(".dztex", StringComparison.OrdinalIgnoreCase))
        {
            return LoadDzTex(resolvedPath, device);
        }

        return LoadStandardFormat(resolvedPath, device);
    }

    private static Texture LoadDzTex(string path, LogicalDevice device)
    {
        var readerDesc = new BinaryReaderDesc();
        using var reader = DenOfIz.BinaryReader.CreateFromFile(StringView.Create(path), in readerDesc);

        using var assetReader = new TextureAssetReader(new TextureAssetReaderDesc
        {
            Reader = reader
        });

        var asset = assetReader.Read();

        var gpuTexture = device.CreateTexture(new TextureDesc
        {
            Width = asset.Width(),
            Height = asset.Height(),
            Depth = Math.Max(1u, asset.Depth()),
            MipLevels = asset.MipLevels(),
            ArraySize = asset.ArraySize(),
            Format = asset.GetFormat(),
            Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
            DebugName = StringView.Create(asset.Name().ToString())
        });

        var constants = device.DeviceInfo().Constants;
        var bufferSize = assetReader.AlignedTotalNumBytes(in constants);

        var stagingBuffer = device.CreateBuffer(new BufferDesc
        {
            NumBytes = bufferSize,
            HeapType = HeapType.CpuGpu,
            Usage = (uint)BufferUsageFlagBits.CopySrc,
            DebugName = StringView.Create("TextureStagingBuffer")
        });

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
        assetReader.LoadIntoGpuTexture(new LoadIntoGpuTextureDesc
        {
            CommandList = commandList,
            StagingBuffer = stagingBuffer,
            Texture = gpuTexture
        });

        commandList.End();
        commandQueue.ExecuteCommandLists(commandLists);
        commandQueue.WaitIdle();
        commandListPool.Dispose();
        commandQueue.Dispose();
        stagingBuffer.Dispose();

        return new Texture
        {
            Name = asset.Name().ToString(),
            SourcePath = path,
            Width = asset.Width(),
            Height = asset.Height(),
            MipLevels = asset.MipLevels(),
            GpuTexture = gpuTexture
        };
    }

    private static Texture LoadStandardFormat(string path, LogicalDevice device)
    {
        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = false
        });
        batchCopy.Begin();

        var gpuTexture = batchCopy.CreateAndLoadTexture(StringView.Create(path));

        batchCopy.Submit(null);
        batchCopy.Dispose();

        return new Texture
        {
            Name = Path.GetFileNameWithoutExtension(path),
            SourcePath = path,
            GpuTexture = gpuTexture
        };
    }
}
