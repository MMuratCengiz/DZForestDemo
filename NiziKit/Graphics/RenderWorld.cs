using System.Runtime.InteropServices;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Graphics;

public struct RenderObject
{
    public Material Material;
    public Mesh Mesh;
    public GameObject Owner;
}

public class RenderWorld : IWorldEventListener
{
    private readonly List<Material> _materials = new(64);
    private readonly Dictionary<Material, Dictionary<Mesh, DrawBatch>> _materialMeshBuckets = new(64);
    private readonly Dictionary<object, (Material material, Mesh mesh, int index)> _objectLookup = new(256);

    private readonly List<DrawBatch> _batchCache = new(64);

    public void SceneReset()
    {
        _materials.Clear();
        _materialMeshBuckets.Clear();
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
        if (component is MeshComponent or MaterialComponent)
        {
            TryRegister(go);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        if (component is MeshComponent or MaterialComponent)
        {
            Unregister(go);
        }
    }

    public void ComponentChanged(GameObject go, IComponent component)
    {
        if (component is MeshComponent or MaterialComponent)
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

        var materialComp = go.GetComponent<MaterialComponent>();
        var meshComp = go.GetComponent<MeshComponent>();

        var material = materialComp?.Material;
        var mesh = meshComp?.Mesh;
        if (material == null || mesh == null)
        {
            return;
        }

        if (!_materialMeshBuckets.TryGetValue(material, out var meshBuckets))
        {
            meshBuckets = new Dictionary<Mesh, DrawBatch>(16);
            _materialMeshBuckets[material] = meshBuckets;
            _materials.Add(material);
        }

        if (!meshBuckets.TryGetValue(mesh, out var batch))
        {
            batch = new DrawBatch(mesh);
            meshBuckets[mesh] = batch;
        }

        var renderObj = new RenderObject
        {
            Material = material,
            Mesh = mesh,
            Owner = go
        };

        _objectLookup[go] = (material, mesh, batch.Count);
        batch.Add(renderObj);
    }

    private void Unregister(GameObject go)
    {
        if (!_objectLookup.Remove(go, out var entry))
        {
            return;
        }

        var meshBuckets = _materialMeshBuckets[entry.material];
        var batch = meshBuckets[entry.mesh];
        var lastIndex = batch.Count - 1;

        if (entry.index < lastIndex)
        {
            var swapped = batch.Objects[lastIndex];
            _objectLookup[swapped.Owner] = (entry.material, entry.mesh, entry.index);
        }

        batch.RemoveAt(entry.index);

        if (batch.Count == 0)
        {
            meshBuckets.Remove(entry.mesh);

            if (meshBuckets.Count == 0)
            {
                _materialMeshBuckets.Remove(entry.material);
                _materials.Remove(entry.material);
            }
        }
    }

    public ReadOnlySpan<Material> GetMaterials()
    {
        return CollectionsMarshal.AsSpan(_materials);
    }

    public ReadOnlySpan<DrawBatch> GetDrawBatches(Material material)
    {
        _batchCache.Clear();

        if (!_materialMeshBuckets.TryGetValue(material, out var meshBuckets))
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