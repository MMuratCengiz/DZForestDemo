using NiziKit.Assets.Pack;
using NiziKit.Assets.Serde;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public sealed class JsonMaterial : Material
{
    private readonly MaterialJson _json;
    private readonly string _basePath;
    private readonly IAssetPackProvider? _provider;
    private readonly AssetPack? _owningPack;
    private bool _shaderLoaded;
    private bool _texturesLoaded;
    private GpuShader? _lazyShader;

    public JsonMaterial(MaterialJson json, string basePath)
    {
        _json = json;
        _basePath = basePath;
        _provider = null;
        _owningPack = null;
        Name = json.Name;
        Variant = json.GetVariant();
    }

    internal JsonMaterial(MaterialJson json, string basePath, IAssetPackProvider? provider, AssetPack? owningPack)
    {
        _json = json;
        _basePath = basePath;
        _provider = provider;
        _owningPack = owningPack;
        Name = json.Name;
        Variant = json.GetVariant();
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
            var shaderPath = ResolveShaderPath(_json.Shader);
            _lazyShader = Assets.LoadShaderFromJson(shaderPath, Variant);
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
            base.Albedo = LoadTexture(_json.Textures.Albedo);
        }

        if (!string.IsNullOrEmpty(_json.Textures.Normal))
        {
            base.Normal = LoadTexture(_json.Textures.Normal);
        }

        if (!string.IsNullOrEmpty(_json.Textures.Metallic))
        {
            base.Metallic = LoadTexture(_json.Textures.Metallic);
        }

        if (!string.IsNullOrEmpty(_json.Textures.Roughness))
        {
            base.Roughness = LoadTexture(_json.Textures.Roughness);
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
            var shaderPath = ResolveShaderPath(_json.Shader);
            _lazyShader = await Assets.LoadShaderFromJsonAsync(shaderPath, Variant, ct);
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

    private Texture2d LoadTexture(string reference)
    {
        var colonIdx = reference.IndexOf(':');
        if (colonIdx > 0 && !reference.StartsWith("Builtin/"))
        {
            var packName = reference[..colonIdx];
            var assetName = reference[(colonIdx + 1)..];

            if (_owningPack != null && _owningPack.Name.Equals(packName, StringComparison.OrdinalIgnoreCase))
            {
                return _owningPack.GetTexture(assetName);
            }

            return AssetPacks.GetTexture(packName, assetName);
        }

        if (_provider != null && _owningPack != null)
        {
            var bytes = _provider.ReadBytes(reference);
            var tex = new Texture2d();
            tex.LoadFromBytes(reference, bytes);
            return tex;
        }

        return Assets.LoadTexture(ResolvePath(reference));
    }

    private async Task LoadTextureAsync(string reference, Action<Texture2d> setter, CancellationToken ct)
    {
        var colonIdx = reference.IndexOf(':');
        if (colonIdx > 0 && !reference.StartsWith("Builtin/"))
        {
            var packName = reference[..colonIdx];
            var assetName = reference[(colonIdx + 1)..];

            if (_owningPack != null && _owningPack.Name.Equals(packName, StringComparison.OrdinalIgnoreCase))
            {
                setter(_owningPack.GetTexture(assetName));
                return;
            }

            setter(AssetPacks.GetTexture(packName, assetName));
            return;
        }

        if (_provider != null && _owningPack != null)
        {
            var bytes = await _provider.ReadBytesAsync(reference, ct);
            var tex = new Texture2d();
            tex.LoadFromBytes(reference, bytes);
            setter(tex);
            return;
        }

        var texture = await Assets.LoadTextureAsync(ResolvePath(reference), ct);
        setter(texture);
    }

    private string ResolveShaderPath(string shaderRef)
    {
        if (Path.IsPathRooted(shaderRef))
        {
            return shaderRef;
        }

        if (_provider != null && _owningPack != null)
        {
            return shaderRef;
        }

        return Path.Combine(_basePath, shaderRef).Replace('\\', '/');
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
