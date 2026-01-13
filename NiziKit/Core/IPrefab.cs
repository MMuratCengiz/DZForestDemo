namespace NiziKit.Core;

public interface IPrefab
{
    GameObject Instantiate();
}

public interface IPrefab<out T> : IPrefab where T : GameObject
{
    new T Instantiate(Scene scene);

    GameObject IPrefab.Instantiate() => Instantiate();
}
