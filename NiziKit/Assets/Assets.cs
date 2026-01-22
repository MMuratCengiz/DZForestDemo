using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets.Serde;
using NiziKit.Assets.Store;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public sealed class Assets : IDisposable
{
    private static Assets? _instance;
    public static Assets Instance => _instance ?? throw new InvalidOperationException("Assets not initialized");

    private readonly BufferPool _vertexPool;
    private readonly BufferPool _indexPool;

    private readonly ConcurrentDictionary<string, Model> _modelCache = new();
    private readonly ConcurrentDictionary<string, Texture2d> _textureCache = new();
    private readonly ConcurrentDictionary<string, Skeleton> _skeletonCache = new();
    private readonly ConcurrentDictionary<string, Mesh> _meshCache = new();
    private readonly ConcurrentDictionary<string, Material> _materialCache = new();
    private readonly ShaderStore _shaderStore = new();
    private readonly ShaderBuilder _shaderBuilder = new();

    private readonly List<Mesh> _meshList = [];
    private readonly List<Texture2d> _textureList = [];
    private readonly List<Material> _materialList = [];
    private readonly Lock _listLock = new();
    private readonly Lock _shaderLoadLock = new();

    public Assets()
    {
        _vertexPool = new BufferPool(GraphicsContext.Device,
            (uint)(BufferUsageFlagBits.Vertex | BufferUsageFlagBits.CopyDst));
        _indexPool = new BufferPool(GraphicsContext.Device,
            (uint)(BufferUsageFlagBits.Index | BufferUsageFlagBits.CopyDst));

        var defaultShader = new DefaultShader();
        _shaderStore.Register("Builtin/Shaders/Default", defaultShader.StaticVariant);
        _shaderStore.Register(
            ShaderVariants.EncodeName("Builtin/Shaders/Default", "SKINNED"),
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
    public static ShaderProgram? GetShaderProgram(string name) => Instance._GetShaderProgram(name);
    public static void RegisterShaderProgram(string name, ShaderProgram program) => Instance._RegisterShaderProgram(name, program);
    public static ShaderProgram LoadShaderProgram(string vertexPath, string pixelPath, Dictionary<string, string?>? defines = null) => Instance._LoadShaderProgram(vertexPath, pixelPath, defines);
    public static Task<ShaderProgram> LoadShaderProgramAsync(string vertexPath, string pixelPath, Dictionary<string, string?>? defines = null, CancellationToken ct = default) => Instance._LoadShaderProgramAsync(vertexPath, pixelPath, defines, ct);
    public static ShaderProgram LoadComputeProgram(string computePath, Dictionary<string, string?>? defines = null) => Instance._LoadComputeProgram(computePath, defines);
    public static Task<ShaderProgram> LoadComputeProgramAsync(string computePath, Dictionary<string, string?>? defines = null, CancellationToken ct = default) => Instance._LoadComputeProgramAsync(computePath, defines, ct);
    public static GpuShader LoadShader(string vertexPath, string pixelPath, GraphicsPipelineDesc pipelineDesc, Dictionary<string, string?>? defines = null) => Instance._LoadShader(vertexPath, pixelPath, pipelineDesc, defines);
    public static async Task<GpuShader> LoadShaderAsync(string vertexPath, string pixelPath, GraphicsPipelineDesc pipelineDesc, Dictionary<string, string?>? defines = null, CancellationToken ct = default) => GpuShader.Graphics(await Instance._LoadShaderProgramAsync(vertexPath, pixelPath, defines, ct), pipelineDesc, ownsProgram: false);
    public static void ClearShaderCache() => Instance._shaderStore.ClearDiskCache();
    public static Material RegisterMaterial(Material material) => Instance._RegisterMaterial(material);
    public static Material? GetMaterial(string name) => Instance._GetMaterial(name);
    public static Material LoadMaterial(string path) => Instance._LoadMaterial(path);
    public static Task<Material> LoadMaterialAsync(string path, CancellationToken ct = default) => Instance._LoadMaterialAsync(path, ct);
    public static GpuShader LoadShaderFromJson(string path) => Instance._LoadShaderFromJson(path);
    public static Task<GpuShader> LoadShaderFromJsonAsync(string path, CancellationToken ct = default) => Instance._LoadShaderFromJsonAsync(path, ct);
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

    internal VertexBufferView UploadVerticesInternal(byte[] data, VertexFormat format)
    {
        return UploadVertices(data, format);
    }

    private VertexBufferView UploadVertices(byte[] data, VertexFormat format)
    {
        var numBytes = (uint)data.Length;
        var gpuView = _vertexPool.Allocate(numBytes);

        lock (GraphicsContext.GpuLock)
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

        lock (GraphicsContext.GpuLock)
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

        _modelCache.TryAdd(path, model);
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

        _modelCache.TryAdd(path, model);
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

    private Skeleton _LoadSkeleton(string modelPath)
    {
        if (_skeletonCache.TryGetValue(modelPath, out var cached))
        {
            return cached;
        }

        var skeleton = Skeleton.Load(modelPath);
        _skeletonCache.TryAdd(modelPath, skeleton);
        return skeleton;
    }

    private async Task<Skeleton> _LoadSkeletonAsync(string modelPath, CancellationToken ct = default)
    {
        if (_skeletonCache.TryGetValue(modelPath, out var cached))
        {
            return cached;
        }

        var skeleton = await Skeleton.LoadAsync(modelPath, ct);
        _skeletonCache.TryAdd(modelPath, skeleton);
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

    private ShaderProgram? _GetShaderProgram(string name)
    {
        return _shaderStore.GetProgram(name);
    }

    private void _RegisterShaderProgram(string name, ShaderProgram program)
    {
        _shaderStore.RegisterProgram(name, program);
    }

    private ShaderProgram _LoadShaderProgram(string vertexPath, string pixelPath, Dictionary<string, string?>? defines)
    {
        var vsFullPath = Path.IsPathRooted(vertexPath) ? vertexPath : ContentPipeline.Content.ResolvePath(vertexPath);
        var psFullPath = Path.IsPathRooted(pixelPath) ? pixelPath : ContentPipeline.Content.ResolvePath(pixelPath);

        var cacheKey = _shaderStore.ComputeCacheKey([vsFullPath, psFullPath], defines);

        var cached = _shaderStore.GetProgram(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var diskCached = _shaderStore.TryLoadFromDisk(cacheKey);
        if (diskCached != null)
        {
            _shaderStore.RegisterProgram(cacheKey, diskCached);
            return diskCached;
        }

        var program = _shaderBuilder.CompileGraphics(vsFullPath, psFullPath, defines: defines);

        _shaderStore.SaveToDisk(cacheKey, program);
        _shaderStore.RegisterProgram(cacheKey, program);

        return program;
    }

    private ShaderProgram _LoadComputeProgram(string computePath, Dictionary<string, string?>? defines)
    {
        var csFullPath = Path.IsPathRooted(computePath) ? computePath : ContentPipeline.Content.ResolvePath(computePath);

        var cacheKey = _shaderStore.ComputeCacheKey([csFullPath], defines);

        var cached = _shaderStore.GetProgram(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var diskCached = _shaderStore.TryLoadFromDisk(cacheKey);
        if (diskCached != null)
        {
            _shaderStore.RegisterProgram(cacheKey, diskCached);
            return diskCached;
        }

        var program = _shaderBuilder.CompileCompute(csFullPath, defines: defines);

        _shaderStore.SaveToDisk(cacheKey, program);
        _shaderStore.RegisterProgram(cacheKey, program);

        return program;
    }

    private GpuShader _LoadShader(string vertexPath, string pixelPath, GraphicsPipelineDesc pipelineDesc, Dictionary<string, string?>? defines)
    {
        var program = _LoadShaderProgram(vertexPath, pixelPath, defines);
        return GpuShader.Graphics(program, pipelineDesc, ownsProgram: false);
    }

    private async Task<ShaderProgram> _LoadShaderProgramAsync(string vertexPath, string pixelPath, Dictionary<string, string?>? defines, CancellationToken ct)
    {
        var vsFullPath = Path.IsPathRooted(vertexPath) ? vertexPath : ContentPipeline.Content.ResolvePath(vertexPath);
        var psFullPath = Path.IsPathRooted(pixelPath) ? pixelPath : ContentPipeline.Content.ResolvePath(pixelPath);

        var cacheKey = _shaderStore.ComputeCacheKey([vsFullPath, psFullPath], defines);

        var cached = _shaderStore.GetProgram(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var diskCached = _shaderStore.TryLoadFromDisk(cacheKey);
        if (diskCached != null)
        {
            _shaderStore.RegisterProgram(cacheKey, diskCached);
            return diskCached;
        }

        var program = await _shaderBuilder.CompileGraphicsAsync(vsFullPath, psFullPath, defines: defines, ct: ct);

        _shaderStore.SaveToDisk(cacheKey, program);
        _shaderStore.RegisterProgram(cacheKey, program);

        return program;
    }

    private async Task<ShaderProgram> _LoadComputeProgramAsync(string computePath, Dictionary<string, string?>? defines, CancellationToken ct)
    {
        var csFullPath = Path.IsPathRooted(computePath) ? computePath : ContentPipeline.Content.ResolvePath(computePath);

        var cacheKey = _shaderStore.ComputeCacheKey([csFullPath], defines);

        var cached = _shaderStore.GetProgram(cacheKey);
        if (cached != null)
        {
            return cached;
        }

        var diskCached = _shaderStore.TryLoadFromDisk(cacheKey);
        if (diskCached != null)
        {
            _shaderStore.RegisterProgram(cacheKey, diskCached);
            return diskCached;
        }

        var program = await _shaderBuilder.CompileComputeAsync(csFullPath, defines: defines, ct: ct);

        _shaderStore.SaveToDisk(cacheKey, program);
        _shaderStore.RegisterProgram(cacheKey, program);

        return program;
    }

    private Material _RegisterMaterial(Material material)
    {
        if (!_materialCache.TryAdd(material.Name, material))
        {
            return _materialCache[material.Name];
        }

        lock (_listLock)
        {
            _materialList.Add(material);
        }

        return material;
    }

    private Material? _GetMaterial(string name)
    {
        return _materialCache.GetValueOrDefault(name);
    }

    private Material _LoadMaterial(string path)
    {
        var fullPath = ContentPipeline.Content.ResolvePath(path);

        if (_materialCache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        var json = ContentPipeline.Content.ReadText(path);
        var materialJson = MaterialJson.FromJson(json);

        var basePath = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var material = new JsonMaterial(materialJson, basePath);

        material.EnsureShaderLoaded();
        material.EnsureTexturesLoaded();

        if (_materialCache.TryAdd(fullPath, material))
        {
            lock (_listLock)
            {
                _materialList.Add(material);
            }
        }

        return _materialCache[fullPath];
    }

    private async Task<Material> _LoadMaterialAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ContentPipeline.Content.ResolvePath(path);

        if (_materialCache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        var json = await ContentPipeline.Content.ReadTextAsync(path, ct);
        var materialJson = MaterialJson.FromJson(json);

        var basePath = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var material = new JsonMaterial(materialJson, basePath);

        await material.LoadAllAsync(ct);

        if (_materialCache.TryAdd(fullPath, material))
        {
            lock (_listLock)
            {
                _materialList.Add(material);
            }
        }

        return _materialCache[fullPath];
    }

    private GpuShader _LoadShaderFromJson(string path)
    {
        var fullPath = ContentPipeline.Content.ResolvePath(path);

        var existingShader = _shaderStore[fullPath];
        if (existingShader != null)
        {
            return existingShader;
        }

        lock (_shaderLoadLock)
        {
            existingShader = _shaderStore[fullPath];
            if (existingShader != null)
            {
                return existingShader;
            }

            var json = ContentPipeline.Content.ReadText(path);
            var shaderJson = ShaderProgramJson.FromJson(json);

            var basePath = Path.GetDirectoryName(fullPath) ?? string.Empty;
            var pipelineType = shaderJson.DetectPipelineType();

            var program = _shaderBuilder.CompileFromJson(shaderJson, basePath);

            GpuShader shader;
            switch (pipelineType)
            {
                case PipelineType.Compute:
                    shader = GpuShader.Compute(program, ownsProgram: false);
                    break;

                case PipelineType.RayTracing:
                    var rtPipelineDesc = new RayTracingPipelineDesc
                    {
                        HitGroups = shaderJson.ToHitGroupDescArray()
                    };
                    var explicitIndices = shaderJson.GetExplicitLocalRootSignatureIndices();
                    shader = GpuShader.RayTracing(program, rtPipelineDesc, ownsProgram: false, explicitIndices);
                    break;

                case PipelineType.Mesh:
                    var meshPipelineDesc = shaderJson.ToMeshPipelineDesc(
                        GraphicsContext.BackBufferFormat,
                        GraphicsContext.DepthBufferFormat);
                    shader = GpuShader.Mesh(program, meshPipelineDesc, ownsProgram: false);
                    break;

                case PipelineType.Graphics:
                default:
                    var graphicsPipelineDesc = shaderJson.ToGraphicsPipelineDesc(
                        GraphicsContext.BackBufferFormat,
                        GraphicsContext.DepthBufferFormat);
                    shader = GpuShader.Graphics(program, graphicsPipelineDesc, ownsProgram: false);
                    break;
            }

            _shaderStore.Register(fullPath, shader);
            return shader;
        }
    }

    private async Task<GpuShader> _LoadShaderFromJsonAsync(string path, CancellationToken ct = default)
    {
        var fullPath = ContentPipeline.Content.ResolvePath(path);

        var existingShader = _shaderStore[fullPath];
        if (existingShader != null)
        {
            return existingShader;
        }

        var json = await ContentPipeline.Content.ReadTextAsync(path, ct);
        var shaderJson = ShaderProgramJson.FromJson(json);

        var basePath = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var pipelineType = shaderJson.DetectPipelineType();

        var program = await _shaderBuilder.CompileFromJsonAsync(shaderJson, basePath, ct);

        GpuShader shader;
        switch (pipelineType)
        {
            case PipelineType.Compute:
                shader = GpuShader.Compute(program, ownsProgram: false);
                break;

            case PipelineType.RayTracing:
                var rtPipelineDesc = new RayTracingPipelineDesc
                {
                    HitGroups = shaderJson.ToHitGroupDescArray()
                };
                var explicitIndicesAsync = shaderJson.GetExplicitLocalRootSignatureIndices();
                shader = GpuShader.RayTracing(program, rtPipelineDesc, ownsProgram: false, explicitIndicesAsync);
                break;

            case PipelineType.Mesh:
                var meshPipelineDesc = shaderJson.ToMeshPipelineDesc(
                    GraphicsContext.BackBufferFormat,
                    GraphicsContext.DepthBufferFormat);
                shader = GpuShader.Mesh(program, meshPipelineDesc, ownsProgram: false);
                break;

            case PipelineType.Graphics:
            default:
                var graphicsPipelineDesc = shaderJson.ToGraphicsPipelineDesc(
                    GraphicsContext.BackBufferFormat,
                    GraphicsContext.DepthBufferFormat);
                shader = GpuShader.Graphics(program, graphicsPipelineDesc, ownsProgram: false);
                break;
        }

        _shaderStore.Register(fullPath, shader);
        return shader;
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
