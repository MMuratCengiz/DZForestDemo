namespace NiziKit.Assets;

public class Model : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public List<Mesh> Meshes { get; set; } = [];
    public Skeleton? Skeleton { get; set; }
    public List<Animation> Animations { get; set; } = [];
    public List<Material> Materials { get; set; } = [];

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
