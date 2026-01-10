using NiziKit.Assets;
using NiziKit.SceneManagement;

namespace NiziKit.Components;

public class MaterialComponent : IComponent
{
    public GameObject? Owner { get; set; }
    public Material? Material { get; set; }

    public void OnAttach() { }
    public void OnDetach() { }
    public void OnUpdate(float deltaTime) { }
}
