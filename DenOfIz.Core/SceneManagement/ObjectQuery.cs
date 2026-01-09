namespace DenOfIz.World.SceneManagement;

public interface IObjectQuery
{
    public ReadOnlySpan<GameObject> FindObjects<T>() where T : GameObject;
}