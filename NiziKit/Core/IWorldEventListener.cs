namespace NiziKit.Core;

public interface IWorldEventListener
{
    public void SceneReset();
    public void GameObjectCreated(GameObject go);
    public void GameObjectDestroyed(GameObject go);
}