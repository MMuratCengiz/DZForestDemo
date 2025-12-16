using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS;

public sealed class Archetype
{
    private int[] _columnIndices;
    private readonly IComponentColumn[] _columns;
    private readonly List<Entity> _entities;
    private readonly Dictionary<uint, int> _entityIndexMap;

    internal Archetype(ArchetypeId id, ArchetypeSignature signature)
    {
        Id = id;
        Signature = signature;
        _entities = [];
        _entityIndexMap = new Dictionary<uint, int>();

        var maxComponentId = Math.Max(ComponentRegistry.MaxId, 16);
        _columnIndices = new int[maxComponentId];
        Array.Fill(_columnIndices, -1);

        _columns = new IComponentColumn[signature.ComponentIds.Length];
    }

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

    public ReadOnlySpan<IComponentColumn> Columns
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _columns;
    }

    public void RegisterColumn<T>() where T : struct
    {
        var componentId = Component<T>.Id;
        EnsureColumnIndicesCapacity(componentId.Id + 1);

        if (_columnIndices[componentId.Id] >= 0)
        {
            return;
        }

        var columnIndex = FindNextColumnSlot();
        _columnIndices[componentId.Id] = columnIndex;
        _columns[columnIndex] = new ComponentColumn<T>();
    }

    internal void RegisterColumn(ComponentId componentId, IComponentColumn column)
    {
        EnsureColumnIndicesCapacity(componentId.Id + 1);

        if (_columnIndices[componentId.Id] >= 0)
        {
            return;
        }

        var columnIndex = FindNextColumnSlot();
        _columnIndices[componentId.Id] = columnIndex;
        _columns[columnIndex] = column.Clone();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureColumnIndicesCapacity(int required)
    {
        if (_columnIndices.Length >= required)
        {
            return;
        }

        var newSize = Math.Max(_columnIndices.Length * 2, required);
        var newIndices = new int[newSize];
        Array.Fill(newIndices, -1);
        Array.Copy(_columnIndices, newIndices, _columnIndices.Length);
        _columnIndices = newIndices;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindNextColumnSlot()
    {
        for (var i = 0; i < _columns.Length; i++)
        {
            if (_columns[i] == null)
            {
                return i;
            }
        }

        return _columns.Length - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>() where T : struct
    {
        var id = Component<T>.Id.Id;
        return id < _columnIndices.Length && _columnIndices[id] >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(ComponentId componentId)
    {
        return componentId.Id < _columnIndices.Length && _columnIndices[componentId.Id] >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ComponentColumn<T> GetColumn<T>() where T : struct
    {
        var columnIndex = _columnIndices[Component<T>.Id.Id];
        return Unsafe.As<ComponentColumn<T>>(_columns[columnIndex]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetColumn<T>(out ComponentColumn<T>? column) where T : struct
    {
        var id = Component<T>.Id.Id;
        if (id < _columnIndices.Length)
        {
            var columnIndex = _columnIndices[id];
            if (columnIndex >= 0)
            {
                column = Unsafe.As<ComponentColumn<T>>(_columns[columnIndex]);
                return true;
            }
        }

        column = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal IComponentColumn GetColumn(ComponentId componentId)
    {
        return _columns[_columnIndices[componentId.Id]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetColumn(ComponentId componentId, out IComponentColumn? column)
    {
        if (componentId.Id < _columnIndices.Length)
        {
            var columnIndex = _columnIndices[componentId.Id];
            if (columnIndex >= 0)
            {
                column = _columns[columnIndex];
                return true;
            }
        }

        column = null;
        return false;
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
        }

        foreach (var column in _columns)
        {
            column?.SwapRemove(index);
        }

        _entities.RemoveAt(lastIndex);
        _entityIndexMap.Remove(removedEntity.Index);

        return removedEntity;
    }

    internal void CopyEntityTo(int sourceIndex, Archetype target)
    {
        foreach (var componentId in Signature.ComponentIds)
        {
            if (target.HasComponent(componentId))
            {
                var sourceColumn = GetColumn(componentId);
                var targetColumn = target.GetColumn(componentId);
                targetColumn.CopyFrom(sourceColumn, sourceIndex);
            }
        }
    }

    internal void Clear()
    {
        _entities.Clear();
        _entityIndexMap.Clear();
        foreach (var column in _columns)
        {
            column.Clear();
        }
    }
}