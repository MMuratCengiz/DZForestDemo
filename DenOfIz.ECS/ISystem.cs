using DenOfIz;

namespace ECS;

public interface ISystem : IDisposable
{
    void Initialize(World world) { }
    void Update(double deltaTime) { }
    void LateUpdate(double deltaTime) { }
    void FixedUpdate(double fixedDeltaTime) { }
    void Render(double deltaTime) { }
    bool OnEvent(ref Event ev) => false;
    void Shutdown() { }
}
