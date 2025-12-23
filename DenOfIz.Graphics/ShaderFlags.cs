namespace Graphics;

public class ShaderFlags
{
    private readonly Dictionary<string, bool> _flags = new();
    
    public ShaderFlags(string[] keys)
    {
        foreach (var key in keys)
        {
            _flags.Add(key, false);
        }
    }

    public void Enable(string key)
    {
        _flags[key] = true;
    }
    
    public void Disable(string key)
    {
        _flags[key] = false;
    }
    
    public bool IsEnabled(string key)
    {
        return _flags[key];
    }
}