namespace NiziKit.SceneManagement;

public interface IObjectQuery
{
    public ReadOnlySpan<GameObject> FindObjects<T>() where T : GameObject;
}