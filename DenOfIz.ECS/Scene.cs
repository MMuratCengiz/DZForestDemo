using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS;

public readonly struct SceneId : IEquatable<SceneId>
{
    public readonly uint Id;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SceneId(uint id)
    {
        Id = id;
    }

    public static SceneId Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Id != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(SceneId other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SceneId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(SceneId left, SceneId right)
    {
        return left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(SceneId left, SceneId right)
    {
        return !left.Equals(right);
    }
}

public struct SceneComponent(SceneId sceneId)
{
    public SceneId SceneId = sceneId;
}

public sealed class Scene
{
    private readonly List<Entity> _entities;
    private readonly HashSet<uint> _entityIndices;
    private readonly EntityStore _store;

    internal Scene(SceneId id, string name, EntityStore store)
    {
        Id = id;
        Name = name;
        _store = store;
        _entities = new List<Entity>();
        _entityIndices = new HashSet<uint>();
        IsLoaded = false;
    }

    public SceneId Id { get; }
    public string Name { get; }
    public bool IsLoaded { get; private set; }

    public int EntityCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entities.Count;
    }

    public ReadOnlySpan<Entity> Entities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => CollectionsMarshal.AsSpan(_entities);
    }

    public Entity Spawn()
    {
        var entity = _store.Spawn();
        _store.AddComponent(entity, new SceneComponent(Id));
        _entities.Add(entity);
        _entityIndices.Add(entity.Index);
        return entity;
    }

    public EntityBuilder SpawnBuilder()
    {
        return new EntityBuilder(_store).SpawnWithScene(Id);
    }

    public void AddEntity(Entity entity)
    {
        if (!_store.IsAlive(entity) || _entityIndices.Contains(entity.Index))
        {
            return;
        }

        _store.AddComponent(entity, new SceneComponent(Id));
        _entities.Add(entity);
        _entityIndices.Add(entity.Index);
    }

    public void RemoveEntity(Entity entity)
    {
        if (!_entityIndices.Contains(entity.Index))
        {
            return;
        }

        _store.RemoveComponent<SceneComponent>(entity);
        _entityIndices.Remove(entity.Index);

        for (var i = _entities.Count - 1; i >= 0; i--)
            if (_entities[i].Index == entity.Index)
            {
                _entities.RemoveAt(i);
                break;
            }
    }

    public void Despawn(Entity entity)
    {
        if (!_entityIndices.Contains(entity.Index))
        {
            return;
        }

        _entityIndices.Remove(entity.Index);
        for (var i = _entities.Count - 1; i >= 0; i--)
            if (_entities[i].Index == entity.Index)
            {
                _entities.RemoveAt(i);
                break;
            }

        _store.Despawn(entity);
    }

    public void Load()
    {
        IsLoaded = true;
    }

    public void Unload()
    {
        IsLoaded = false;
        var entities = CollectionsMarshal.AsSpan(_entities);
        for (var i = entities.Length - 1; i >= 0; i--) _store.Despawn(entities[i]);
        _entities.Clear();
        _entityIndices.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Entity entity)
    {
        return _entityIndices.Contains(entity.Index);
    }

    internal void CleanupDespawnedEntities()
    {
        for (var i = _entities.Count - 1; i >= 0; i--)
        {
            var entity = _entities[i];
            if (!_store.IsAlive(entity))
            {
                _entities.RemoveAt(i);
                _entityIndices.Remove(entity.Index);
            }
        }
    }
}

public sealed class SceneManager(EntityStore store)
{
    private readonly Dictionary<string, SceneId> _nameToId = new();
    private readonly List<Scene> _scenes = new();
    private uint _nextId = 1;

    public Scene? ActiveScene
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public int SceneCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _scenes.Count;
    }

    public Scene CreateScene(string name)
    {
        if (_nameToId.ContainsKey(name))
        {
            throw new InvalidOperationException($"Scene with name '{name}' already exists.");
        }

        var id = new SceneId(_nextId++);
        var scene = new Scene(id, name, store);
        _scenes.Add(scene);
        _nameToId[name] = id;
        return scene;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scene? GetScene(SceneId id)
    {
        var index = (int)id.Id - 1;
        if (index < 0 || index >= _scenes.Count)
        {
            return null;
        }

        return _scenes[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scene? GetScene(string name)
    {
        if (_nameToId.TryGetValue(name, out var id))
        {
            return GetScene(id);
        }

        return null;
    }

    public void SetActiveScene(Scene scene)
    {
        ActiveScene = scene;
    }

    public void SetActiveScene(string name)
    {
        ActiveScene = GetScene(name);
    }

    public void LoadScene(Scene scene)
    {
        scene.Load();
    }

    public void UnloadScene(Scene scene)
    {
        if (ActiveScene == scene)
        {
            ActiveScene = null;
        }

        scene.Unload();
    }

    public void DestroyScene(Scene scene)
    {
        UnloadScene(scene);
        _nameToId.Remove(scene.Name);
        _scenes.Remove(scene);
    }

    public void CleanupDespawnedEntities()
    {
        foreach (var scene in _scenes)
        {
            scene.CleanupDespawnedEntities();
        }
    }
}