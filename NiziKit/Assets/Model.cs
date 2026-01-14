using NiziKit.GLTF;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public class Model : IAsset
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public List<Mesh> Meshes { get; set; } = [];
    public Skeleton? Skeleton { get; set; }
    public List<Animation> Animations { get; set; } = [];
    public List<Material> Materials { get; set; } = [];

    public void Load(GraphicsContext context, string path)
    {
        var gltfModel = GltfModel.Load(path);
        ApplyGltfModel(gltfModel);
    }

    public async Task LoadAsync(GraphicsContext context, string path, CancellationToken ct = default)
    {
        var gltfModel = await GltfModel.LoadAsync(path, null, ct);
        ApplyGltfModel(gltfModel);
    }

    public void LoadFromBytes(byte[] bytes, string name)
    {
        var gltfModel = GltfModel.LoadFromBytes(bytes, name);
        ApplyGltfModel(gltfModel);
    }

    private void ApplyGltfModel(GltfModel gltfModel)
    {
        Name = gltfModel.Name;
        SourcePath = gltfModel.SourcePath;
        Meshes = gltfModel.Meshes;
    }

    public void Dispose()
    {
        foreach (var mesh in Meshes)
        {
            mesh.Dispose();
        }

        Skeleton?.Dispose();

        foreach (var animation in Animations)
        {
            animation.Dispose();
        }
    }
}
