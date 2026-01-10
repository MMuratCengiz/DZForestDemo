using DenOfIz;
using NiziKit.Graphics;

namespace NiziKit.Assets;

public abstract class Material : IDisposable
{
    public string Name { get; protected init; } = string.Empty;
    public GpuShader? GpuShader { get; private set; }

    protected GraphicsContext? Context { get; private set; }

    protected abstract ShaderProgram LoadShaderProgram();

    protected abstract GraphicsPipelineDesc ConfigurePipeline(GraphicsContext context);

    public void Initialize(GraphicsContext context)
    {
        Context = context;
        var program = LoadShaderProgram();
        var pipelineDesc = ConfigurePipeline(context);
        GpuShader = new GpuShader(context, program, pipelineDesc);
    }

    public virtual void Dispose()
    {
        GpuShader?.Dispose();
    }
}
