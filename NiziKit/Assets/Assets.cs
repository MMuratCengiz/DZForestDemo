using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets.Loaders;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public sealed class Assets : IDisposable
{
    private readonly BufferPool _vertexPool;
    private readonly BufferPool _indexPool;
    private readonly GraphicsContext _context;

    private readonly Dictionary<string, Model> _modelCache = new();
    private readonly Dictionary<string, Texture2d> _textureCache = new();
    private readonly Dictionary<string, Skeleton> _skeletonCache = new();
    private readonly Dictionary<string, Animation> _animationCache = new();
    private readonly Dictionary<string, Mesh> _meshCache = new();
    private readonly Dictionary<string, Material> _materialCache = new();

    private readonly List<Mesh> _meshList = [];
    private readonly List<Texture2d> _textureList = [];
    private readonly List<Material> _materialList = [];

    public Assets(GraphicsContext context)
    {
        _context = context;
        _vertexPool = new BufferPool(context.LogicalDevice,
            (uint)(BufferUsageFlagBits.Vertex | BufferUsageFlagBits.CopyDst));
        _indexPool = new BufferPool(context.LogicalDevice,
            (uint)(BufferUsageFlagBits.Index | BufferUsageFlagBits.CopyDst));

        _materialCache["Builtin/Default"] = new DefaultMaterial(context);
    }

    public void Upload(Mesh mesh)
    {
        if (mesh.IsUploaded || mesh.CpuVertices == null || mesh.CpuIndices == null)
        {
            return;
        }

        mesh.VertexBuffer = UploadVertices(mesh.CpuVertices, mesh.Format);
        mesh.IndexBuffer = UploadIndices(mesh.CpuIndices);

        mesh.CpuVertices = null;
        mesh.CpuIndices = null;
    }

    public Mesh Register(Mesh mesh, string? cacheKey = null)
    {
        if (cacheKey != null && _meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        Upload(mesh);
        mesh.Index = (uint)_meshList.Count;
        _meshList.Add(mesh);

        if (cacheKey != null)
        {
            _meshCache[cacheKey] = mesh;
        }

        return mesh;
    }

    private VertexBufferView UploadVertices(byte[] data, VertexFormat format)
    {
        var numBytes = (uint)data.Length;
        var gpuView = _vertexPool.Allocate(numBytes);

        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = _context.LogicalDevice,
            IssueBarriers = false
        });
        batchCopy.Begin();

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
            {
                Data = new ByteArrayView
                {
                    Elements = handle.AddrOfPinnedObject(),
                    NumElements = numBytes
                },
                DstBuffer = gpuView.Buffer,
                DstBufferOffset = gpuView.Offset
            });
        }
        finally
        {
            handle.Free();
        }

        batchCopy.Submit(null);
        batchCopy.Dispose();

        var vertexCount = (uint)(data.Length / format.Stride);
        return new VertexBufferView(gpuView, (uint)format.Stride, vertexCount);
    }

    private IndexBufferView UploadIndices(uint[] indices)
    {
        var numBytes = (uint)(sizeof(uint) * indices.Length);
        var gpuView = _indexPool.Allocate(numBytes);

        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = _context.LogicalDevice,
            IssueBarriers = false
        });
        batchCopy.Begin();

        var handle = GCHandle.Alloc(indices, GCHandleType.Pinned);
        try
        {
            batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
            {
                Data = new ByteArrayView
                {
                    Elements = handle.AddrOfPinnedObject(),
                    NumElements = numBytes
                },
                DstBuffer = gpuView.Buffer,
                DstBufferOffset = gpuView.Offset
            });
        }
        finally
        {
            handle.Free();
        }

        batchCopy.Submit(null);
        batchCopy.Dispose();

        return new IndexBufferView(gpuView, IndexType.Uint32, (uint)indices.Length);
    }

    public Model LoadModel(string path)
    {
        if (_modelCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var model = new Model();
        model.Load(_context, path);

        foreach (var mesh in model.Meshes)
        {
            Register(mesh, $"{path}:{mesh.Name}");
        }

        _modelCache[path] = model;
        return model;
    }

    public async Task<Model> LoadModelAsync(string path, CancellationToken ct = default)
    {
        if (_modelCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var model = new Model();
        await model.LoadAsync(_context, path, ct);

        foreach (var mesh in model.Meshes)
        {
            Register(mesh, $"{path}:{mesh.Name}");
        }

        _modelCache[path] = model;
        return model;
    }

    public Texture2d LoadTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = new Texture2d();
        texture.Load(_context, path);
        _textureList.Add(texture);
        _textureCache[path] = texture;
        return texture;
    }

    public async Task<Texture2d> LoadTextureAsync(string path, CancellationToken ct = default)
    {
        if (_textureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = new Texture2d();
        await texture.LoadAsync(_context, path, ct);
        _textureList.Add(texture);
        _textureCache[path] = texture;
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

    public Material LoadMaterial(string path)
    {
        if (_materialCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        throw new NotImplementedException();
    }

    public Mesh CreateBox(float width, float height, float depth)
    {
        var cacheKey = $"box:{width}:{height}:{depth}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return Register(GeometryMesh.Box(width, height, depth), cacheKey);
    }

    public Mesh CreateSphere(float diameter, uint tessellation = 16)
    {
        var cacheKey = $"sphere:{diameter}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return Register(GeometryMesh.Sphere(diameter, tessellation), cacheKey);
    }

    public Mesh CreateQuad(float width, float height)
    {
        var cacheKey = $"quad:{width}:{height}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return Register(GeometryMesh.Quad(width, height), cacheKey);
    }

    public Mesh CreateCylinder(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"cylinder:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return Register(GeometryMesh.Cylinder(diameter, height, tessellation), cacheKey);
    }

    public Mesh CreateCone(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"cone:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return Register(GeometryMesh.Cone(diameter, height, tessellation), cacheKey);
    }

    public Mesh CreateTorus(float diameter, float thickness, uint tessellation = 16)
    {
        var cacheKey = $"torus:{diameter}:{thickness}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return Register(GeometryMesh.Torus(diameter, thickness, tessellation), cacheKey);
    }

    public IReadOnlyList<Mesh> AllMeshes => _meshList;
    public IReadOnlyList<Texture2d> AllTextures => _textureList;
    public IReadOnlyList<Material> AllMaterials => _materialList;

    public Mesh? GetMeshByIndex(uint index)
    {
        if (index >= _meshList.Count)
        {
            return null;
        }

        return _meshList[(int)index];
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
