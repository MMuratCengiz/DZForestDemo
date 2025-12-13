using DenOfIz;

namespace ECS;

public interface ISystem : IDisposable
{
    void Initialize(World world) { }
    void Run() { }
    bool OnEvent(ref Event ev) => false;
    void Shutdown() { }
}
