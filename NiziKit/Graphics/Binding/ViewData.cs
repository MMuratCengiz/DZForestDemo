using NiziKit.Core;

namespace NiziKit.Graphics.Binding;

public class ViewData
{
    public Scene Scene { get; set; } = null!;
    public ICameraProvider? Camera { get; set; }
    public float DeltaTime { get; set; }
    public float TotalTime { get; set; }
}
