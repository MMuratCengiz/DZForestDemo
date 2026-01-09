using System.Numerics;

namespace NiziKit.Assets;

public class Joint
{
    public string Name { get; set; } = string.Empty;
    public int Index { get; set; }
    public int ParentIndex { get; set; } = -1;
    public Matrix4x4 InverseBindMatrix { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 LocalTransform { get; set; } = Matrix4x4.Identity;
    public List<int> ChildIndices { get; set; } = [];
}
