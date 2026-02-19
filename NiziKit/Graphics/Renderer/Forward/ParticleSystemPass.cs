using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;
using NiziKit.Particles;

namespace NiziKit.Graphics.Renderer.Forward;

public class ParticleSystemPass : IDisposable
{
    public const int MaxParticlesPerFrame = 1048576;

    private readonly GpuShader _computeShader;
    private readonly GpuShader _rasterShader;

    private readonly BindGroupLayout _computeBindGroupLayout;
    private readonly BindGroup[] _computeBindGroups;
    private readonly BindGroupLayout _rasterBindGroupLayout;
    private readonly BindGroup[] _rasterBindGroups;

    private readonly StagingBuffer _stagingParticles;
    private readonly StructuredBuffer<Particle> _particles;
    private readonly StructuredBuffer<ResolvedParticle> _resolvedParticles;
    private readonly StructuredBuffer<DrawIndexedIndirectCommand> _drawCommands;

    public ParticleSystemPass()
    {
        _stagingParticles = new StagingBuffer((uint)(MaxParticlesPerFrame * Marshal.SizeOf<Particle>()));
        _particles = new StructuredBuffer<Particle>(MaxParticlesPerFrame);
        _drawCommands = new StructuredBuffer<DrawIndexedIndirectCommand>(MaxParticlesPerFrame);
        _resolvedParticles = new StructuredBuffer<ResolvedParticle>(MaxParticlesPerFrame);

        var numFrames = (int)GraphicsContext.NumFrames;

        var computeProgram = BuiltinShaderProgram.Load("ParticleSystemComputeShader")
                             ?? throw new InvalidOperationException("ParticleSystemComputeShader not found");
        _computeShader = GpuShader.Compute(computeProgram);


        var blendDesc = new BlendDesc
        {
            Enable = false,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = GraphicsContext.BackBufferFormat,
            Blend = blendDesc
        };

        var rasterProgram = BuiltinShaderProgram.Load("ParticleSystemRasterShader")
                            ?? throw new InvalidOperationException("ParticleSystemRasterShader not found");
        var reflection = rasterProgram.Reflect();
        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);
        _rasterShader = GpuShader.Graphics(rasterProgram, new GraphicsPipelineDesc
        {
            PrimitiveTopology = PrimitiveTopology.Triangle,
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
            DepthTest = new DepthTest
            {
                Enable = false,
                CompareOp = CompareOp.Always,
                Write = false
            },
            RenderTargets = renderTargets,
            DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat
        });

        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _rasterBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[0]);
        _rasterBindGroups = new BindGroup[numFrames];

        var computeReflection = computeProgram.Reflect();
        var computeBindGroupLayoutDescs = computeReflection.BindGroupLayouts.ToArray();
        _computeBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(computeBindGroupLayoutDescs[0]);
        _computeBindGroups = new BindGroup[numFrames];


        for (var i = 0; i < numFrames; i++)
        {
            _computeBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _computeBindGroupLayout
            });

            _computeBindGroups[i].BeginUpdate();
            _computeBindGroups[i].SrvBuffer(0, _particles.Buffer);
            _computeBindGroups[i].SrvBuffer(1, _resolvedParticles.Buffer);
            _computeBindGroups[i].SrvBuffer(2, _drawCommands.Buffer);
            _computeBindGroups[i].EndUpdate();

            _rasterBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _rasterBindGroupLayout
            });
        }
    }

    // TODO read particles somehow
    public void Execute(RenderFrame frame)
    {
        var compute = frame.BeginComputePass();
        _stagingParticles.CopyTo(compute, _particles);
        compute.Bind(_computeBindGroups[GraphicsContext.FrameIndex]);
        compute.Dispatch(64, 1, 1);

        var graphics = frame.BeginGraphicsPass(compute);
        graphics.BindShader(_rasterShader);
        graphics.Bind(_rasterBindGroups[GraphicsContext.FrameIndex]);
        graphics.DrawIndexedIndirect(_drawCommands.Buffer, 0, 0, 0);
        graphics.End();
    }

    public void Dispose()
    {
        _computeShader.Dispose();
        _rasterShader.Dispose();
        foreach (var bindGroup in _computeBindGroups)
        {
            bindGroup.Dispose();
        }

        _computeBindGroupLayout.Dispose();
    }
}
