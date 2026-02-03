using DenOfIz;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;
using BinaryReader = DenOfIz.BinaryReader;

namespace NiziKit.Assets;

public class Texture2d
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint MipLevels { get; set; }
    public Format Format { get; set; }
    public Texture GpuTexture { get; set; } = null!;

    public Texture2d()
    {
    }

    public void Load(string path)
    {
        var bytes = Content.ReadBytes(path);
        LoadFromBytes(path, bytes);
    }

    public async Task LoadAsync(string path, CancellationToken ct = default)
    {
        var bytes = await Content.ReadBytesAsync(path, ct);
        LoadFromBytes(path, bytes);
    }

    public void LoadFromBytes(string path, byte[] bytes)
    {
        if (IsDzTex(path))
        {
            LoadFromDzTex(bytes, path);
        }
        else
        {
            LoadFromBytes(GetTextureExtension(path), bytes, path);
        }
    }

    private static bool IsDzTex(string path)
    {
        return Path.GetExtension(path).Equals(".dztex", StringComparison.OrdinalIgnoreCase);
    }

    private static TextureExtension GetTextureExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".dds" => TextureExtension.Dds,
            ".png" => TextureExtension.Png,
            ".jpg" or ".jpeg" => TextureExtension.Jpg,
            ".bmp" => TextureExtension.Bmp,
            ".tga" => TextureExtension.Tga,
            ".hdr" => TextureExtension.Hdr,
            ".gif" => TextureExtension.Gif,
            ".pic" => TextureExtension.Pic,
            _ => throw new ArgumentException($"Unsupported texture extension: {ext}", nameof(path))
        };
    }

    public void LoadFromBytes(TextureExtension extension, byte[] bytes, string name)
    {
        var device = GraphicsContext.Device;

        var textureDataDesc = new TextureCreateFromDataDesc
        {
            Data = ByteArrayView.Create(bytes),
            Extension = extension
        };
        using var textureData = TextureData.CreateFromData(textureDataDesc);

        lock (GraphicsContext.TransferLock)
        {
            var gpuTexture = device.CreateTexture(new TextureDesc
            {
                Width = textureData.GetWidth(),
                Height = textureData.GetHeight(),
                Depth = Math.Max(1u, textureData.GetDepth()),
                MipLevels = textureData.GetMipLevels(),
                ArraySize = textureData.GetArraySize(),
                Format = textureData.GetFormat(),
                Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
                DebugName = StringView.Create(Path.GetFileNameWithoutExtension(name))
            });

            using var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
            {
                Device = device,
                IssueBarriers = true
            });
            batchCopy.Begin();
            batchCopy.LoadTextureFromData(new LoadTextureFromDataDesc
            {
                TextureData = textureData,
                DstTexture = gpuTexture
            });
            batchCopy.Submit(null);

            GraphicsContext.ResourceTracking.TrackTexture(gpuTexture, QueueType.Graphics);
            Name = Path.GetFileNameWithoutExtension(name);
            SourcePath = name;
            Width = textureData.GetWidth();
            Height = textureData.GetHeight();
            MipLevels = textureData.GetMipLevels();
            Format = textureData.GetFormat();
            GpuTexture = gpuTexture;
        }
    }

    private void LoadFromDzTex(byte[] bytes, string name)
    {
        var device = GraphicsContext.Device;

        using var reader = BinaryReader.CreateFromData(
            ByteArrayView.Create(bytes), new BinaryReaderDesc { NumBytes = 0 });
        using var assetReader = new TextureAssetReader(new TextureAssetReaderDesc { Reader = reader });
        using var textureAsset = assetReader.Read();

        var width = textureAsset.Width();
        var height = textureAsset.Height();
        var depth = Math.Max(1u, textureAsset.Depth());
        var mipLevels = textureAsset.MipLevels();
        var arraySize = textureAsset.ArraySize();
        var format = textureAsset.GetFormat();

        var deviceInfo = device.DeviceInfo();
        var alignedSize = assetReader.AlignedTotalNumBytes(deviceInfo.Constants);

        lock (GraphicsContext.TransferLock)
        {
            var gpuTexture = device.CreateTexture(new TextureDesc
            {
                Width = width,
                Height = height,
                Depth = depth,
                MipLevels = mipLevels,
                ArraySize = arraySize,
                Format = format,
                Usage = (uint)(TextureUsageFlagBits.CopyDst | TextureUsageFlagBits.TextureBinding),
                DebugName = StringView.Create(Path.GetFileNameWithoutExtension(name))
            });

            var stagingBuffer = device.CreateBuffer(new BufferDesc
            {
                HeapType = HeapType.CpuGpu,
                Usage = (uint)BufferUsageFlagBits.CopySrc,
                NumBytes = alignedSize,
                DebugName = StringView.Create("DzTex_StagingBuffer")
            });

            var copyQueueDesc = new CommandQueueDesc { QueueType = QueueType.Copy };
            var copyQueue = device.CreateCommandQueue(copyQueueDesc);
            var poolDesc = new CommandListPoolDesc { CommandQueue = copyQueue, NumCommandLists = 1 };
            var pool = device.CreateCommandListPool(poolDesc);
            var commandList = pool.GetCommandLists().ToArray()[0];
            var fence = device.CreateFence();

            commandList.Begin();

            assetReader.LoadIntoGpuTexture(new LoadIntoGpuTextureDesc
            {
                CommandList = commandList,
                StagingBuffer = stagingBuffer,
                Texture = gpuTexture
            });

            commandList.End();

            fence.Reset();
            copyQueue.ExecuteCommandLists(new ExecuteCommandListsDesc
            {
                Signal = fence,
                CommandLists = CommandListArray.Create([commandList])
            });
            fence.Wait();

            var syncQueueDesc = new CommandQueueDesc { QueueType = QueueType.Graphics };
            var syncQueue = device.CreateCommandQueue(syncQueueDesc);
            var syncPool = device.CreateCommandListPool(new CommandListPoolDesc { CommandQueue = syncQueue, NumCommandLists = 1 });
            var syncCommandList = syncPool.GetCommandLists().ToArray()[0];
            var syncFence = device.CreateFence();

            syncCommandList.Begin();
            syncCommandList.PipelineBarrier(new PipelineBarrierDesc
            {
                TextureBarriers = TextureBarrierDescArray.Create([new TextureBarrierDesc
                {
                    Resource = gpuTexture,
                    OldState = (uint)ResourceUsageFlagBits.Common,
                    NewState = (uint)ResourceUsageFlagBits.ShaderResource
                }])
            });
            syncCommandList.End();

            syncFence.Reset();
            syncQueue.ExecuteCommandLists(new ExecuteCommandListsDesc
            {
                Signal = syncFence,
                CommandLists = CommandListArray.Create([syncCommandList])
            });
            syncFence.Wait();

            stagingBuffer.Dispose();
            fence.Dispose();
            pool.Dispose();
            copyQueue.Dispose();
            syncFence.Dispose();
            syncPool.Dispose();
            syncQueue.Dispose();

            GraphicsContext.ResourceTracking.TrackTexture(gpuTexture, QueueType.Graphics);
            Name = Path.GetFileNameWithoutExtension(name);
            SourcePath = name;
            Width = width;
            Height = height;
            MipLevels = mipLevels;
            Format = format;
            GpuTexture = gpuTexture;
        }
    }

    public void Dispose()
    {
        GpuTexture.Dispose();
    }
}
