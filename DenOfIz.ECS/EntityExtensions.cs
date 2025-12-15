using System.Runtime.CompilerServices;

namespace ECS;

public static class EntityExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Get<T>(this Entity entity, EntityStore store) where T : struct
    {
        return ref store.GetComponent<T>(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<T>(this Entity entity, EntityStore store) where T : struct
    {
        return store.HasComponent<T>(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<T>(this Entity entity, EntityStore store, in T component) where T : struct
    {
        store.AddComponent(entity, in component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<T>(this Entity entity, EntityStore store) where T : struct
    {
        store.RemoveComponent<T>(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAlive(this Entity entity, EntityStore store)
    {
        return store.IsAlive(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Despawn(this Entity entity, EntityStore store)
    {
        store.Despawn(entity);
    }
}

public static class EntityStoreCreateExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity Create<T1>(this EntityStore store, T1 c1) where T1 : struct
    {
        var entity = store.Spawn();
        store.AddComponent(entity, in c1);
        return entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity Create<T1, T2>(this EntityStore store, T1 c1, T2 c2)
        where T1 : struct where T2 : struct
    {
        var entity = store.Spawn();
        store.AddComponent(entity, in c1);
        store.AddComponent(entity, in c2);
        return entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity Create<T1, T2, T3>(this EntityStore store, T1 c1, T2 c2, T3 c3)
        where T1 : struct where T2 : struct where T3 : struct
    {
        var entity = store.Spawn();
        store.AddComponent(entity, in c1);
        store.AddComponent(entity, in c2);
        store.AddComponent(entity, in c3);
        return entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity Create<T1, T2, T3, T4>(this EntityStore store, T1 c1, T2 c2, T3 c3, T4 c4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        var entity = store.Spawn();
        store.AddComponent(entity, in c1);
        store.AddComponent(entity, in c2);
        store.AddComponent(entity, in c3);
        store.AddComponent(entity, in c4);
        return entity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entity Create<T1, T2, T3, T4, T5>(this EntityStore store, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        var entity = store.Spawn();
        store.AddComponent(entity, in c1);
        store.AddComponent(entity, in c2);
        store.AddComponent(entity, in c3);
        store.AddComponent(entity, in c4);
        store.AddComponent(entity, in c5);
        return entity;
    }
}
