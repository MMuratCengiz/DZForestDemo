using System.Runtime.CompilerServices;
using DenOfIz;
using RuntimeAssets.Store;

namespace RuntimeAssets;

public sealed class AssetResource(LogicalDevice device) : IDisposable
{
    private readonly GeometryBuilder _geometryBuilder = new();
    private readonly RuntimeMeshStore _meshStore = new(device);
    private readonly RuntimeTextureStore _textureStore = new(device);

    private BatchResourceCopy? _batchCopy;
    private bool _disposed;
    private bool _uploading;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _batchCopy?.Dispose();
        _meshStore.Dispose();
        _textureStore.Dispose();
    }

    public void BeginUpload()
    {
        if (_uploading)
        {
            throw new InvalidOperationException("Upload already in progress.");
        }

        _batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = false
        });
        _batchCopy.Begin();
        _uploading = true;
    }

    public RuntimeMeshHandle AddGeometry(GeometryData geometry)
    {
        EnsureUploading();
        var handle = _meshStore.AddGeometry(geometry, _batchCopy!);
        geometry.Dispose();
        return handle;
    }

    public RuntimeMeshHandle AddQuad(float width, float height)
    {
        EnsureUploading();
        using var geometry = _geometryBuilder.BuildQuadXY(width, height);
        return _meshStore.AddGeometry(geometry, _batchCopy!);
    }

    public RuntimeMeshHandle AddBox(float width, float height, float depth)
    {
        EnsureUploading();
        using var geometry = _geometryBuilder.BuildBox(width, height, depth);
        return _meshStore.AddGeometry(geometry, _batchCopy!);
    }

    public RuntimeMeshHandle AddSphere(float diameter, uint tessellation = 16)
    {
        EnsureUploading();
        using var geometry = _geometryBuilder.BuildSphere(diameter, tessellation);
        return _meshStore.AddGeometry(geometry, _batchCopy!);
    }

    public RuntimeMeshHandle AddMesh(string path)
    {
        EnsureUploading();
        var resolvedPath = AssetPaths.ResolveMesh(path);
        return _meshStore.AddFromFile(resolvedPath, _batchCopy!);
    }

    public RuntimeMeshHandle AddMeshAbsolute(string absolutePath)
    {
        EnsureUploading();
        return _meshStore.AddFromFile(absolutePath, _batchCopy!);
    }

    public RuntimeTextureHandle AddTexture(string path)
    {
        EnsureUploading();
        var resolvedPath = AssetPaths.ResolveTexture(path);
        return _textureStore.Add(resolvedPath, _batchCopy!);
    }

    public RuntimeTextureHandle AddTextureAbsolute(string absolutePath)
    {
        EnsureUploading();
        return _textureStore.Add(absolutePath, _batchCopy!);
    }

    public void EndUpload()
    {
        EnsureUploading();

        _batchCopy!.Submit(null);
        _meshStore.ReleasePendingHandles();
        _batchCopy.Dispose();
        _batchCopy = null;
        _uploading = false;
    }

    public Task EndUploadAsync()
    {
        EnsureUploading();

        _batchCopy!.Submit(null);
        _meshStore.ReleasePendingHandles();

        var batchCopy = _batchCopy;
        _batchCopy = null;
        _uploading = false;

        return Task.Run(() => { batchCopy.Dispose(); });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMesh(RuntimeMeshHandle handle, out RuntimeMesh mesh)
    {
        return _meshStore.TryGet(handle, out mesh);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeMesh GetMeshRef(RuntimeMeshHandle handle)
    {
        return ref _meshStore.GetRef(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTexture(RuntimeTextureHandle handle, out RuntimeTexture texture)
    {
        return _textureStore.TryGet(handle, out texture);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeTexture GetTextureRef(RuntimeTextureHandle handle)
    {
        return ref _textureStore.GetRef(handle);
    }

    public void RemoveMesh(RuntimeMeshHandle handle)
    {
        _meshStore.Remove(handle);
    }

    public void RemoveTexture(RuntimeTextureHandle handle)
    {
        _textureStore.Remove(handle);
    }

    private void EnsureUploading()
    {
        if (!_uploading)
        {
            throw new InvalidOperationException("Call BeginUpload() first.");
        }
    }
}