using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets.Store;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public sealed class NiziAssets : IDisposable
{
    private static NiziAssets? _instance;
    private static NiziAssets Instance => _instance ?? throw new InvalidOperationException("Assets not initialized");

    private readonly BufferPool _vertexPool;
    private readonly BufferPool _indexPool;

    private readonly ConcurrentDictionary<string, Texture2d> _textureCache = new();
    private readonly ConcurrentDictionary<string, Skeleton> _skeletonCache = new();
    private readonly ConcurrentDictionary<string, Mesh> _meshCache = new();
    private readonly ShaderStore _shaderStore = new();

    private readonly List<Mesh> _meshList = [];
    private readonly List<Texture2d> _textureList = [];
    private readonly Lock _listLock = new();
    private readonly SemaphoreSlim _shaderLoadSemaphore = new(1, 1);

    public NiziAssets()
    {
        _vertexPool = new BufferPool(GraphicsContext.Device,
            (uint)(BufferUsageFlagBits.Vertex | BufferUsageFlagBits.CopyDst));
        _indexPool = new BufferPool(GraphicsContext.Device,
            (uint)(BufferUsageFlagBits.Index | BufferUsageFlagBits.CopyDst));

        var defaultShader = new DefaultShader();
        _shaderStore.Register("Builtin/Shaders/Default", defaultShader.StaticVariant);
        _shaderStore.Register("Builtin/Shaders/Default_SKINNED", defaultShader.SkinnedVariant);

        _instance = this;
    }

    public static Texture2d LoadTexture(string path) => Instance._LoadTexture(path);
    public static Task<Texture2d> LoadTextureAsync(string path, CancellationToken ct = default) => Instance._LoadTextureAsync(path, ct);
    public static Skeleton LoadSkeleton(string path) => Instance._LoadSkeleton(path);
    public static Task<Skeleton> LoadSkeletonAsync(string path, CancellationToken ct = default) => Instance._LoadSkeletonAsync(path, ct);
    public static void RegisterShader(string name, GpuShader shader) => Instance._RegisterShader(name, shader);
    internal static VertexBufferView UploadVertices(byte[] data, VertexFormat format) => Instance._UploadVertices(data, format);
    public static GpuShader? GetShader(string name) => Instance._GetShader(name);
    public static GpuShader? GetShader(string name, ReadOnlySpan<string> variants) => Instance._GetShader(name, variants);
    public static ShaderProgram? GetShaderProgram(string name) => Instance._GetShaderProgram(name);
    public static void ClearShaderCache() => Instance._shaderStore.ClearDiskCache();
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

    private void _Upload(Mesh mesh)
    {
        if (mesh.IsUploaded)
        {
            return;
        }

        if (mesh is { CpuVertices: not null, CpuIndices: not null })
        {
            mesh.VertexBuffer = _UploadVertices(mesh.CpuVertices, mesh.Format);
            mesh.IndexBuffer = UploadIndices(mesh.CpuIndices);
            mesh.CpuVertices = null;
            mesh.CpuIndices = null;
        }
        else if (mesh.SourceAttributes != null)
        {
            var packed = VertexPacker.Pack(mesh.SourceAttributes, mesh.Format);
            mesh.VertexBuffer = _UploadVertices(packed, mesh.Format);
            mesh.IndexBuffer = UploadIndices(mesh.SourceAttributes.Indices);
        }
    }

    private Mesh _Register(Mesh mesh, string? cacheKey = null)
    {
        if (cacheKey != null && _meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        _Upload(mesh);

        lock (_listLock)
        {
            mesh.Index = (uint)_meshList.Count;
            _meshList.Add(mesh);
        }

        if (cacheKey != null)
        {
            _meshCache.TryAdd(cacheKey, mesh);
        }

        return mesh;
    }

    private VertexBufferView _UploadVertices(byte[] data, VertexFormat format)
    {
        var numBytes = (uint)data.Length;
        var gpuView = _vertexPool.Allocate(numBytes);

        lock (GraphicsContext.TransferLock)
        {
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
        }

        var vertexCount = (uint)(data.Length / format.Stride);
        return new VertexBufferView(gpuView, (uint)format.Stride, vertexCount);
    }

    private IndexBufferView UploadIndices(uint[] indices)
    {
        var numBytes = (uint)(sizeof(uint) * indices.Length);
        var gpuView = _indexPool.Allocate(numBytes);

        lock (GraphicsContext.TransferLock)
        {
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
        }

        return new IndexBufferView(gpuView, IndexType.Uint32, (uint)indices.Length);
    }

    private Texture2d _LoadTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = new Texture2d();
        texture.Load(path);

        if (_textureCache.TryAdd(path, texture))
        {
            lock (_listLock)
            {
                _textureList.Add(texture);
            }
        }

        return _textureCache[path];
    }

    private async Task<Texture2d> _LoadTextureAsync(string path, CancellationToken ct = default)
    {
        if (_textureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = new Texture2d();
        await texture.LoadAsync(path, ct);

        if (_textureCache.TryAdd(path, texture))
        {
            lock (_listLock)
            {
                _textureList.Add(texture);
            }
        }

        return _textureCache[path];
    }

    private Skeleton _LoadSkeleton(string path)
    {
        if (_skeletonCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var bytes = Content.ReadBytes(path);
        var skeleton = Skeleton.Load(bytes);
        _skeletonCache.TryAdd(path, skeleton);
        return skeleton;
    }

    private async Task<Skeleton> _LoadSkeletonAsync(string path, CancellationToken ct = default)
    {
        if (_skeletonCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var bytes = await Content.ReadBytesAsync(path, ct);
        var skeleton = Skeleton.Load(bytes);
        _skeletonCache.TryAdd(path, skeleton);
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

    private GpuShader? _GetShader(string name, ReadOnlySpan<string> variants)
    {
        return _shaderStore.Get(name, variants);
    }

    private ShaderProgram? _GetShaderProgram(string name)
    {
        return _shaderStore.GetProgram(name);
    }

    private Mesh _CreateBox(float width, float height, float depth)
    {
        var cacheKey = $"geometry:box:{width}:{height}:{depth}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var mesh = GeometryMesh.Box(width, height, depth);
        mesh.AssetPath = cacheKey;
        return _Register(mesh, cacheKey);
    }

    private Mesh _CreateSphere(float diameter, uint tessellation = 16)
    {
        var cacheKey = $"geometry:sphere:{diameter}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var mesh = GeometryMesh.Sphere(diameter, tessellation);
        mesh.AssetPath = cacheKey;
        return _Register(mesh, cacheKey);
    }

    private Mesh _CreateQuad(float width, float height)
    {
        var cacheKey = $"geometry:quad:{width}:{height}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var mesh = GeometryMesh.Quad(width, height);
        mesh.AssetPath = cacheKey;
        return _Register(mesh, cacheKey);
    }

    private Mesh _CreateCylinder(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"geometry:cylinder:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var mesh = GeometryMesh.Cylinder(diameter, height, tessellation);
        mesh.AssetPath = cacheKey;
        return _Register(mesh, cacheKey);
    }

    private Mesh _CreateCone(float diameter, float height, uint tessellation = 16)
    {
        var cacheKey = $"geometry:cone:{diameter}:{height}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var mesh = GeometryMesh.Cone(diameter, height, tessellation);
        mesh.AssetPath = cacheKey;
        return _Register(mesh, cacheKey);
    }

    private Mesh _CreateTorus(float diameter, float thickness, uint tessellation = 16)
    {
        var cacheKey = $"geometry:torus:{diameter}:{thickness}:{tessellation}";
        if (_meshCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var mesh = GeometryMesh.Torus(diameter, thickness, tessellation);
        mesh.AssetPath = cacheKey;
        return _Register(mesh, cacheKey);
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

        _shaderStore.Dispose();
        _vertexPool.Dispose();
        _indexPool.Dispose();
    }
}
