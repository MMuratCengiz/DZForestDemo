namespace NiziKit.Core;

public interface IObjectQuery
{
    public ReadOnlySpan<GameObject> FindObjects<T>() where T : GameObject;
}