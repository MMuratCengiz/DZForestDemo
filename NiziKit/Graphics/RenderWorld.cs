using System.Runtime.InteropServices;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Graphics;

public struct RenderObject
{
    public SurfaceComponent Surface;
    public GpuShader? Shader;
    public Mesh Mesh;
    public GameObject Owner;
}

public class RenderWorld : IWorldEventListener
{
    private readonly List<GpuShader> _shaders = new(64);
    private readonly Dictionary<GpuShader, Dictionary<SurfaceComponent, Dictionary<Mesh, DrawBatch>>> _shaderSurfaceMeshBuckets = new(64);
    private readonly Dictionary<object, (GpuShader? shader, SurfaceComponent surface, Mesh mesh, int index)> _objectLookup = new(256);

    private readonly List<DrawBatch> _batchCache = new(64);
    private readonly List<SurfaceComponent> _surfaceCache = new(64);

    private const string DefaultShaderPath = "Builtin/Shaders/Default";

    public void SceneReset()
    {
        _shaders.Clear();
        _shaderSurfaceMeshBuckets.Clear();
        _objectLookup.Clear();
    }

    public void GameObjectCreated(GameObject go)
    {
        TryRegister(go);
    }

    public void GameObjectDestroyed(GameObject go)
    {
        Unregister(go);
    }

    public void ComponentAdded(GameObject go, IComponent component)
    {
        if (component is MeshComponent or SurfaceComponent or MaterialComponent)
        {
            TryRegister(go);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        if (component is MeshComponent or SurfaceComponent or MaterialComponent)
        {
            Unregister(go);
        }
    }

    public void ComponentChanged(GameObject go, IComponent component)
    {
        if (component is MeshComponent or SurfaceComponent or MaterialComponent)
        {
            Unregister(go);
            TryRegister(go);
        }
    }

    private void TryRegister(GameObject go)
    {
        if (_objectLookup.ContainsKey(go))
        {
            return;
        }

        var surfaceComp = go.GetComponent<SurfaceComponent>();
        var meshComp = go.GetComponent<MeshComponent>();
        var materialComp = go.GetComponent<MaterialComponent>();

        var mesh = meshComp?.Mesh;
        if (surfaceComp == null || mesh == null)
        {
            return;
        }

        var shader = ResolveShader(materialComp);
        if (shader == null)
        {
            return;
        }

        if (!_shaderSurfaceMeshBuckets.TryGetValue(shader, out var surfaceBuckets))
        {
            surfaceBuckets = new Dictionary<SurfaceComponent, Dictionary<Mesh, DrawBatch>>(16);
            _shaderSurfaceMeshBuckets[shader] = surfaceBuckets;
            _shaders.Add(shader);
        }

        if (!surfaceBuckets.TryGetValue(surfaceComp, out var meshBuckets))
        {
            meshBuckets = new Dictionary<Mesh, DrawBatch>(16);
            surfaceBuckets[surfaceComp] = meshBuckets;
        }

        if (!meshBuckets.TryGetValue(mesh, out var batch))
        {
            batch = new DrawBatch(mesh);
            meshBuckets[mesh] = batch;
        }

        var renderObj = new RenderObject
        {
            Surface = surfaceComp,
            Shader = shader,
            Mesh = mesh,
            Owner = go
        };

        _objectLookup[go] = (shader, surfaceComp, mesh, batch.Count);
        batch.Add(renderObj);
    }

    private GpuShader? ResolveShader(MaterialComponent? materialComp)
    {
        if (materialComp?.Tags != null && materialComp.Tags.TryGetValue("shader", out var shaderPath))
        {
            string? variant = null;
            materialComp.Tags.TryGetValue("variant", out variant);

            if (shaderPath.StartsWith("Builtin/"))
            {
                var fullName = string.IsNullOrEmpty(variant) ? shaderPath : $"{shaderPath}_{variant}";
                return NiziKit.Assets.Assets.GetShader(fullName);
            }
            else
            {
                return NiziKit.Assets.Assets.LoadShaderFromJson(shaderPath, variant);
            }
        }

        return NiziKit.Assets.Assets.GetShader(DefaultShaderPath);
    }

    private void Unregister(GameObject go)
    {
        if (!_objectLookup.Remove(go, out var entry))
        {
            return;
        }

        if (entry.shader == null)
        {
            return;
        }

        var surfaceBuckets = _shaderSurfaceMeshBuckets[entry.shader];
        var meshBuckets = surfaceBuckets[entry.surface];
        var batch = meshBuckets[entry.mesh];
        var lastIndex = batch.Count - 1;

        if (entry.index < lastIndex)
        {
            var swapped = batch.Objects[lastIndex];
            _objectLookup[swapped.Owner] = (entry.shader, entry.surface, entry.mesh, entry.index);
        }

        batch.RemoveAt(entry.index);

        if (batch.Count == 0)
        {
            meshBuckets.Remove(entry.mesh);

            if (meshBuckets.Count == 0)
            {
                surfaceBuckets.Remove(entry.surface);

                if (surfaceBuckets.Count == 0)
                {
                    _shaderSurfaceMeshBuckets.Remove(entry.shader);
                    _shaders.Remove(entry.shader);
                }
            }
        }
    }

    public ReadOnlySpan<GpuShader> GetShaders()
    {
        return CollectionsMarshal.AsSpan(_shaders);
    }

    public ReadOnlySpan<SurfaceComponent> GetSurfaces(GpuShader shader)
    {
        _surfaceCache.Clear();

        if (!_shaderSurfaceMeshBuckets.TryGetValue(shader, out var surfaceBuckets))
        {
            return ReadOnlySpan<SurfaceComponent>.Empty;
        }

        foreach (var surface in surfaceBuckets.Keys)
        {
            _surfaceCache.Add(surface);
        }

        return CollectionsMarshal.AsSpan(_surfaceCache);
    }

    public ReadOnlySpan<DrawBatch> GetDrawBatches(GpuShader shader, SurfaceComponent surface)
    {
        _batchCache.Clear();

        if (!_shaderSurfaceMeshBuckets.TryGetValue(shader, out var surfaceBuckets))
        {
            return ReadOnlySpan<DrawBatch>.Empty;
        }

        if (!surfaceBuckets.TryGetValue(surface, out var meshBuckets))
        {
            return ReadOnlySpan<DrawBatch>.Empty;
        }

        foreach (var (_, batch) in meshBuckets)
        {
            _batchCache.Add(batch);
        }

        return CollectionsMarshal.AsSpan(_batchCache);
    }
}
