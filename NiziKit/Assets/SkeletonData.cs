namespace NiziKit.Assets;

public sealed class SkeletonData
{
    public required string Name { get; init; }
    public required byte[] Data { get; init; }
    public required string SourcePath { get; init; }
}