using System.Runtime.CompilerServices;
using DenOfIz;
using ECS;

namespace RuntimeAssets;

public sealed class AssetContext(LogicalDevice device) : IContext, IDisposable
{
    private readonly RuntimeMeshStore _meshStore = new(device);
    private readonly RuntimeTextureStore _textureStore = new(device);
    private readonly GeometryBuilder _geometryBuilder = new();

    private BatchResourceCopy? _batchCopy;
    private bool _uploading;
    private bool _disposed;

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

    public RuntimeTextureHandle AddTexture(string path)
    {
        EnsureUploading();
        return _textureStore.Add(path, _batchCopy!);
    }

    public void EndUpload()
    {
        EnsureUploading();

        _batchCopy!.Submit(null);
        _batchCopy.Dispose();
        _batchCopy = null;
        _uploading = false;
    }

    public Task EndUploadAsync()
    {
        EnsureUploading();

        _batchCopy!.Submit(null);

        var batchCopy = _batchCopy;
        _batchCopy = null;
        _uploading = false;

        return Task.Run(() =>
        {
            batchCopy.Dispose();
        });
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
}
