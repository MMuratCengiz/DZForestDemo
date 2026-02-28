using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Common;

/// <summary>
/// Screen-space bilateral blur pass that smooths shadow edges while preserving
/// geometry boundaries. Reads scene color + depth, writes to a destination target.
/// </summary>
public class ShadowSmoothPass : IDisposable
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
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachment;

    // Track what's currently bound to avoid redundant updates.
    private readonly Texture?[] _boundColor;
    private readonly Texture?[] _boundDepth;

    public ShadowSmoothPass()
    {
        _program = LoadProgram();

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
        _boundColor = new Texture?[numFrames];
        _boundDepth = new Texture?[numFrames];

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

    private static ShaderProgram LoadProgram()
    {
        if (ShaderHotReload.IsEnabled && ShaderHotReload.EngineShaderDirectory != null)
        {
            var vsPath = Path.Combine(ShaderHotReload.EngineShaderDirectory, "FullscreenQuad.VS.hlsl");
            var psPath = Path.Combine(ShaderHotReload.EngineShaderDirectory, "ShadowSmooth.PS.hlsl");

            if (File.Exists(vsPath) && File.Exists(psPath))
            {
                return CompileFromSource(vsPath, psPath);
            }
        }

        return BuiltinShaderProgram.Load("ShadowSmoothShader")
               ?? throw new InvalidOperationException("ShadowSmoothShader not found");
    }

    private static ShaderProgram CompileFromSource(string vsPath, string psPath)
    {
        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Path = StringView.Create(vsPath),
            EntryPoint = StringView.Create("VSMain")
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Path = StringView.Create(psPath),
            EntryPoint = StringView.Create("PSMain")
        };

        using var stagesArray = ShaderStageDescArray.Create([vsDesc, psDesc]);
        return new ShaderProgram(new ShaderProgramDesc
        {
            ShaderStages = stagesArray
        });
    }

    public void Execute(CommandList commandList, CycledTexture sceneColor, CycledTexture sceneDepth,
        CycledTexture dest)
    {
        var frameIndex = GraphicsContext.FrameIndex;
        Execute(commandList,
            sceneColor[frameIndex], sceneDepth[frameIndex],
            dest[frameIndex], dest.Format, dest.Width, dest.Height);
    }

    private void Execute(CommandList commandList,
        Texture colorSource, Texture depthSource,
        Texture dest, Format destFormat, uint width, uint height)
    {
        var frameIndex = GraphicsContext.FrameIndex;
        var pipeline = GetOrCreatePipeline(destFormat);

        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList, colorSource,
            (uint)ResourceUsageFlagBits.ShaderResource, QueueType.Graphics);
        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList, depthSource,
            (uint)ResourceUsageFlagBits.ShaderResource, QueueType.Graphics);
        GraphicsContext.ResourceTracking.TransitionTexture(
            commandList, dest,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

        UpdateBindGroup(frameIndex, colorSource, depthSource);

        _rtAttachment[0] = new RenderingAttachmentDesc
        {
            Resource = dest,
            LoadOp = LoadOp.Load,
            StoreOp = StoreOp.Store
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachment.Handle, 1),
            NumLayers = 1
        };

        commandList.BeginRendering(renderingDesc);
        commandList.BindViewport(0, 0, width, height);
        commandList.BindScissorRect(0, 0, width, height);
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

    private void UpdateBindGroup(int frameIndex, Texture colorTexture, Texture depthTexture)
    {
        if (_boundColor[frameIndex] == colorTexture && _boundDepth[frameIndex] == depthTexture)
        {
            return;
        }

        var bindGroup = _bindGroups[frameIndex];
        bindGroup.BeginUpdate();
        bindGroup.SrvTexture(0, colorTexture);
        bindGroup.SrvTexture(1, depthTexture);
        bindGroup.Sampler(0, _linearSampler);
        bindGroup.EndUpdate();

        _boundColor[frameIndex] = colorTexture;
        _boundDepth[frameIndex] = depthTexture;
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
