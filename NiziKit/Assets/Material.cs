using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public abstract class Material : IDisposable
{
    public string Name { get; protected init; } = string.Empty;
    public Texture2d? Albedo { get; set; }
    public Texture2d? Normal { get; set; }
    public Texture2d? Metallic { get; set; }
    public Texture2d? Roughness { get; set; }
    public GpuShader? GpuShader { get; set; }
    public IReadOnlyDictionary<string, string?>? Variants { get; set; }

    protected static GraphicsContext Context => GraphicsContext.Instance;

    public virtual void Dispose()
    {
    }
}