using Microsoft.Extensions.Logging;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Animation;

public sealed class AnimationWorld : IWorldEventListener
{
    private readonly HashSet<Animator> _animatorsSet = new(64);
    private readonly List<Animator> _animatorsList = new(64);
    private bool _dirty;

    public void SceneReset()
    {
        foreach (var animator in _animatorsList)
        {
            animator.Dispose();
        }
        _animatorsSet.Clear();
        _animatorsList.Clear();
        _dirty = false;
    }

    public void GameObjectCreated(GameObject go)
    {
        var animator = go.GetComponent<Animator>();
        if (animator != null && _animatorsSet.Add(animator))
        {
            _dirty = true;
        }
    }

    public void GameObjectDestroyed(GameObject go)
    {
        var animator = go.GetComponent<Animator>();
        if (animator != null && _animatorsSet.Remove(animator))
        {
            _dirty = true;
            animator.Dispose();
        }
    }

    public void ComponentAdded(GameObject go, IComponent component)
    {
        if (component is Animator animator && _animatorsSet.Add(animator))
        {
            _dirty = true;
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        if (component is Animator animator && _animatorsSet.Remove(animator))
        {
            _dirty = true;
            animator.Dispose();
        }
    }

    public void ComponentChanged(GameObject go, IComponent component)
    {
    }

    public void Update(float deltaTime)
    {
        if (_dirty)
        {
            _animatorsList.Clear();
            _animatorsList.AddRange(_animatorsSet);
            _dirty = false;
        }

        for (var i = 0; i < _animatorsList.Count; i++)
        {
            _animatorsList[i].Update(deltaTime);
        }
    }
}
