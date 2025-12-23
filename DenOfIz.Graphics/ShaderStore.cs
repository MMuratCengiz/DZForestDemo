namespace Graphics;

public class ShaderStore
{
    private readonly List<Shader> _shaders = [];
    
    public ulong AddShader(Shader shader)
    {
        var shaderHandle = (ulong)_shaders.Count;
        _shaders.Add(shader);
        return shaderHandle;
    }
}