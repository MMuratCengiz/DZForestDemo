namespace NiziKit.Assets.Store;

public abstract class Store<T> : IDisposable where T : IDisposable
{
    protected Dictionary<string, T> _cache = new Dictionary<string, T>();

    public T this[string key] => _cache[key];

    public void Register(string key, T value)
    {
        _cache.Add(key, value);
    }

    public void Dispose()
    {
        foreach (var item in _cache.Values)
        {
            item.Dispose();
        }
        _cache.Clear();
    }
}
