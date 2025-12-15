using System.Runtime.CompilerServices;

namespace ECS;

public interface ICommand
{
    void Execute(EntityStore store);
}

public struct SpawnCommand : ICommand
{
    private readonly Action<EntityStore, Entity>? _configure;

    public SpawnCommand(Action<EntityStore, Entity>? configure = null)
    {
        _configure = configure;
    }

    public void Execute(EntityStore store)
    {
        var entity = store.Spawn();
        _configure?.Invoke(store, entity);
    }
}

public struct DespawnCommand : ICommand
{
    public Entity Entity;

    public DespawnCommand(Entity entity)
    {
        Entity = entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(EntityStore store)
    {
        store.Despawn(Entity);
    }
}

public struct AddComponentCommand<T> : ICommand where T : struct
{
    public Entity Entity;
    public T Component;

    public AddComponentCommand(Entity entity, T component)
    {
        Entity = entity;
        Component = component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(EntityStore store)
    {
        store.AddComponent(Entity, in Component);
    }
}

public struct RemoveComponentCommand<T> : ICommand where T : struct
{
    public Entity Entity;

    public RemoveComponentCommand(Entity entity)
    {
        Entity = entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(EntityStore store)
    {
        store.RemoveComponent<T>(Entity);
    }
}

public sealed class Commands
{
    private readonly List<ICommand> _commands;
    private readonly EntityStore _store;

    public Commands(EntityStore store)
    {
        _store = store;
        _commands = new List<ICommand>(64);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Spawn(Action<EntityStore, Entity>? configure = null)
    {
        _commands.Add(new SpawnCommand(configure));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Despawn(Entity entity)
    {
        _commands.Add(new DespawnCommand(entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddComponent<T>(Entity entity, T component) where T : struct
    {
        _commands.Add(new AddComponentCommand<T>(entity, component));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        _commands.Add(new RemoveComponentCommand<T>(entity));
    }

    public void Apply()
    {
        foreach (var command in _commands)
        {
            command.Execute(_store);
        }
        _commands.Clear();
    }

    public void Clear()
    {
        _commands.Clear();
    }

    public int PendingCount => _commands.Count;
}

public sealed class EntityBuilder
{
    private readonly EntityStore _store;
    private Entity _entity;

    internal EntityBuilder(EntityStore store)
    {
        _store = store;
        _entity = Entity.Invalid;
    }

    public EntityBuilder Spawn()
    {
        _entity = _store.Spawn();
        return this;
    }

    internal EntityBuilder SpawnWithScene(SceneId sceneId)
    {
        _entity = _store.Spawn();
        _store.AddComponent(_entity, new SceneComponent(sceneId));
        return this;
    }

    public EntityBuilder With<T>(T component) where T : struct
    {
        if (_entity.IsValid)
        {
            _store.AddComponent(_entity, in component);
        }
        return this;
    }

    public Entity Build()
    {
        return _entity;
    }
}

public static class EntityBuilderExtensions
{
    public static EntityBuilder SpawnBuilder(this EntityStore store)
    {
        return new EntityBuilder(store).Spawn();
    }
}
