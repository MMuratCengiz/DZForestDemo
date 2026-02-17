using System.Runtime.InteropServices;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Graphics;

public struct Renderable
{
    public GameObject Owner;
    public MeshComponent MeshComp;
    public SurfaceComponent Surface;
    public MaterialTags? Tags;
}

public class RenderBatch(SurfaceComponent surface, Mesh mesh)
{
    public SurfaceComponent Surface { get; } = surface;
    public Mesh Mesh { get; } = mesh;
    public List<Renderable> Objects { get; } = new(32);

    public int Count => Objects.Count;

    public void Add(Renderable obj)
    {
        Objects.Add(obj);
    }

    public void RemoveAt(int index)
    {
        var lastIndex = Objects.Count - 1;
        if (index < lastIndex)
        {
            Objects[index] = Objects[lastIndex];
        }
        Objects.RemoveAt(lastIndex);
    }

    public ReadOnlySpan<Renderable> AsSpan()
    {
        return CollectionsMarshal.AsSpan(Objects);
    }
}

public class RenderWorld : IWorldEventListener
{
    private readonly Dictionary<SurfaceComponent, Dictionary<Mesh, RenderBatch>> _surfaceMeshBatches = new(64);
    private readonly Dictionary<object, (SurfaceComponent surface, Mesh mesh, int index)> _objectLookup = new(256);

    private readonly List<RenderBatch> _batchCache = new(64);
    private readonly List<SurfaceComponent> _surfaceCache = new(64);

    public void SceneReset()
    {
        _surfaceMeshBatches.Clear();
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

    public void ComponentAdded(GameObject go, NiziComponent component)
    {
        if (component is MeshComponent or SurfaceComponent or MaterialComponent)
        {
            TryRegister(go);
        }
    }

    public void ComponentRemoved(GameObject go, NiziComponent component)
    {
        if (component is MeshComponent or SurfaceComponent or MaterialComponent)
        {
            Unregister(go);
        }
    }

    public void ComponentChanged(GameObject go, NiziComponent component)
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

        if (!_surfaceMeshBatches.TryGetValue(surfaceComp, out var meshBatches))
        {
            meshBatches = new Dictionary<Mesh, RenderBatch>(16);
            _surfaceMeshBatches[surfaceComp] = meshBatches;
        }

        if (!meshBatches.TryGetValue(mesh, out var batch))
        {
            batch = new RenderBatch(surfaceComp, mesh);
            meshBatches[mesh] = batch;
        }

        var renderable = new Renderable
        {
            Owner = go,
            MeshComp = meshComp!,
            Surface = surfaceComp,
            Tags = materialComp?.Tags
        };

        _objectLookup[go] = (surfaceComp, mesh, batch.Count);
        batch.Add(renderable);
    }

    private void Unregister(GameObject go)
    {
        if (!_objectLookup.Remove(go, out var entry))
        {
            return;
        }

        var meshBatches = _surfaceMeshBatches[entry.surface];
        var batch = meshBatches[entry.mesh];
        var lastIndex = batch.Count - 1;

        if (entry.index < lastIndex)
        {
            var swapped = batch.Objects[lastIndex];
            _objectLookup[swapped.Owner] = (entry.surface, entry.mesh, entry.index);
        }

        batch.RemoveAt(entry.index);

        if (batch.Count == 0)
        {
            meshBatches.Remove(entry.mesh);

            if (meshBatches.Count == 0)
            {
                _surfaceMeshBatches.Remove(entry.surface);
            }
        }
    }

    public ReadOnlySpan<SurfaceComponent> GetSurfaces()
    {
        _surfaceCache.Clear();
        foreach (var surface in _surfaceMeshBatches.Keys)
        {
            _surfaceCache.Add(surface);
        }
        return CollectionsMarshal.AsSpan(_surfaceCache);
    }

    public ReadOnlySpan<RenderBatch> GetBatches(SurfaceComponent surface)
    {
        _batchCache.Clear();

        if (!_surfaceMeshBatches.TryGetValue(surface, out var meshBatches))
        {
            return ReadOnlySpan<RenderBatch>.Empty;
        }

        foreach (var (_, batch) in meshBatches)
        {
            _batchCache.Add(batch);
        }

        return CollectionsMarshal.AsSpan(_batchCache);
    }
}
