using System.Runtime.InteropServices;
using NiziKit.Assets;

namespace NiziKit.Graphics;

public class DrawBatch
{
    public Mesh Mesh { get; }
    public List<RenderObject> Objects { get; } = new(32);

    public int Count => Objects.Count;

    public DrawBatch(Mesh mesh)
    {
        Mesh = mesh;
    }

    public void Add(RenderObject obj)
    {
        Objects.Add(obj);
    }

    public void RemoveAt(int index)
    {
        var lastIndex = Objects.Count - 1;
        if (index < lastIndex)
        {
            Objects[index] = Objects[lastIndex];
        }
        Objects.RemoveAt(lastIndex);
    }

    public ReadOnlySpan<RenderObject> AsSpan()
    {
        return CollectionsMarshal.AsSpan(Objects);
    }
}
