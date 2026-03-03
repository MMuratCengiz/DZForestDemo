using System.Numerics;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Binding;

public class ViewData
{
    public Scene Scene { get; set; } = null!;
    public CameraComponent? Camera { get; set; }
    public float DeltaTime { get; set; }
    public float TotalTime { get; set; }
    public Matrix4x4? ViewProjectionOverride { get; set; }
}
