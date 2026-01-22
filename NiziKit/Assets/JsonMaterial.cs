using NiziKit.Assets.Serde;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public sealed class JsonMaterial : Material
{
    private readonly MaterialJson _json;
    private readonly string _basePath;
    private bool _shaderLoaded;
    private bool _texturesLoaded;
    private GpuShader? _lazyShader;

    public JsonMaterial(MaterialJson json, string basePath)
    {
        _json = json;
        _basePath = basePath;
        Name = json.Name;
        Variants = json.GetVariants().ToArray();
    }

    public new GpuShader? GpuShader
    {
        get
        {
            EnsureShaderLoaded();
            return _lazyShader;
        }
        set
        {
            _lazyShader = value;
            _shaderLoaded = true;
            base.GpuShader = value;
        }
    }

    public new Texture2d? Albedo
    {
        get
        {
            EnsureTexturesLoaded();
            return base.Albedo;
        }
        set => base.Albedo = value;
    }

    public new Texture2d? Normal
    {
        get
        {
            EnsureTexturesLoaded();
            return base.Normal;
        }
        set => base.Normal = value;
    }

    public new Texture2d? Metallic
    {
        get
        {
            EnsureTexturesLoaded();
            return base.Metallic;
        }
        set => base.Metallic = value;
    }

    public new Texture2d? Roughness
    {
        get
        {
            EnsureTexturesLoaded();
            return base.Roughness;
        }
        set => base.Roughness = value;
    }

    public void EnsureShaderLoaded()
    {
        if (_shaderLoaded)
        {
            return;
        }

        _shaderLoaded = true;

        if (_json.Shader.StartsWith("Builtin/"))
        {
            _lazyShader = Assets.GetShader(_json.Shader);
        }
        else
        {
            _lazyShader = Assets.LoadShaderFromJson(ResolvePath(_json.Shader));
        }

        base.GpuShader = _lazyShader;
    }

    public void EnsureTexturesLoaded()
    {
        if (_texturesLoaded)
        {
            return;
        }

        _texturesLoaded = true;

        if (!string.IsNullOrEmpty(_json.Textures.Albedo))
        {
            base.Albedo = Assets.LoadTexture(ResolvePath(_json.Textures.Albedo));
        }

        if (!string.IsNullOrEmpty(_json.Textures.Normal))
        {
            base.Normal = Assets.LoadTexture(ResolvePath(_json.Textures.Normal));
        }

        if (!string.IsNullOrEmpty(_json.Textures.Metallic))
        {
            base.Metallic = Assets.LoadTexture(ResolvePath(_json.Textures.Metallic));
        }

        if (!string.IsNullOrEmpty(_json.Textures.Roughness))
        {
            base.Roughness = Assets.LoadTexture(ResolvePath(_json.Textures.Roughness));
        }
    }

    public async Task EnsureShaderLoadedAsync(CancellationToken ct = default)
    {
        if (_shaderLoaded)
        {
            return;
        }

        _shaderLoaded = true;

        if (_json.Shader.StartsWith("Builtin/"))
        {
            _lazyShader = Assets.GetShader(_json.Shader);
        }
        else
        {
            _lazyShader = await Assets.LoadShaderFromJsonAsync(ResolvePath(_json.Shader), ct);
        }

        base.GpuShader = _lazyShader;
    }

    public async Task EnsureTexturesLoadedAsync(CancellationToken ct = default)
    {
        if (_texturesLoaded)
        {
            return;
        }

        _texturesLoaded = true;

        var tasks = new List<Task>();

        if (!string.IsNullOrEmpty(_json.Textures.Albedo))
        {
            tasks.Add(LoadTextureAsync(_json.Textures.Albedo, t => base.Albedo = t, ct));
        }

        if (!string.IsNullOrEmpty(_json.Textures.Normal))
        {
            tasks.Add(LoadTextureAsync(_json.Textures.Normal, t => base.Normal = t, ct));
        }

        if (!string.IsNullOrEmpty(_json.Textures.Metallic))
        {
            tasks.Add(LoadTextureAsync(_json.Textures.Metallic, t => base.Metallic = t, ct));
        }

        if (!string.IsNullOrEmpty(_json.Textures.Roughness))
        {
            tasks.Add(LoadTextureAsync(_json.Textures.Roughness, t => base.Roughness = t, ct));
        }

        await Task.WhenAll(tasks);
    }

    public async Task LoadAllAsync(CancellationToken ct = default)
    {
        await Task.WhenAll(
            EnsureShaderLoadedAsync(ct),
            EnsureTexturesLoadedAsync(ct)
        );
    }

    private async Task LoadTextureAsync(string path, Action<Texture2d> setter, CancellationToken ct)
    {
        var texture = await Assets.LoadTextureAsync(ResolvePath(path), ct);
        setter(texture);
    }

    private string ResolvePath(string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
        {
            return relativePath;
        }

        return Path.Combine(_basePath, relativePath).Replace('\\', '/');
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
