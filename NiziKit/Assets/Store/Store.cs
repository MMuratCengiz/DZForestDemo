namespace NiziKit.Assets.Store;

public abstract class Store<T> 
{
    protected Dictionary<string, T> _cache = new Dictionary<string, T>();

    public T this[string key] => _cache[key];
    
    public void Register(string key, T value)
    {
        _cache.Add(key, value);
    }
}