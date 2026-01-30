using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Forward;

public class SkyboxPass : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SkyboxConstants
    {
        public Matrix4x4 InverseViewProjection;
    }

    private readonly ShaderProgram _program;
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly Pipeline _pipeline;
    private readonly BindGroupLayout _bindGroupLayout;
    private readonly Sampler _sampler;
    private readonly BindGroup[] _bindGroups;
    private readonly MappedBuffer<SkyboxConstants> _constantBuffer;

    public SkyboxPass()
    {
        _program = BuiltinShaderProgram.Load("SkyboxShader")
                   ?? throw new InvalidOperationException("SkyboxShader not found");

        var reflection = _program.Reflect();
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _bindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[0]);

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create([_bindGroupLayout]),
        };
        _rootSignature = GraphicsContext.Device.CreateRootSignature(rootSigDesc);
        _inputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);

        _sampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Linear
        });

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

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var pipelineDesc = new PipelineDesc
        {
            RootSignature = _rootSignature,
            InputLayout = _inputLayout,
            ShaderProgram = _program,
            BindPoint = BindPoint.Graphics,
            Graphics = new GraphicsPipelineDesc
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
            }
        };

        _pipeline = GraphicsContext.Device.CreatePipeline(pipelineDesc);

        var numFrames = (int)GraphicsContext.NumFrames;
        _bindGroups = new BindGroup[numFrames];
        _constantBuffer = new MappedBuffer<SkyboxConstants>(true, "SkyboxConstants");

        for (var i = 0; i < numFrames; i++)
        {
            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _bindGroupLayout
            });
        }
    }

    public void Execute(Pass.GraphicsPass pass, Matrix4x4 inverseViewProjection, SkyboxData skybox)
    {
        var frameIndex = GraphicsContext.FrameIndex;

        _constantBuffer.Write(new SkyboxConstants
        {
            InverseViewProjection = inverseViewProjection
        });

        var bg = _bindGroups[frameIndex];
        bg.BeginUpdate();
        bg.CbvWithDesc(new BindBufferDesc
        {
            Binding = 0,
            Resource = _constantBuffer[frameIndex]
        });
        bg.SrvTexture(0, skybox.Right?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(1, skybox.Left?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(2, skybox.Up?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(3, skybox.Down?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(4, skybox.Front?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(5, skybox.Back?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.Sampler(0, _sampler);
        bg.EndUpdate();

        pass.BindPipeline(_pipeline);
        pass.Bind(bg);
        pass.Draw(3);
    }

    public void Dispose()
    {
        _constantBuffer.Dispose();

        foreach (var bindGroup in _bindGroups)
        {
            bindGroup.Dispose();
        }

        _pipeline.Dispose();
        _sampler.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        _bindGroupLayout.Dispose();
        _program.Dispose();
    }
}
