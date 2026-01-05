using DenOfIz;
using RuntimeAssets.Store;

namespace RuntimeAssets;

public sealed class AssetManager(LogicalDevice device) : IDisposable
{
    private readonly AssetResource _resource = new(device);
    private bool _disposed;

    public AssetResource Resource => _resource;

    public void BeginUpload() => _resource.BeginUpload();
    public void EndUpload() => _resource.EndUpload();

    public RuntimeMeshHandle AddBox(float width, float height, float depth) => _resource.AddBox(width, height, depth);
    public RuntimeMeshHandle AddSphere(float diameter, uint tessellation = 16) => _resource.AddSphere(diameter, tessellation);
    public RuntimeMeshHandle AddQuad(float width, float height) => _resource.AddQuad(width, height);
    public RuntimeMeshHandle AddGeometry(GeometryData geometry) => _resource.AddGeometry(geometry);

    public RuntimeMeshHandle AddMesh(string path) => _resource.AddMesh(path);
    public RuntimeMeshHandle AddMeshAbsolute(string absolutePath) => _resource.AddMeshAbsolute(absolutePath);

    public RuntimeTextureHandle AddTexture(string path) => _resource.AddTexture(path);
    public RuntimeTextureHandle AddTextureAbsolute(string absolutePath) => _resource.AddTextureAbsolute(absolutePath);

    public bool TryGetMesh(RuntimeMeshHandle handle, out RuntimeMesh mesh) => _resource.TryGetMesh(handle, out mesh);
    public ref readonly RuntimeMesh GetMeshRef(RuntimeMeshHandle handle) => ref _resource.GetMeshRef(handle);

    public bool TryGetTexture(RuntimeTextureHandle handle, out RuntimeTexture texture) => _resource.TryGetTexture(handle, out texture);
    public ref readonly RuntimeTexture GetTextureRef(RuntimeTextureHandle handle) => ref _resource.GetTextureRef(handle);

    public void RemoveMesh(RuntimeMeshHandle handle) => _resource.RemoveMesh(handle);
    public void RemoveTexture(RuntimeTextureHandle handle) => _resource.RemoveTexture(handle);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _resource.Dispose();
    }
}
