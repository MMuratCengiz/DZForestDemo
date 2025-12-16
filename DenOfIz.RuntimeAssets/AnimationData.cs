namespace RuntimeAssets;

public sealed class AnimationData
{
    public required string Name { get; init; }
    public required byte[] Data { get; init; }
    public required string SourcePath { get; init; }
}