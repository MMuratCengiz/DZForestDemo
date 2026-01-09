using DenOfIz.World.SceneManagement;

namespace DenOfIz.World.Components;

public interface IComponent
{
    GameObject? Owner { get; set; }
    void OnAttach() { }
    void OnDetach() { }
    void OnUpdate(float deltaTime) { }
}
