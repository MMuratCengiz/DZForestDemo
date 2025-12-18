using ECS;

namespace DZForestDemo.Scenes;

public enum DemoState : byte
{
    Loading,
    FoxScene,
    VikingScene
}

public readonly struct DemoGameState : IGameState, IEquatable<DemoGameState>
{
    public DemoState State { get; init; }

    public static DemoGameState Loading => new() { State = DemoState.Loading };
    public static DemoGameState Fox => new() { State = DemoState.FoxScene };
    public static DemoGameState Viking => new() { State = DemoState.VikingScene };

    public bool Equals(DemoGameState other) => State == other.State;
    public override bool Equals(object? obj) => obj is DemoGameState other && Equals(other);
    public override int GetHashCode() => (int)State;
    public static bool operator ==(DemoGameState left, DemoGameState right) => left.Equals(right);
    public static bool operator !=(DemoGameState left, DemoGameState right) => !left.Equals(right);
    public override string ToString() => State.ToString();
}
