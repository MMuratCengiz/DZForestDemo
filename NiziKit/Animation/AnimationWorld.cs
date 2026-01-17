using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Animation;

public sealed class AnimationWorld : IWorldEventListener
{
    private readonly List<Animator> _animators = [];

    public void SceneReset()
    {
        foreach (var animator in _animators)
        {
            animator.Dispose();
        }
        _animators.Clear();
    }

    public void GameObjectCreated(GameObject go)
    {
        var animator = go.GetComponent<Animator>();
        if (animator != null && !_animators.Contains(animator))
        {
            _animators.Add(animator);
        }
    }

    public void GameObjectDestroyed(GameObject go)
    {
        var animator = go.GetComponent<Animator>();
        if (animator != null)
        {
            _animators.Remove(animator);
            animator.Dispose();
        }
    }

    public void ComponentAdded(GameObject go, IComponent component)
    {
        if (component is Animator animator && !_animators.Contains(animator))
        {
            _animators.Add(animator);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
        if (component is Animator animator)
        {
            _animators.Remove(animator);
            animator.Dispose();
        }
    }

    public void ComponentChanged(GameObject go, IComponent component)
    {
    }

    public void Update(float deltaTime)
    {
        foreach (var animator in _animators)
        {
            animator.Update(deltaTime);
        }
    }
}
