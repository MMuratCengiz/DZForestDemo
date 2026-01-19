using DenOfIz;

namespace NiziKit.Graphics;

public class GpuShader : IDisposable
{
    public Pipeline Pipeline { get; }
    public ShaderProgram ShaderProgram { get; private set; }
    public RootSignature RootSignature { get; private set; }
    public InputLayout InputLayout { get; private set; }

    private readonly bool _ownsProgram;

    private GpuShader(ShaderProgram program, GraphicsPipelineDesc? graphicsDesc,
        RayTracingPipelineDesc? rayTracingPipelineDesc, BindPoint? explicitBindPoint = null, bool ownsProgram = true)
    {
        ShaderProgram = program;
        _ownsProgram = ownsProgram;
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

        var bindPoint = explicitBindPoint ?? DetermineBindPoint(graphicsDesc, rayTracingPipelineDesc);

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

    private static BindPoint DetermineBindPoint(GraphicsPipelineDesc? graphicsDesc, RayTracingPipelineDesc? rayTracingPipelineDesc)
    {
        if (rayTracingPipelineDesc != null)
            return BindPoint.Raytracing;
        if (graphicsDesc != null)
            return BindPoint.Graphics;
        return BindPoint.Compute;
    }

    public static GpuShader Compute(ShaderProgram program, bool ownsProgram = true)
    {
        return new GpuShader(program, null, null, BindPoint.Compute, ownsProgram);
    }

    public static GpuShader Graphics(ShaderProgram program, GraphicsPipelineDesc graphicsDesc, bool ownsProgram = true)
    {
        return new GpuShader(program, graphicsDesc, null, BindPoint.Graphics, ownsProgram);
    }

    public static GpuShader RayTracing(ShaderProgram program, RayTracingPipelineDesc rayTracingPipelineDesc, bool ownsProgram = true)
    {
        return new GpuShader(program, null, rayTracingPipelineDesc, BindPoint.Raytracing, ownsProgram);
    }

    public static GpuShader Mesh(ShaderProgram program, GraphicsPipelineDesc graphicsDesc, bool ownsProgram = true)
    {
        return new GpuShader(program, graphicsDesc, null, BindPoint.Mesh, ownsProgram);
    }

    public void Dispose()
    {
        Pipeline.Dispose();
        RootSignature.Dispose();
        InputLayout.Dispose();
        if (_ownsProgram)
        {
            ShaderProgram.Dispose();
        }
    }
}
