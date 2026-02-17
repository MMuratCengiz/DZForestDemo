using NiziKit.Components;

namespace NiziKit.Core;

public interface IWorldEventListener
{
    void SceneReset();
    void GameObjectCreated(GameObject go);
    void GameObjectDestroyed(GameObject go);
    void ComponentAdded(GameObject go, NiziComponent component);
    void ComponentRemoved(GameObject go, NiziComponent component);
    void ComponentChanged(GameObject go, NiziComponent component);
}
