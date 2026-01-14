using NiziKit.Components;

namespace NiziKit.Core;

public interface IWorldEventListener
{
    void SceneReset();
    void GameObjectCreated(GameObject go);
    void GameObjectDestroyed(GameObject go);
    void ComponentAdded(GameObject go, IComponent component);
    void ComponentRemoved(GameObject go, IComponent component);
}