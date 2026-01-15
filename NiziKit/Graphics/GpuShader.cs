using DenOfIz;
using NiziKit.Graphics.RootSignatures;

namespace NiziKit.Graphics;

public class GpuShader : IDisposable
{
    public Pipeline Pipeline { get; }
    public ShaderProgram ShaderProgram { get; private set; }
    public RootSignature RootSignature { get; private set; }
    public InputLayout InputLayout { get; private set; }

    private GpuShader(ShaderProgram program, GraphicsPipelineDesc? graphicsDesc,
        RayTracingPipelineDesc? rayTracingPipelineDesc)
    {
        ShaderProgram = program;
        var reflection = program.Reflect();

        var store = GraphicsContext.BindGroupLayoutStore;
        var bindGroupLayouts = new[]
        {
            store.Camera,
            store.Material,
            store.Draw
        };

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create(bindGroupLayouts),
            RootConstants = reflection.RootConstants
        };
        RootSignature = GraphicsContext.Device.CreateRootSignature(rootSigDesc);
        InputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);

        var bindPoint = BindPoint.Compute;
        if (rayTracingPipelineDesc != null)
        {
            bindPoint = BindPoint.Raytracing;
        }
        if (graphicsDesc != null)
        {
            bindPoint = BindPoint.Graphics;
        }
        var pipelineDesc = new PipelineDesc
        {
            BindPoint = bindPoint,
            ShaderProgram = program,
            RootSignature = RootSignature,
            InputLayout = InputLayout,
            Graphics = graphicsDesc ?? new GraphicsPipelineDesc(),
            RayTracing = rayTracingPipelineDesc ?? new RayTracingPipelineDesc(),
        };

        Pipeline = GraphicsContext.Device.CreatePipeline(pipelineDesc);
    }

    public static GpuShader Compute(ShaderProgram program)
    {
        return new GpuShader(program, null, null);
    }

    public static GpuShader Graphics(ShaderProgram program, GraphicsPipelineDesc graphicsDesc)
    {
        return new GpuShader(program, graphicsDesc, null);
    }

    public static GpuShader RayTracing(ShaderProgram program, RayTracingPipelineDesc rayTracingPipelineDesc)
    {
        return new GpuShader(program, null, rayTracingPipelineDesc);
    }

    public void Dispose()
    {
        Pipeline.Dispose();
        RootSignature.Dispose();
        InputLayout.Dispose();
        ShaderProgram.Dispose();
    }
}
