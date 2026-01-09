namespace DenOfIz.World.SceneManagement;

public interface IPrefab
{
    GameObject Instantiate();
}

public interface IPrefab<out T> : IPrefab where T : GameObject
{
    new T Instantiate();

    GameObject IPrefab.Instantiate() => Instantiate();
}
