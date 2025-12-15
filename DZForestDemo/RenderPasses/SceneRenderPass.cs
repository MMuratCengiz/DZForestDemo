using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using ECS;
using ECS.Components;
using Graphics;
using Graphics.RenderGraph;
using RuntimeAssets;

namespace DZForestDemo.RenderPasses;

public sealed class SceneRenderPass : IDisposable
{
    private const int MaxDrawsPerFrame = 512;
    private const int MaxLights = 8;

    private readonly GraphicsContext _ctx;
    private readonly AssetContext _assets;
    private readonly World _world;

    private readonly InputLayout _inputLayout;
    private readonly RootSignature _rootSignature;
    private readonly Pipeline _pipeline;
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    private readonly DenOfIz.Buffer _frameConstantsBuffer;
    private readonly ResourceBindGroup _frameBindGroup;
    private readonly IntPtr _frameBufferMappedPtr;

    private readonly DenOfIz.Buffer _lightConstantsBuffer;
    private readonly ResourceBindGroup _lightBindGroup;
    private readonly IntPtr _lightBufferMappedPtr;

    private readonly RingBuffer _drawConstantsRingBuffer;
    private readonly ResourceBindGroup[] _drawBindGroups;

    private readonly RingBuffer _materialConstantsRingBuffer;
    private readonly ResourceBindGroup[] _materialBindGroups;

    private bool _disposed;

    [StructLayout(LayoutKind.Sequential)]
    private struct FrameConstants
    {
        public Matrix4x4 ViewProjection;
        public Vector3 CameraPosition;
        public float Time;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpuLight
    {
        public Vector3 PositionOrDirection;
        public uint Type;
        public Vector3 Color;
        public float Intensity;
        public float Radius;
        public float InnerConeAngle;
        public float OuterConeAngle;
        public float Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct LightConstants
    {
        public fixed byte Lights[MaxLights * 48];
        public Vector3 AmbientSkyColor;
        public uint NumLights;
        public Vector3 AmbientGroundColor;
        public float AmbientIntensity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DrawConstants
    {
        public Matrix4x4 Model;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MaterialConstants
    {
        public Vector4 BaseColor;
        public float Metallic;
        public float Roughness;
        public float AmbientOcclusion;
        public float Padding;
    }

    private static readonly MaterialConstants DefaultMaterial = new()
    {
        BaseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        Metallic = 0.0f,
        Roughness = 0.5f,
        AmbientOcclusion = 1.0f
    };

    private const uint LightTypeDirectional = 0;
    private const uint LightTypePoint = 1;
    private const uint LightTypeSpot = 2;

    public SceneRenderPass(GraphicsContext ctx, AssetContext assets, World world)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;

        var logicalDevice = ctx.LogicalDevice;

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);

        CreatePipeline(logicalDevice, out _inputLayout, out _rootSignature, out _pipeline);

        _frameConstantsBuffer = logicalDevice.CreateBuffer(new BufferDesc
        {
            Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
            HeapType = HeapType.CpuGpu,
            NumBytes = (ulong)Unsafe.SizeOf<FrameConstants>(),
            DebugName = StringView.Create("FrameConstants")
        });
        _frameBufferMappedPtr = _frameConstantsBuffer.MapMemory();

        _frameBindGroup = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _rootSignature,
            RegisterSpace = 0
        });
        _frameBindGroup.BeginUpdate();
        _frameBindGroup.Cbv(0, _frameConstantsBuffer);
        _frameBindGroup.EndUpdate();

        _lightConstantsBuffer = logicalDevice.CreateBuffer(new BufferDesc
        {
            Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
            HeapType = HeapType.CpuGpu,
            NumBytes = (ulong)Unsafe.SizeOf<LightConstants>(),
            DebugName = StringView.Create("LightConstants")
        });
        _lightBufferMappedPtr = _lightConstantsBuffer.MapMemory();

        _lightBindGroup = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _rootSignature,
            RegisterSpace = 1
        });
        _lightBindGroup.BeginUpdate();
        _lightBindGroup.Cbv(0, _lightConstantsBuffer);
        _lightBindGroup.EndUpdate();

        _drawConstantsRingBuffer = new RingBuffer(new RingBufferDesc
        {
            LogicalDevice = logicalDevice,
            DataNumBytes = (nuint)Unsafe.SizeOf<DrawConstants>(),
            NumEntries = MaxDrawsPerFrame,
            MaxChunkNumBytes = 65536
        });

        _drawBindGroups = new ResourceBindGroup[MaxDrawsPerFrame];
        for (var i = 0; i < MaxDrawsPerFrame; i++)
        {
            var bufferView = _drawConstantsRingBuffer.GetBufferView((nuint)i);
            var bindGroup = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature,
                RegisterSpace = 2
            });
            bindGroup.BeginUpdate();
            bindGroup.CbvWithDesc(new BindBufferDesc
            {
                Binding = 0,
                Resource = bufferView.Buffer,
                ResourceOffset = bufferView.Offset
            });
            bindGroup.EndUpdate();
            _drawBindGroups[i] = bindGroup;
        }

        _materialConstantsRingBuffer = new RingBuffer(new RingBufferDesc
        {
            LogicalDevice = logicalDevice,
            DataNumBytes = (nuint)Unsafe.SizeOf<MaterialConstants>(),
            NumEntries = MaxDrawsPerFrame,
            MaxChunkNumBytes = 65536
        });

        _materialBindGroups = new ResourceBindGroup[MaxDrawsPerFrame];
        for (var i = 0; i < MaxDrawsPerFrame; i++)
        {
            var bufferView = _materialConstantsRingBuffer.GetBufferView((nuint)i);
            var bindGroup = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature,
                RegisterSpace = 3
            });
            bindGroup.BeginUpdate();
            bindGroup.CbvWithDesc(new BindBufferDesc
            {
                Binding = 0,
                Resource = bufferView.Buffer,
                ResourceOffset = bufferView.Offset
            });
            bindGroup.EndUpdate();
            _materialBindGroups[i] = bindGroup;
        }
    }

    private void CreatePipeline(LogicalDevice logicalDevice, out InputLayout inputLayout,
        out RootSignature rootSignature, out Pipeline pipeline)
    {
        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(System.Text.Encoding.UTF8.GetBytes(Shaders.Geometry3DVertexShader)),
                    Stage = ShaderStage.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(System.Text.Encoding.UTF8.GetBytes(Shaders.Geometry3DPixelShader)),
                    Stage = ShaderStage.Pixel
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        inputLayout = logicalDevice.CreateInputLayout(reflection.InputLayout);
        rootSignature = logicalDevice.CreateRootSignature(reflection.RootSignature);

        pipeline = logicalDevice.CreatePipeline(new PipelineDesc
        {
            InputLayout = inputLayout,
            RootSignature = rootSignature,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                CullMode = CullMode.BackFace,
                DepthTest = new DepthTest
                {
                    Enable = true,
                    Write = true,
                    CompareOp = CompareOp.Less,
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
    }

    public unsafe void Execute(
        ref RenderPassExecuteContext ctx,
        ResourceHandle sceneRt,
        ResourceHandle depthRt,
        Viewport viewport,
        in Matrix4x4 viewProjection,
        Vector3 cameraPosition,
        float time)
    {
        var cmd = ctx.CommandList;
        var rt = ctx.GetTexture(sceneRt);
        var depth = ctx.GetTexture(depthRt);

        ctx.ResourceTracking.TransitionTexture(cmd, rt,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);
        ctx.ResourceTracking.TransitionTexture(cmd, depth,
            (uint)ResourceUsageFlagBits.DepthWrite, QueueType.Graphics);

        var frameConstants = new FrameConstants
        {
            ViewProjection = viewProjection,
            CameraPosition = cameraPosition,
            Time = time
        };
        Unsafe.Write(_frameBufferMappedPtr.ToPointer(), frameConstants);

        UpdateLightConstants();

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = rt,
            LoadOp = LoadOp.Clear,
            ClearColor = new Float4 { X = 0.05f, Y = 0.05f, Z = 0.08f, W = 1.0f }
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            DepthAttachment = new RenderingAttachmentDesc
            {
                Resource = depth,
                LoadOp = LoadOp.Clear,
                ClearDepthStencil = new Float2 { X = 1.0f, Y = 0.0f }
            },
            NumLayers = 1
        };

        cmd.BeginRendering(renderingDesc);
        cmd.BindPipeline(_pipeline);
        cmd.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        cmd.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);

        cmd.BindResourceGroup(_frameBindGroup);
        cmd.BindResourceGroup(_lightBindGroup);

        var drawIndex = 0;
        foreach (var (entity, mesh, transform) in _world.Query<MeshComponent, Transform>())
        {
            if (!mesh.IsValid || drawIndex >= MaxDrawsPerFrame)
            {
                continue;
            }

            ref readonly var runtimeMesh = ref _assets.GetMeshRef(mesh.Mesh);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            var drawConstants = new DrawConstants { Model = transform.Matrix };
            var drawMappedPtr = _drawConstantsRingBuffer.GetMappedMemory((nuint)drawIndex);
            Unsafe.Write(drawMappedPtr.ToPointer(), drawConstants);

            var materialConstants = GetMaterialConstants(entity);
            var materialMappedPtr = _materialConstantsRingBuffer.GetMappedMemory((nuint)drawIndex);
            Unsafe.Write(materialMappedPtr.ToPointer(), materialConstants);

            cmd.BindResourceGroup(_drawBindGroups[drawIndex]);
            cmd.BindResourceGroup(_materialBindGroups[drawIndex]);

            var vb = runtimeMesh.VertexBuffer;
            var ib = runtimeMesh.IndexBuffer;

            cmd.BindVertexBuffer(vb.View.Buffer, (uint)vb.View.Offset, 0, 0);
            cmd.BindIndexBuffer(ib.View.Buffer, ib.IndexType, ib.View.Offset);
            cmd.DrawIndexed(ib.Count, 1, 0, 0, 0);

            drawIndex++;
        }

        cmd.EndRendering();
    }

    private unsafe void UpdateLightConstants()
    {
        var lightConstants = new LightConstants
        {
            AmbientSkyColor = new Vector3(0.4f, 0.5f, 0.6f),
            AmbientGroundColor = new Vector3(0.2f, 0.18f, 0.15f),
            AmbientIntensity = 0.3f,
            NumLights = 0
        };

        var lightPtr = (GpuLight*)lightConstants.Lights;
        uint lightIndex = 0;

        foreach (var (_, ambient) in _world.Query<AmbientLight>())
        {
            lightConstants.AmbientSkyColor = ambient.SkyColor;
            lightConstants.AmbientGroundColor = ambient.GroundColor;
            lightConstants.AmbientIntensity = ambient.Intensity;
            break;
        }

        foreach (var (_, light) in _world.Query<DirectionalLight>())
        {
            if (lightIndex >= MaxLights) break;

            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = light.Direction,
                Type = LightTypeDirectional,
                Color = light.Color,
                Intensity = light.Intensity,
                Radius = 0,
                InnerConeAngle = 0,
                OuterConeAngle = 0
            };
            lightIndex++;
        }

        foreach (var (_, light, transform) in _world.Query<PointLight, Transform>())
        {
            if (lightIndex >= MaxLights) break;

            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = transform.Position,
                Type = LightTypePoint,
                Color = light.Color,
                Intensity = light.Intensity,
                Radius = light.Radius,
                InnerConeAngle = 0,
                OuterConeAngle = 0
            };
            lightIndex++;
        }

        foreach (var (_, light, transform) in _world.Query<SpotLight, Transform>())
        {
            if (lightIndex >= MaxLights) break;

            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = transform.Position,
                Type = LightTypeSpot,
                Color = light.Color,
                Intensity = light.Intensity,
                Radius = light.Radius,
                InnerConeAngle = MathF.Cos(light.InnerConeAngle),
                OuterConeAngle = MathF.Cos(light.OuterConeAngle)
            };
            lightIndex++;
        }

        lightConstants.NumLights = lightIndex;

        Unsafe.Write(_lightBufferMappedPtr.ToPointer(), lightConstants);
    }

    private MaterialConstants GetMaterialConstants(Entity entity)
    {
        if (_world.TryGetComponent<StandardMaterial>(entity, out var material))
        {
            return new MaterialConstants
            {
                BaseColor = material.BaseColor,
                Metallic = material.Metallic,
                Roughness = material.Roughness,
                AmbientOcclusion = material.AmbientOcclusion
            };
        }

        return DefaultMaterial;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _frameConstantsBuffer.UnmapMemory();
        _lightConstantsBuffer.UnmapMemory();

        foreach (var bindGroup in _drawBindGroups)
        {
            bindGroup?.Dispose();
        }

        foreach (var bindGroup in _materialBindGroups)
        {
            bindGroup?.Dispose();
        }

        _lightBindGroup.Dispose();
        _frameBindGroup.Dispose();

        _materialConstantsRingBuffer.Dispose();
        _drawConstantsRingBuffer.Dispose();
        _lightConstantsBuffer.Dispose();
        _frameConstantsBuffer.Dispose();
        _pipeline.Dispose();
        _rootSignature.Dispose();
        _inputLayout.Dispose();
        _rtAttachments.Dispose();

        GC.SuppressFinalize(this);
    }
}
