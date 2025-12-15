using DenOfIz;

namespace RuntimeAssets;

public sealed class AssetLoader : IDisposable
{
    private readonly LogicalDevice _device;
    private readonly GltfLoader _gltfLoader;
    private readonly GeometryBuilder _geometryBuilder;

    public RuntimeMeshStore Meshes { get; }
    public RuntimeTextureStore Textures { get; }
    public RuntimeSkeletonStore Skeletons { get; }

    private bool _disposed;

    public AssetLoader(LogicalDevice device)
    {
        _device = device;
        _gltfLoader = new GltfLoader();
        _geometryBuilder = new GeometryBuilder();
        Meshes = new RuntimeMeshStore(device);
        Textures = new RuntimeTextureStore(device);
        Skeletons = new RuntimeSkeletonStore();
    }

    public async Task<RuntimeMeshHandle> LoadMeshAsync(string gltfPath, CancellationToken cancellationToken = default)
    {
        var result = await _gltfLoader.LoadAsync(gltfPath, cancellationToken);
        if (!result.Success || result.Meshes.Count == 0)
        {
            return RuntimeMeshHandle.Invalid;
        }

        using var batchCopy = CreateBatchCopy();
        batchCopy.Begin();
        var handle = Meshes.Add(result.Meshes[0], batchCopy);
        batchCopy.Submit(_device.CreateSemaphore());
        return handle;
    }

    public RuntimeMeshHandle LoadMesh(string gltfPath)
    {
        var result = _gltfLoader.Load(gltfPath);
        if (!result.Success || result.Meshes.Count == 0)
        {
            return RuntimeMeshHandle.Invalid;
        }

        using var batchCopy = CreateBatchCopy();
        batchCopy.Begin();
        var handle = Meshes.Add(result.Meshes[0], batchCopy);
        batchCopy.Submit(_device.CreateSemaphore());
        return handle;
    }

    public async Task<RuntimeTextureHandle> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadTexture(path), cancellationToken);
    }

    public RuntimeTextureHandle LoadTexture(string path)
    {
        if (!File.Exists(path))
        {
            return RuntimeTextureHandle.Invalid;
        }

        using var batchCopy = CreateBatchCopy();
        batchCopy.Begin();
        var handle = Textures.Add(path, batchCopy);
        batchCopy.Submit(_device.CreateSemaphore());
        return handle;
    }

    public async Task<RuntimeSkeletonHandle> LoadSkeletonAsync(string ozzPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Skeletons.AddSkeleton(ozzPath), cancellationToken);
    }

    public RuntimeSkeletonHandle LoadSkeleton(string ozzPath)
    {
        return Skeletons.AddSkeleton(ozzPath);
    }

    public async Task<RuntimeAnimationHandle> LoadAnimationAsync(RuntimeSkeletonHandle skeletonHandle, string ozzPath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Skeletons.AddAnimation(skeletonHandle, ozzPath), cancellationToken);
    }

    public RuntimeAnimationHandle LoadAnimation(RuntimeSkeletonHandle skeletonHandle, string ozzPath)
    {
        return Skeletons.AddAnimation(skeletonHandle, ozzPath);
    }

    public RuntimeMeshHandle LoadBox(float width, float height, float depth)
    {
        using var geometry = _geometryBuilder.BuildBox(width, height, depth);
        return LoadGeometry(geometry);
    }

    public RuntimeMeshHandle LoadSphere(float diameter, uint tessellation = 16)
    {
        using var geometry = _geometryBuilder.BuildSphere(diameter, tessellation);
        return LoadGeometry(geometry);
    }

    public RuntimeMeshHandle LoadCylinder(float diameter, float height, uint tessellation = 16)
    {
        using var geometry = _geometryBuilder.BuildCylinder(diameter, height, tessellation);
        return LoadGeometry(geometry);
    }

    public RuntimeMeshHandle LoadCone(float diameter, float height, uint tessellation = 16)
    {
        using var geometry = _geometryBuilder.BuildCone(diameter, height, tessellation);
        return LoadGeometry(geometry);
    }

    public RuntimeMeshHandle LoadTorus(float diameter, float thickness, uint tessellation = 16)
    {
        using var geometry = _geometryBuilder.BuildTorus(diameter, thickness, tessellation);
        return LoadGeometry(geometry);
    }

    public RuntimeMeshHandle LoadQuadXY(float width, float height)
    {
        using var geometry = _geometryBuilder.BuildQuadXY(width, height);
        return LoadGeometry(geometry);
    }

    public RuntimeMeshHandle LoadQuadXZ(float width, float height)
    {
        using var geometry = _geometryBuilder.BuildQuadXZ(width, height);
        return LoadGeometry(geometry);
    }

    private RuntimeMeshHandle LoadGeometry(GeometryData geometry)
    {
        using var batchCopy = CreateBatchCopy();
        batchCopy.Begin();
        var handle = Meshes.AddGeometry(geometry, batchCopy);
        batchCopy.Submit(_device.CreateSemaphore());
        return handle;
    }

    private BatchResourceCopy CreateBatchCopy()
    {
        return new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = _device,
            IssueBarriers = false
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Meshes.Dispose();
        Textures.Dispose();
        Skeletons.Dispose();
    }
}
