using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS;

public sealed class Archetype
{
    private readonly Dictionary<ComponentId, IComponentColumn> _columns;
    private readonly List<Entity> _entities;
    private readonly Dictionary<uint, int> _entityIndexMap;

    public ArchetypeId Id { get; }
    public ArchetypeSignature Signature { get; }

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

    internal Archetype(ArchetypeId id, ArchetypeSignature signature)
    {
        Id = id;
        Signature = signature;
        _columns = new Dictionary<ComponentId, IComponentColumn>();
        _entities = new List<Entity>();
        _entityIndexMap = new Dictionary<uint, int>();
    }

    public void RegisterColumn<T>() where T : struct
    {
        var componentId = ComponentRegistry.GetId<T>();
        if (!_columns.ContainsKey(componentId))
        {
            _columns[componentId] = new ComponentColumn<T>();
        }
    }

    internal void RegisterColumn(ComponentId componentId, IComponentColumn column)
    {
        if (!_columns.ContainsKey(componentId))
        {
            _columns[componentId] = column.Clone();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>() where T : struct
    {
        return Signature.Contains(ComponentRegistry.GetId<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(ComponentId componentId)
    {
        return Signature.Contains(componentId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentColumn<T> GetColumn<T>() where T : struct
    {
        var componentId = ComponentRegistry.GetId<T>();
        return (ComponentColumn<T>)_columns[componentId];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetColumn<T>(out ComponentColumn<T>? column) where T : struct
    {
        var componentId = ComponentRegistry.GetId<T>();
        if (_columns.TryGetValue(componentId, out var col))
        {
            column = (ComponentColumn<T>)col;
            return true;
        }
        column = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IComponentColumn GetColumn(ComponentId componentId)
    {
        return _columns[componentId];
    }

    internal int AddEntity(Entity entity)
    {
        var index = _entities.Count;
        _entities.Add(entity);
        _entityIndexMap[entity.Index] = index;
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetEntityIndex(Entity entity)
    {
        return _entityIndexMap.TryGetValue(entity.Index, out var index) ? index : -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetEntity(int index)
    {
        return _entities[index];
    }

    internal Entity SwapRemoveEntity(int index)
    {
        var removedEntity = _entities[index];
        var lastIndex = _entities.Count - 1;

        if (index < lastIndex)
        {
            var movedEntity = _entities[lastIndex];
            _entities[index] = movedEntity;
            _entityIndexMap[movedEntity.Index] = index;

            foreach (var column in _columns.Values)
            {
                column.SwapRemove(index);
            }
        }
        else
        {
            foreach (var column in _columns.Values)
            {
                column.SwapRemove(index);
            }
        }

        _entities.RemoveAt(lastIndex);
        _entityIndexMap.Remove(removedEntity.Index);

        return removedEntity;
    }

    internal void CopyEntityTo(int sourceIndex, Archetype target)
    {
        foreach (var componentId in Signature.ComponentIds)
        {
            if (target.Signature.Contains(componentId))
            {
                var sourceColumn = _columns[componentId];
                var targetColumn = target._columns[componentId];
                targetColumn.CopyFrom(sourceColumn, sourceIndex);
            }
        }
    }

    internal void Clear()
    {
        _entities.Clear();
        _entityIndexMap.Clear();
        foreach (var column in _columns.Values)
        {
            column.Clear();
        }
    }
}
