using Flecs.NET.Core;

namespace ECS;

/// <summary>
/// Factory for creating the state transition system.
/// </summary>
public static class StateTransitionSystem<T> where T : struct, IGameState
{
    /// <summary>
    /// Registers a system that handles game state transitions.
    /// Checks for pending state changes and invokes callbacks.
    /// </summary>
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

/// <summary>
/// Factory for creating the asset load tracker system.
/// </summary>
public static class AssetLoadTrackerSystem
{
    /// <summary>
    /// Registers a system that updates async asset loading.
    /// </summary>
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
