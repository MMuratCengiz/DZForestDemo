using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Common;

public class BlitPass : IDisposable
{
    private static readonly Format[] CommonFormats = [Format.B8G8R8A8Unorm, Format.R8G8B8A8Unorm];

    private readonly ShaderProgram _program;
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly BindGroupLayout _bindGroupLayout;
    private readonly Sampler _linearSampler;
    private readonly Dictionary<Format, Pipeline> _pipelines = new();
    private readonly Lock _pipelineLock = new();
    private readonly BindGroup[] _bindGroups;
    private readonly Texture?[] _boundTextures;
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachment;

    public BlitPass()
    {
        _program = BuiltinShaderProgram.Load("BlitShader")
                   ?? throw new InvalidOperationException("BlitShader not found");

        var reflection = _program.Reflect();
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _bindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[0]);

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create([_bindGroupLayout]),
        };
        _rootSignature = GraphicsContext.Device.CreateRootSignature(rootSigDesc);
        _inputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);

        _linearSampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest
        });

        var numFrames = (int)GraphicsContext.NumFrames;
        _bindGroups = new BindGroup[numFrames];
        _boundTextures = new Texture?[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _bindGroupLayout
            });
        }

        _rtAttachment = new PinnedArray<RenderingAttachmentDesc>(1);

        Parallel.ForEach(CommonFormats, format => GetOrCreatePipeline(format));
    }

    public void Execute(CommandList commandList, CycledTexture source, CycledTexture dest)
    {
        var frameIndex = GraphicsContext.FrameIndex;
        var sourceTexture = source[frameIndex];
        var destTexture = dest[frameIndex];

        var pipeline = GetOrCreatePipeline(dest.Format);

        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList,
            sourceTexture,
            (uint)ResourceUsageFlagBits.ShaderResource,
            QueueType.Graphics);

        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList,
            destTexture,
            (uint)ResourceUsageFlagBits.RenderTarget,
            QueueType.Graphics);

        UpdateBindGroup(frameIndex, sourceTexture);

        _rtAttachment[0] = new RenderingAttachmentDesc
        {
            Resource = destTexture,
            LoadOp = LoadOp.DontCare,
            StoreOp = StoreOp.Store
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachment.Handle, 1),
            NumLayers = 1
        };

        commandList.BeginRendering(renderingDesc);
        commandList.BindViewport(0, 0, dest.Width, dest.Height);
        commandList.BindScissorRect(0, 0, dest.Width, dest.Height);
        commandList.BindPipeline(pipeline);
        commandList.BindGroup(_bindGroups[frameIndex]);
        commandList.Draw(3, 1, 0, 0);
        commandList.EndRendering();
    }

    private Pipeline GetOrCreatePipeline(Format format)
    {
        lock (_pipelineLock)
        {
            if (_pipelines.TryGetValue(format, out var existing))
            {
                return existing;
            }

            var blendDesc = new BlendDesc
            {
                Enable = false,
                RenderTargetWriteMask = 0x0F
            };

            var renderTarget = new RenderTargetDesc
            {
                Format = format,
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
                    RenderTargets = renderTargets
                }
            };

            var pipeline = GraphicsContext.Device.CreatePipeline(pipelineDesc);
            _pipelines[format] = pipeline;
            return pipeline;
        }
    }

    private void UpdateBindGroup(int frameIndex, Texture sourceTexture)
    {
        if (_boundTextures[frameIndex] == sourceTexture)
        {
            return;
        }

        var bindGroup = _bindGroups[frameIndex];
        bindGroup.BeginUpdate();
        bindGroup.SrvTexture(0, sourceTexture);
        bindGroup.SrvTexture(1, ColorTexture.Empty.Texture);
        bindGroup.Sampler(0, _linearSampler);
        bindGroup.EndUpdate();

        _boundTextures[frameIndex] = sourceTexture;
    }

    public void Dispose()
    {
        _rtAttachment.Dispose();

        foreach (var bindGroup in _bindGroups)
        {
            bindGroup.Dispose();
        }

        foreach (var pipeline in _pipelines.Values)
        {
            pipeline.Dispose();
        }

        _pipelines.Clear();

        _linearSampler.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        _bindGroupLayout.Dispose();
        _program.Dispose();
    }
}