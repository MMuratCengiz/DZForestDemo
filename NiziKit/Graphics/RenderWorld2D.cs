using System.Runtime.InteropServices;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Graphics;

public struct Renderable2D
{
    public GameObject Owner;
    public SpriteComponent Sprite;
}

public class RenderWorld2D : IWorldEventListener
{
    private readonly List<Renderable2D> _sprites = new(64);
    private readonly Dictionary<GameObject, int> _spriteLookup = new(64);
    private bool _dirty;

    public void SceneReset()
    {
        _sprites.Clear();
        _spriteLookup.Clear();
        _dirty = true;
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
        if (component is SpriteComponent)
        {
            TryRegister(go);
        }
    }

    public void ComponentRemoved(GameObject go, NiziComponent component)
    {
        if (component is SpriteComponent)
        {
            Unregister(go);
        }
    }

    public void ComponentChanged(GameObject go, NiziComponent component)
    {
        if (component is SpriteComponent)
        {
            _dirty = true;
        }
    }

    private void TryRegister(GameObject go)
    {
        if (_spriteLookup.ContainsKey(go))
        {
            return;
        }

        var sprite = go.GetComponent<SpriteComponent>();
        if (sprite == null)
        {
            return;
        }

        _spriteLookup[go] = _sprites.Count;
        _sprites.Add(new Renderable2D
        {
            Owner = go,
            Sprite = sprite
        });
        _dirty = true;
    }

    private void Unregister(GameObject go)
    {
        if (!_spriteLookup.Remove(go, out var index))
        {
            return;
        }

        var lastIndex = _sprites.Count - 1;
        if (index < lastIndex)
        {
            var swapped = _sprites[lastIndex];
            _sprites[index] = swapped;
            _spriteLookup[swapped.Owner] = index;
        }

        _sprites.RemoveAt(lastIndex);
        _dirty = true;
    }

    public ReadOnlySpan<Renderable2D> GetSortedSprites()
    {
        if (_dirty)
        {
            _sprites.Sort((a, b) =>
            {
                var layerCmp = a.Sprite.SortingLayer.CompareTo(b.Sprite.SortingLayer);
                return layerCmp != 0 ? layerCmp : a.Sprite.SortOrder.CompareTo(b.Sprite.SortOrder);
            });

            // Rebuild lookup after sort
            for (var i = 0; i < _sprites.Count; i++)
            {
                _spriteLookup[_sprites[i].Owner] = i;
            }

            _dirty = false;
        }

        return CollectionsMarshal.AsSpan(_sprites);
    }
}
