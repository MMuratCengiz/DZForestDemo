using DenOfIz;

namespace NiziKit.Graphics;

public class GpuShader : IDisposable
{
    public Pipeline Pipeline { get; private set; }
    public ShaderProgram ShaderProgram { get; private set; }
    public RootSignature RootSignature { get; private set; }
    public InputLayout InputLayout { get; private set; }

    private readonly BindGroupLayout[] _bindGroupLayouts;
    private bool _disposed;

    public GpuShader(GraphicsContext context, ShaderProgram program, GraphicsPipelineDesc graphicsDesc)
    {
        ShaderProgram = program;
        var reflection = program.Reflect();

        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _bindGroupLayouts = new BindGroupLayout[bindGroupLayoutDescs.Length];
        for (var i = 0; i < bindGroupLayoutDescs.Length; i++)
        {
            _bindGroupLayouts[i] = context.LogicalDevice.CreateBindGroupLayout(bindGroupLayoutDescs[i]);
        }

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create(_bindGroupLayouts),
            RootConstants = reflection.RootConstants
        };
        RootSignature = context.LogicalDevice.CreateRootSignature(rootSigDesc);
        InputLayout = context.LogicalDevice.CreateInputLayout(reflection.InputLayout);

        var pipelineDesc = new PipelineDesc
        {
            BindPoint = BindPoint.Graphics,
            ShaderProgram = program,
            RootSignature = RootSignature,
            InputLayout = InputLayout,
            Graphics = graphicsDesc
        };

        Pipeline = context.LogicalDevice.CreatePipeline(pipelineDesc);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Pipeline.Dispose();
        RootSignature.Dispose();
        InputLayout.Dispose();
        foreach (var layout in _bindGroupLayouts)
        {
            layout.Dispose();
        }
        ShaderProgram.Dispose();
    }
}
