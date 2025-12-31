using DenOfIz;

namespace Application;

public interface IGame
{
    void OnLoad(Game game) { }
    void OnUpdate(float dt) { }
    void OnFixedUpdate(float fixedDt) { }
    void OnRender() { }
    void OnEvent(ref Event ev) { }
    void OnShutdown() { }
}
