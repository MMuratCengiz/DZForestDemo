using DenOfIz;
using NiziKit.Assets.Loaders;

namespace NiziKit.Assets;

public sealed class Assets(LogicalDevice device) : IDisposable
{
    private readonly BufferPool _vertexPool = new(device, (uint)(BufferUsageFlagBits.Vertex | BufferUsageFlagBits.CopyDst));
    private readonly BufferPool _indexPool = new(device, (uint)(BufferUsageFlagBits.Index | BufferUsageFlagBits.CopyDst));
    private readonly GeometryBuilder _geometryBuilder = new();

    private readonly Dictionary<string, Model> _modelCache = new();
    private readonly Dictionary<string, Texture2d> _textureCache = new();
    private readonly Dictionary<string, Skeleton> _skeletonCache = new();
    private readonly Dictionary<string, Animation> _animationCache = new();
    private readonly Dictionary<string, Mesh> _meshCache = new();
    private readonly Dictionary<string, Material> _materialCache = new();

    private readonly List<Mesh> _meshList = [];
    private readonly List<Texture2d> _textureList = [];
    private readonly List<Material> _materialList = [];

    public Model LoadModel(string path)
    {
        var resolvedPath = AssetPaths.ResolveModel(path);
        if (_modelCache.TryGetValue(resolvedPath, out var cached))
        {
            return cached;
        }

        var model = new Model
        {
            Name = Path.GetFileNameWithoutExtension(path),
            SourcePath = resolvedPath
        };

        if (resolvedPath.EndsWith(".dzmesh", StringComparison.OrdinalIgnoreCase))
        {
            var mesh = MeshCreator.LoadDzMesh(resolvedPath, device, _vertexPool, _indexPool);
            model.Meshes.Add(mesh);
        }

        _modelCache[resolvedPath] = model;
        return model;
    }

    public Mesh LoadMesh(string path)
    {
        var resolvedPath = AssetPaths.ResolveMesh(path);
        if (_meshCache.TryGetValue(resolvedPath, out var cached))
        {
            return cached;
        }

        var mesh = MeshCreator.LoadDzMesh(resolvedPath, device, _vertexPool, _indexPool);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);
        _meshCache[resolvedPath] = mesh;
        return mesh;
    }

    public Texture2d LoadTexture(string path)
    {
        var resolvedPath = AssetPaths.ResolveTexture(path);
        if (_textureCache.TryGetValue(resolvedPath, out var cached))
        {
            return cached;
        }

        var texture = TextureLoader.Load(resolvedPath, device);
        texture.Index = (uint)_textureList.Count;
        _textureList.Add(texture);
        _textureCache[resolvedPath] = texture;
        return texture;
    }

    public Skeleton LoadSkeleton(string path)
    {
        var resolvedPath = AssetPaths.ResolveSkeleton(path);
        if (_skeletonCache.TryGetValue(resolvedPath, out var cached))
        {
            return cached;
        }

        var skeleton = SkeletonLoader.Load(resolvedPath);
        _skeletonCache[resolvedPath] = skeleton;
        return skeleton;
    }

    public Animation LoadAnimation(string path, Skeleton skeleton)
    {
        var resolvedPath = AssetPaths.ResolveAnimation(path);
        var cacheKey = $"{resolvedPath}:{skeleton.Name}";
        if (_animationCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var animation = AnimationLoader.Load(resolvedPath, skeleton);
        _animationCache[cacheKey] = animation;
        return animation;
    }

    public Mesh CreateBox(float width, float height, float depth)
    {
        var cacheKey = $"box:{width}:{height}:{depth}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var geometry = _geometryBuilder.BuildBox(width, height, depth);
        var mesh = MeshCreator.CreateFromGeometry($"Box_{width}x{height}x{depth}", geometry, device, _vertexPool, _indexPool);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);
        _meshCache[cacheKey] = mesh;
        return mesh;
    }

    public Mesh CreateSphere(float diameter, uint tessellation = 16)
    {
        var cacheKey = $"sphere:{diameter}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var geometry = _geometryBuilder.BuildSphere(diameter, tessellation);
        var mesh = MeshCreator.CreateFromGeometry($"Sphere_{diameter}", geometry, device, _vertexPool, _indexPool);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);
        _meshCache[cacheKey] = mesh;
        return mesh;
    }

    public Mesh CreateQuad(float width, float height)
    {
        var cacheKey = $"quad:{width}:{height}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        
        var desc = new QuadDesc
        {
            Width = width,
            Height = height,
            BuildDesc = 0
        };
        var geometry = Geometry.BuildQuadXY(in desc);
        var mesh = MeshCreator.CreateFromGeometry($"Quad_{width}x{height}", geometry, device, _vertexPool, _indexPool);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);
        _meshCache[cacheKey] = mesh;
        return mesh;
    }

    public Mesh CreateCylinder(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"cylinder:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var geometry = _geometryBuilder.BuildCylinder(diameter, height, tessellation);
        var mesh = MeshCreator.CreateFromGeometry($"Cylinder_{diameter}x{height}", geometry, device, _vertexPool, _indexPool);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);
        _meshCache[cacheKey] = mesh;
        return mesh;
    }

    public Mesh CreateCone(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"cone:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var geometry = _geometryBuilder.BuildCone(diameter, height, tessellation);
        var mesh = MeshCreator.CreateFromGeometry($"Cone_{diameter}x{height}", geometry, device, _vertexPool, _indexPool);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);
        _meshCache[cacheKey] = mesh;
        return mesh;
    }

    public Mesh CreateTorus(float diameter, float thickness, uint tessellation = 16)
    {
        var cacheKey = $"torus:{diameter}:{thickness}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var geometry = _geometryBuilder.BuildTorus(diameter, thickness, tessellation);
        var mesh = MeshCreator.CreateFromGeometry($"Torus_{diameter}x{thickness}", geometry, device, _vertexPool, _indexPool);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);
        _meshCache[cacheKey] = mesh;
        return mesh;
    }

    public IReadOnlyList<Mesh> AllMeshes => _meshList;
    public IReadOnlyList<Texture2d> AllTextures => _textureList;
    public IReadOnlyList<Material> AllMaterials => _materialList;

    public Mesh? GetMeshById(Graphics.Batching.MeshId meshId)
    {
        if (!meshId.IsValid || meshId.Index >= _meshList.Count)
        {
            return null;
        }
        return _meshList[(int)meshId.Index];
    }

    public void RegisterMaterial(Material material)
    {
        if (!_materialCache.TryAdd(material.Name, material))
        {
            return;
        }

        _materialList.Add(material);
    }

    public Material? GetMaterial(string name)
    {
        return _materialCache.GetValueOrDefault(name);
    }

    public void Dispose()
    {
        foreach (var model in _modelCache.Values)
        {
            model.Dispose();
        }
        _modelCache.Clear();

        foreach (var texture in _textureCache.Values)
        {
            texture.Dispose();
        }
        _textureCache.Clear();

        foreach (var skeleton in _skeletonCache.Values)
        {
            skeleton.Dispose();
        }
        _skeletonCache.Clear();

        foreach (var animation in _animationCache.Values)
        {
            animation.Dispose();
        }
        _animationCache.Clear();

        foreach (var mesh in _meshCache.Values)
        {
            mesh.Dispose();
        }
        _meshCache.Clear();

        foreach (var material in _materialCache.Values)
        {
            material.Dispose();
        }
        _materialCache.Clear();

        _vertexPool.Dispose();
        _indexPool.Dispose();
    }
}
