using NiziKit.SceneManagement;

namespace NiziKit.Components;

public interface IComponent
{
    GameObject? Owner { get; set; }
    void OnAttach() { }
    void OnDetach() { }
    void OnUpdate(float deltaTime) { }
}
