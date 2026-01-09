using NiziKit.Graphics.Batching;
using NiziKit.SceneManagement;

namespace NiziKit.Components;

public class MeshComponent : IComponent
{
    public GameObject? Owner { get; set; }

    public MeshId Mesh { get; set; } = MeshId.Invalid;
    public RenderMaterial Material { get; set; } = RenderMaterial.Default;
    public RenderFlags Flags { get; set; } = RenderFlags.CastsShadow;

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnUpdate(float deltaTime) { }
}
