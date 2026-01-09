namespace DenOfIz.World.Assets;

public class Skeleton : IDisposable
{
    public string Name { get; set; } = string.Empty;
    public int JointCount { get; set; }
    public List<Joint> Joints { get; set; } = [];
    public int[] RootJointIndices { get; set; } = [];
    public OzzAnimation OzzSkeleton { get; set; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        OzzSkeleton.Dispose();
    }
}
