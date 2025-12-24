using Flecs.NET.Core;

namespace ECS;

public static class StateTransitionSystem<T> where T : struct, IGameState
{
    public static void Register(World world)
    {
        world.System($"StateTransition<{typeof(T).Name}>")
            .Kind(Ecs.PreUpdate)
            .Run((Iter _) =>
            {
                if (!world.Has<NextState<T>>())
                {
                    return;
                }

                ref var nextState = ref world.GetMut<NextState<T>>();
                if (!nextState.HasPending)
                {
                    return;
                }

                var pending = nextState.Take();
                if (!pending.HasValue)
                {
                    return;
                }

                if (!world.Has<State<T>>())
                {
                    return;
                }

                ref var state = ref world.GetMut<State<T>>();

                var oldState = state.Current;
                var newState = pending.Value;

                if (EqualityComparer<T>.Default.Equals(oldState, newState))
                {
                    return;
                }

                if (world.Has<StateCallbacks<T>>())
                {
                    ref var callbacks = ref world.GetMut<StateCallbacks<T>>();
                    callbacks.InvokeOnExit(world, oldState);
                    state.Current = newState;
                    callbacks.InvokeOnEnter(world, newState);
                }
                else
                {
                    state.Current = newState;
                }
            });
    }
}

public static class AssetLoadTrackerSystem
{
    public static void Register(World world)
    {
        world.System("AssetLoadTracker")
            .Kind(Ecs.PreUpdate)
            .Run((Iter _) =>
            {
                if (world.Has<AssetLoadTracker>())
                {
                    world.GetMut<AssetLoadTracker>().Update();
                }
            });
    }
}
