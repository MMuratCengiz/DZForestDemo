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
    private readonly Dictionary<Material, List<RenderObject>> _materialRenderObjects = new(64);
    private readonly Dictionary<object, (Material material, int index)> _objectLookup = new(256);

    public void SceneReset()
    {
        _materials.Clear();
        _materialRenderObjects.Clear();
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

    private void TryRegister(GameObject go)
    {
        if (_objectLookup.ContainsKey(go))
        {
            return;
        }

        var materialComp = go.GetComponent<MaterialComponent>();
        var meshComp = go.GetComponent<MeshComponent>();

        var material = materialComp?.Material;
        if (material == null || meshComp?.Mesh == null)
        {
            return;
        }
        if (!_materialRenderObjects.TryGetValue(material, out var bucket))
        {
            bucket = new List<RenderObject>(32);
            _materialRenderObjects[material] = bucket;
            _materials.Add(material);
        }

        var renderObj = new RenderObject
        {
            Material = material,
            Mesh = meshComp.Mesh,
            Owner = go
        };

        _objectLookup[go] = (material, bucket.Count);
        bucket.Add(renderObj);
    }

    private void Unregister(GameObject go)
    {
        if (!_objectLookup.Remove(go, out var entry))
        {
            return;
        }

        var bucket = _materialRenderObjects[entry.material];
        var lastIndex = bucket.Count - 1;

        if (entry.index < lastIndex)
        {
            var swapped = bucket[lastIndex];
            bucket[entry.index] = swapped;
            _objectLookup[swapped.Owner] = (entry.material, entry.index);
        }

        bucket.RemoveAt(lastIndex);
        if (bucket.Count == 0)
        {
            _materialRenderObjects.Remove(entry.material);
            _materials.Remove(entry.material);
        }
    }

    public ReadOnlySpan<Material> GetMaterials()
    {
        return CollectionsMarshal.AsSpan(_materials);
    }

    public ReadOnlySpan<RenderObject> GetObjects(Material material)
    {
        if (_materialRenderObjects.TryGetValue(material, out var bucket))
        {
            return CollectionsMarshal.AsSpan(bucket);
        }

        return ReadOnlySpan<RenderObject>.Empty;
    }
}