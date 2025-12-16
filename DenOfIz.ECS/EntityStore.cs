using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public struct EntityLocation(ArchetypeId archetypeId, int row, uint generation)
{
    public ArchetypeId ArchetypeId = archetypeId;
    public int Row = row;
    public uint Generation = generation;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ArchetypeId.IsValid && Generation != 0;
    }
}

public sealed class EntityStore
{
    private readonly List<Archetype> _archetypes;
    private readonly Archetype _emptyArchetype;
    private readonly List<EntityLocation> _entityLocations;
    private readonly Queue<uint> _freeIndices;
    private readonly Dictionary<ArchetypeSignature, ArchetypeId> _signatureToArchetype;

    public EntityStore()
    {
        _entityLocations = new List<EntityLocation>();
        _freeIndices = new Queue<uint>();
        _archetypes = new List<Archetype>();
        _signatureToArchetype = new Dictionary<ArchetypeSignature, ArchetypeId>();

        var emptySignature = new ArchetypeSignature(Array.Empty<ComponentId>());
        _emptyArchetype = CreateArchetype(emptySignature);
    }

    public int EntityCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _entityLocations.Count - _freeIndices.Count;
    }

    public int ArchetypeCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _archetypes.Count;
    }

    public ReadOnlySpan<Archetype> Archetypes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => CollectionsMarshal.AsSpan(_archetypes);
    }

    public Entity Spawn()
    {
        Entity entity;
        int row;

        if (_freeIndices.TryDequeue(out var freeIndex))
        {
            var locations = CollectionsMarshal.AsSpan(_entityLocations);
            ref var location = ref locations[(int)freeIndex];
            var newGeneration = location.Generation + 1;
            entity = new Entity(freeIndex, newGeneration);
            row = _emptyArchetype.AddEntity(entity);
            location = new EntityLocation(_emptyArchetype.Id, row, newGeneration);
        }
        else
        {
            var index = (uint)_entityLocations.Count;
            const uint initialGeneration = 1;
            entity = new Entity(index, initialGeneration);
            row = _emptyArchetype.AddEntity(entity);
            _entityLocations.Add(new EntityLocation(_emptyArchetype.Id, row, initialGeneration));
        }

        return entity;
    }

    public void Despawn(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return;
        }

        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        ref var location = ref locations[(int)entity.Index];
        var archetype = _archetypes[location.ArchetypeId.Id];

        if (archetype.EntityCount > 1 && location.Row < archetype.EntityCount - 1)
        {
            var movedEntity = archetype.GetEntity(archetype.EntityCount - 1);
            ref var movedLocation = ref locations[(int)movedEntity.Index];
            movedLocation.Row = location.Row;
        }

        archetype.SwapRemoveEntity(location.Row);
        location = new EntityLocation(ArchetypeId.Invalid, -1, location.Generation);
        _freeIndices.Enqueue(entity.Index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAlive(Entity entity)
    {
        if (!entity.IsValid || entity.Index >= (uint)_entityLocations.Count)
        {
            return false;
        }

        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        ref readonly var location = ref locations[(int)entity.Index];
        return location.Generation == entity.Generation && location.ArchetypeId.IsValid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EntityLocation GetLocation(Entity entity)
    {
        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        return ref locations[(int)entity.Index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Archetype GetArchetype(ArchetypeId id)
    {
        return _archetypes[id.Id];
    }

    public void AddComponent<T>(Entity entity, in T component) where T : struct
    {
        if (!IsAlive(entity))
        {
            return;
        }

        var componentId = ComponentRegistry.GetId<T>();
        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        ref var location = ref locations[(int)entity.Index];
        var currentArchetype = _archetypes[location.ArchetypeId.Id];

        if (currentArchetype.HasComponent(componentId))
        {
            var column = currentArchetype.GetColumn<T>();
            column.Set(location.Row, in component);
            return;
        }

        var newSignature = currentArchetype.Signature.With(componentId);
        var newArchetype = GetOrCreateArchetype(newSignature);

        MoveEntity(entity, ref location, currentArchetype, newArchetype);

        var newColumn = newArchetype.GetColumn<T>();
        newColumn.Add(in component);
    }

    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        if (!IsAlive(entity))
        {
            return;
        }

        var componentId = ComponentRegistry.GetId<T>();
        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        ref var location = ref locations[(int)entity.Index];
        var currentArchetype = _archetypes[location.ArchetypeId.Id];

        if (!currentArchetype.HasComponent(componentId))
        {
            return;
        }

        var newSignature = currentArchetype.Signature.Without(componentId);
        var newArchetype = GetOrCreateArchetype(newSignature);

        MoveEntity(entity, ref location, currentArchetype, newArchetype);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>(Entity entity) where T : struct
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        ref readonly var location = ref locations[(int)entity.Index];
        var archetype = _archetypes[location.ArchetypeId.Id];
        return archetype.HasComponent<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent<T>(Entity entity) where T : struct
    {
        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        ref readonly var location = ref locations[(int)entity.Index];
        var archetype = _archetypes[location.ArchetypeId.Id];
        var column = archetype.GetColumn<T>();
        return ref column.Get(location.Row);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetComponent<T>(Entity entity, out T component) where T : struct
    {
        if (!IsAlive(entity))
        {
            component = default;
            return false;
        }

        var locations = CollectionsMarshal.AsSpan(_entityLocations);
        ref readonly var location = ref locations[(int)entity.Index];
        var archetype = _archetypes[location.ArchetypeId.Id];

        if (!archetype.TryGetColumn<T>(out var column))
        {
            component = default;
            return false;
        }

        component = column!.Get(location.Row);
        return true;
    }

    private void MoveEntity(Entity entity, ref EntityLocation location, Archetype from, Archetype to)
    {
        var oldRow = location.Row;

        from.CopyEntityTo(oldRow, to);
        var newRow = to.AddEntity(entity);

        if (from.EntityCount > 1 && oldRow < from.EntityCount - 1)
        {
            var movedEntity = from.GetEntity(from.EntityCount - 1);
            var locations = CollectionsMarshal.AsSpan(_entityLocations);
            ref var movedLocation = ref locations[(int)movedEntity.Index];
            movedLocation.Row = oldRow;
        }

        from.SwapRemoveEntity(oldRow);

        location.ArchetypeId = to.Id;
        location.Row = newRow;
    }

    private Archetype GetOrCreateArchetype(ArchetypeSignature signature)
    {
        if (_signatureToArchetype.TryGetValue(signature, out var existingId))
        {
            return _archetypes[existingId.Id];
        }

        return CreateArchetype(signature);
    }

    private Archetype CreateArchetype(ArchetypeSignature signature)
    {
        var id = new ArchetypeId(_archetypes.Count);
        var archetype = new Archetype(id, signature);

        foreach (var componentId in signature.ComponentIds)
        {
            var info = ComponentRegistry.GetInfo(componentId);
            var columnType = typeof(ComponentColumn<>).MakeGenericType(info.Type);
            var column = (IComponentColumn)Activator.CreateInstance(columnType)!;
            archetype.RegisterColumn(componentId, column);
        }

        _archetypes.Add(archetype);
        _signatureToArchetype[signature] = id;
        return archetype;
    }

    public void Clear()
    {
        foreach (var archetype in _archetypes)
        {
            archetype.Clear();
        }

        _entityLocations.Clear();
        _freeIndices.Clear();
    }
}