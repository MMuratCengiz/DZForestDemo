using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using DenOfIz;
using Graphics;
using Graphics.Batching;
using Graphics.Binding;
using Graphics.Binding.Data;
using Graphics.Binding.Layout;
using Graphics.RenderGraph;
using RuntimeAssets;

namespace DZForestDemo.RenderPasses;

public sealed class SceneRenderPass : IDisposable
{
    private readonly AssetResource _assets;
    private readonly GraphicsContext _ctx;

    private readonly Pipeline _staticPipeline;
    private readonly Pipeline _skinnedPipeline;
    private readonly ShaderProgram _staticProgram;
    private readonly ShaderProgram _skinnedProgram;
    private readonly InputLayout _inputLayout;

    private readonly Sampler _textureSampler;
    private readonly NullTexture _nullTexture;

    private readonly BindGroup[] _materialBindGroups;
    private Texture? _boundAlbedoTexture;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    private bool _disposed;

    public SceneRenderPass(GraphicsContext ctx, AssetResource assets)
    {
        _ctx = ctx;
        _assets = assets;

        var logicalDevice = ctx.LogicalDevice;
        var numFrames = (int)ctx.NumFrames;

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);

        _textureSampler = logicalDevice.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Linear,
            MaxAnisotropy = 8
        });

        _nullTexture = new NullTexture(logicalDevice);

        var rootSig = ctx.RootSignatureStore.Forward;

        (_staticPipeline, _staticProgram, _inputLayout) = CreatePipeline(
            logicalDevice, rootSig.StaticRootSignature, "scene_vs_model.hlsl", "scene_ps.hlsl");

        (_skinnedPipeline, _skinnedProgram, _) = CreatePipeline(
            logicalDevice, rootSig.SkinnedRootSignature, "scene_vs_skinned.hlsl", "scene_ps.hlsl");

        _materialBindGroups = new BindGroup[numFrames];
        for (var i = 0; i < numFrames; i++)
        {
            _materialBindGroups[i] = logicalDevice.CreateBindGroup(new BindGroupDesc
            {
                Layout = ctx.BindGroupLayoutStore.Material
            });

            _materialBindGroups[i].BeginUpdate();
            _materialBindGroups[i].SrvTexture(GpuMaterialLayout.Albedo.Binding, _nullTexture.Texture);
            _materialBindGroups[i].SrvTexture(GpuMaterialLayout.Normal.Binding, _nullTexture.Texture);
            _materialBindGroups[i].SrvTexture(GpuMaterialLayout.Roughness.Binding, _nullTexture.Texture);
            _materialBindGroups[i].SrvTexture(GpuMaterialLayout.Metallic.Binding, _nullTexture.Texture);
            _materialBindGroups[i].Sampler(GpuMaterialLayout.TextureSampler.Binding, _textureSampler);
            _materialBindGroups[i].EndUpdate();
        }
    }

    private (Pipeline, ShaderProgram, InputLayout) CreatePipeline(
        LogicalDevice device, RootSignature rootSignature, string vsFilename, string psFilename)
    {
        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load(vsFilename);
        var psSource = shaderLoader.Load(psFilename);

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(vsSource)),
                    Stage = (uint)ShaderStageFlagBits.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(psSource)),
                    Stage = (uint)ShaderStageFlagBits.Pixel
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        var inputLayout = device.CreateInputLayout(reflection.InputLayout);

        var pipeline = device.CreatePipeline(new PipelineDesc
        {
            InputLayout = inputLayout,
            RootSignature = rootSignature,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                CullMode = CullMode.None,
                DepthTest = new DepthTest
                {
                    Enable = true,
                    Write = true,
                    CompareOp = CompareOp.Less
                },
                RenderTargets = RenderTargetDescArray.Create([
                    new RenderTargetDesc
                    {
                        Format = _ctx.BackBufferFormat,
                        Blend = new BlendDesc { RenderTargetWriteMask = 0x0F }
                    }
                ]),
                DepthStencilAttachmentFormat = Format.D32Float
            }
        });

        return (pipeline, program, inputLayout);
    }

    public void SetActiveTexture(Texture? texture)
    {
        _boundAlbedoTexture = texture;
    }

    public void Execute(
        ref RenderPassExecuteContext ctx,
        GpuView gpuView,
        GpuDrawBatcher drawBatcher,
        AssetResource assets,
        ResourceHandle sceneRt,
        ResourceHandle depthRt,
        Viewport viewport)
    {
        var cmd = ctx.CommandList;
        var rt = ctx.GetTexture(sceneRt);
        var depth = ctx.GetTexture(depthRt);
        var frameIndex = ctx.FrameIndex;

        ctx.ResourceTracking.TransitionTexture(cmd, rt,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);
        ctx.ResourceTracking.TransitionTexture(cmd, depth,
            (uint)ResourceUsageFlagBits.DepthWrite, QueueType.Graphics);

        var activeTexture = _boundAlbedoTexture ?? _nullTexture.Texture;
        UpdateMaterialBindings(frameIndex, activeTexture);

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = rt,
            LoadOp = LoadOp.Clear,
            ClearColor = new Vector4 { X = 0.02f, Y = 0.02f, Z = 0.04f, W = 1.0f }
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            DepthAttachment = new RenderingAttachmentDesc
            {
                Resource = depth,
                LoadOp = LoadOp.Clear,
                ClearDepthStencil = new Vector2 { X = 1.0f, Y = 0.0f }
            },
            NumLayers = 1
        };

        cmd.BeginRendering(renderingDesc);
        cmd.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        cmd.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);

        RenderStaticDraws(cmd, frameIndex, gpuView, drawBatcher, assets);
        RenderSkinnedDraws(cmd, frameIndex, gpuView, drawBatcher, assets);

        cmd.EndRendering();
    }

    private void UpdateMaterialBindings(uint frameIndex, Texture albedo)
    {
        var bg = _materialBindGroups[frameIndex];
        bg.BeginUpdate();
        bg.SrvTexture(GpuMaterialLayout.Albedo.Binding, albedo);
        bg.EndUpdate();
    }

    private void RenderStaticDraws(CommandList cmd, uint frameIndex, GpuView gpuView, GpuDrawBatcher drawBatcher,
        AssetResource assets)
    {
        var draws = drawBatcher.StaticDraws;
        if (draws.Length == 0)
        {
            return;
        }

        cmd.BindPipeline(_staticPipeline);
        cmd.BindGroup(gpuView.GetBindGroup(frameIndex));
        cmd.BindGroup(_materialBindGroups[frameIndex]);
        cmd.BindGroup(drawBatcher.GetInstanceBindGroup(frameIndex));

        foreach (var draw in draws)
        {
            var meshHandle = Unsafe.As<MeshId, RuntimeMeshHandle>(ref Unsafe.AsRef(in draw.Mesh));
            ref readonly var runtimeMesh = ref assets.GetMeshRef(meshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            cmd.BindVertexBuffer(runtimeMesh.VertexBuffer.View.Buffer, runtimeMesh.VertexBuffer.View.Offset,
                runtimeMesh.VertexBuffer.Stride, 0);
            cmd.BindIndexBuffer(runtimeMesh.IndexBuffer.View.Buffer, runtimeMesh.IndexBuffer.IndexType,
                runtimeMesh.IndexBuffer.View.Offset);
            cmd.DrawIndexed(runtimeMesh.IndexBuffer.Count, (uint)draw.InstanceCount, 0, 0, (uint)draw.InstanceOffset);
        }
    }

    private void RenderSkinnedDraws(CommandList cmd, uint frameIndex, GpuView gpuView, GpuDrawBatcher drawBatcher,
        AssetResource assets)
    {
        var draws = drawBatcher.SkinnedDraws;
        if (draws.Length == 0)
        {
            return;
        }

        cmd.BindPipeline(_skinnedPipeline);
        cmd.BindGroup(gpuView.GetBindGroup(frameIndex)); // Space 1: Camera/Lights
        cmd.BindGroup(_materialBindGroups[frameIndex]); // Space 2: Material
        cmd.BindGroup(drawBatcher.GetSkinnedBindGroup(frameIndex)); // Space 3: Instance + Bones

        foreach (var draw in draws)
        {
            var meshHandle = Unsafe.As<MeshId, RuntimeMeshHandle>(ref Unsafe.AsRef(in draw.Mesh));
            ref readonly var runtimeMesh = ref assets.GetMeshRef(meshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            cmd.BindVertexBuffer(runtimeMesh.VertexBuffer.View.Buffer, runtimeMesh.VertexBuffer.View.Offset,
                runtimeMesh.VertexBuffer.Stride, 0);
            cmd.BindIndexBuffer(runtimeMesh.IndexBuffer.View.Buffer, runtimeMesh.IndexBuffer.IndexType,
                runtimeMesh.IndexBuffer.View.Offset);
            cmd.DrawIndexed(runtimeMesh.IndexBuffer.Count, 1, 0, 0, (uint)draw.InstanceOffset);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var bg in _materialBindGroups)
        {
            bg.Dispose();
        }

        _nullTexture.Dispose();
        _textureSampler.Dispose();
        _staticPipeline.Dispose();
        _skinnedPipeline.Dispose();
        _staticProgram.Dispose();
        _skinnedProgram.Dispose();
        _inputLayout.Dispose();
        _rtAttachments.Dispose();
    }
}