using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets.Store;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public sealed class Assets : IDisposable
{
    private static Assets? _instance;
    public static Assets Instance => _instance ?? throw new InvalidOperationException("Assets not initialized");

    private readonly BufferPool _vertexPool;
    private readonly BufferPool _indexPool;

    private readonly Dictionary<string, Model> _modelCache = new();
    private readonly Dictionary<string, Texture2d> _textureCache = new();
    private readonly Dictionary<string, Skeleton> _skeletonCache = new();
    private readonly Dictionary<string, Mesh> _meshCache = new();
    private readonly Dictionary<string, Material> _materialCache = new();
    private readonly ShaderStore _shaderStore = new();

    private readonly List<Mesh> _meshList = [];
    private readonly List<Texture2d> _textureList = [];
    private readonly List<Material> _materialList = [];

    public Assets()
    {
        _vertexPool = new BufferPool(GraphicsContext.Device,
            (uint)(BufferUsageFlagBits.Vertex | BufferUsageFlagBits.CopyDst));
        _indexPool = new BufferPool(GraphicsContext.Device,
            (uint)(BufferUsageFlagBits.Index | BufferUsageFlagBits.CopyDst));

        var defaultShader = new DefaultShader();
        _shaderStore.Register("Builtin/Shaders/Default", defaultShader.StaticVariant);
        _shaderStore.Register(
            ShaderVariants.EncodeName("Builtin/Shaders/Default", ShaderVariants.Skinned()),
            defaultShader.SkinnedVariant);
        _materialCache["Builtin/Materials/Default"] = new DefaultMaterial(_shaderStore);

        _instance = this;
    }

    public static Model LoadModel(string path) => Instance._LoadModel(path);
    public static Task<Model> LoadModelAsync(string path, CancellationToken ct = default) => Instance._LoadModelAsync(path, ct);
    public static Texture2d LoadTexture(string path) => Instance._LoadTexture(path);
    public static Task<Texture2d> LoadTextureAsync(string path, CancellationToken ct = default) => Instance._LoadTextureAsync(path, ct);
    public static Skeleton LoadSkeleton(string modelPath) => Instance._LoadSkeleton(modelPath);
    public static Task<Skeleton> LoadSkeletonAsync(string modelPath, CancellationToken ct = default) => Instance._LoadSkeletonAsync(modelPath, ct);
    public static void RegisterShader(string name, GpuShader shader) => Instance._RegisterShader(name, shader);
    public static GpuShader? GetShader(string name) => Instance._GetShader(name);
    public static GpuShader? GetShader(string name, IReadOnlyDictionary<string, string?>? variants) => Instance._GetShader(name, variants);
    public static Material RegisterMaterial(Material material) => Instance._RegisterMaterial(material);
    public static Material? GetMaterial(string name) => Instance._GetMaterial(name);
    public static Mesh CreateBox(float width, float height, float depth) => Instance._CreateBox(width, height, depth);
    public static Mesh CreateSphere(float diameter, uint tessellation = 16) => Instance._CreateSphere(diameter, tessellation);
    public static Mesh CreateQuad(float width, float height) => Instance._CreateQuad(width, height);
    public static Mesh CreateCylinder(float diameter, float height, uint tessellation = 16) => Instance._CreateCylinder(diameter, height, tessellation);
    public static Mesh CreateCone(float diameter, float height, uint tessellation = 16) => Instance._CreateCone(diameter, height, tessellation);
    public static Mesh CreateTorus(float diameter, float thickness, uint tessellation = 16) => Instance._CreateTorus(diameter, thickness, tessellation);
    public static Mesh? GetMeshByIndex(uint index) => Instance._GetMeshByIndex(index);
    public static void Upload(Mesh mesh) => Instance._Upload(mesh);
    public static Mesh Register(Mesh mesh, string? cacheKey = null) => Instance._Register(mesh, cacheKey);

    public static IReadOnlyList<Mesh> AllMeshes => Instance._meshList;
    public static IReadOnlyList<Texture2d> AllTextures => Instance._textureList;
    public static IReadOnlyList<Material> AllMaterials => Instance._materialList;

    private void _Upload(Mesh mesh)
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

    private Mesh _Register(Mesh mesh, string? cacheKey = null)
    {
        if (cacheKey != null && _meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        _Upload(mesh);
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
            Device = GraphicsContext.Device,
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
            Device = GraphicsContext.Device,
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

    private Model _LoadModel(string path)
    {
        if (_modelCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var model = new Model();
        model.Load(path);

        foreach (var mesh in model.Meshes)
        {
            _Register(mesh, $"{path}:{mesh.Name}");
        }

        _modelCache[path] = model;
        return model;
    }

    private async Task<Model> _LoadModelAsync(string path, CancellationToken ct = default)
    {
        if (_modelCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var model = new Model();
        await model.LoadAsync(path, ct);

        foreach (var mesh in model.Meshes)
        {
            _Register(mesh, $"{path}:{mesh.Name}");
        }

        _modelCache[path] = model;
        return model;
    }

    private Texture2d _LoadTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = new Texture2d();
        texture.Load(path);
        _textureList.Add(texture);
        _textureCache[path] = texture;
        return texture;
    }

    private async Task<Texture2d> _LoadTextureAsync(string path, CancellationToken ct = default)
    {
        if (_textureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = new Texture2d();
        await texture.LoadAsync(path, ct);
        _textureList.Add(texture);
        _textureCache[path] = texture;
        return texture;
    }

    private Skeleton _LoadSkeleton(string modelPath)
    {
        if (_skeletonCache.TryGetValue(modelPath, out var cached))
        {
            return cached;
        }

        var skeleton = Skeleton.Load(modelPath);
        _skeletonCache[modelPath] = skeleton;
        return skeleton;
    }

    private async Task<Skeleton> _LoadSkeletonAsync(string modelPath, CancellationToken ct = default)
    {
        if (_skeletonCache.TryGetValue(modelPath, out var cached))
        {
            return cached;
        }

        var skeleton = await Skeleton.LoadAsync(modelPath, ct);
        _skeletonCache[modelPath] = skeleton;
        return skeleton;
    }

    private void _RegisterShader(string name, GpuShader shader)
    {
        _shaderStore.Register(name, shader);
    }

    private GpuShader? _GetShader(string name)
    {
        return _shaderStore[name];
    }

    private GpuShader? _GetShader(string name, IReadOnlyDictionary<string, string?>? variants)
    {
        return _shaderStore.Get(name, variants);
    }

    private Material _RegisterMaterial(Material material)
    {
        if (!_materialCache.TryAdd(material.Name, material))
        {
            return _materialCache[material.Name];
        }

        _materialList.Add(material);
        return material;
    }

    private Material? _GetMaterial(string name)
    {
        return _materialCache.GetValueOrDefault(name);
    }

    private Mesh _CreateBox(float width, float height, float depth)
    {
        var cacheKey = $"box:{width}:{height}:{depth}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return _Register(GeometryMesh.Box(width, height, depth), cacheKey);
    }

    private Mesh _CreateSphere(float diameter, uint tessellation = 16)
    {
        var cacheKey = $"sphere:{diameter}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return _Register(GeometryMesh.Sphere(diameter, tessellation), cacheKey);
    }

    private Mesh _CreateQuad(float width, float height)
    {
        var cacheKey = $"quad:{width}:{height}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return _Register(GeometryMesh.Quad(width, height), cacheKey);
    }

    private Mesh _CreateCylinder(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"cylinder:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return _Register(GeometryMesh.Cylinder(diameter, height, tessellation), cacheKey);
    }

    private Mesh _CreateCone(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"cone:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return _Register(GeometryMesh.Cone(diameter, height, tessellation), cacheKey);
    }

    private Mesh _CreateTorus(float diameter, float thickness, uint tessellation = 16)
    {
        var cacheKey = $"torus:{diameter}:{thickness}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        return _Register(GeometryMesh.Torus(diameter, thickness, tessellation), cacheKey);
    }

    private Mesh? _GetMeshByIndex(uint index)
    {
        if (index >= _meshList.Count)
        {
            return null;
        }

        return _meshList[(int)index];
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

        _shaderStore.Dispose();
        _vertexPool.Dispose();
        _indexPool.Dispose();
    }
}
