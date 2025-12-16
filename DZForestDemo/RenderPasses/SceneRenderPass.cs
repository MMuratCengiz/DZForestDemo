using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;
using ECS;
using ECS.Components;
using Graphics;
using Graphics.RenderGraph;
using RuntimeAssets;
using Buffer = DenOfIz.Buffer;

namespace DZForestDemo.RenderPasses;

public sealed class SceneRenderPass : IDisposable
{
    private const int MaxInstancesPerFrame = 4096;
    private const int MaxLights = 8;
    private const int MaxShadowLights = 4;

    private const uint LightTypeDirectional = 0;
    private const uint LightTypePoint = 1;
    private const uint LightTypeSpot = 2;


    private readonly AssetContext _assets;
    private readonly RenderBatcher _batcher;
    private readonly GraphicsContext _ctx;

    private readonly Buffer[] _frameConstantsBuffers;
    private readonly ResourceBindGroup[] _frameBindGroups;
    private readonly IntPtr[] _frameBufferMappedPtrs;

    private readonly Buffer[] _lightConstantsBuffers;
    private readonly ResourceBindGroup[] _lightBindGroups;
    private readonly IntPtr[] _lightBufferMappedPtrs;

    // Per-batch instance buffers (one per unique mesh, per frame)
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameBatchData;

    private readonly InputLayout _inputLayout;
    private readonly Pipeline _pipeline;
    private readonly RootSignature _rootSignature;
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    private readonly Sampler _shadowSampler;
    private readonly World _world;

    private bool _disposed;

    public SceneRenderPass(GraphicsContext ctx, AssetContext assets, World world, RenderBatcher batcher)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _batcher = batcher;

        var logicalDevice = ctx.LogicalDevice;

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);

        CreatePipeline(logicalDevice, out _inputLayout, out _rootSignature, out _pipeline);

        // Create per-frame buffers and bind groups
        var numFrames = (int)ctx.NumFrames;

        _frameConstantsBuffers = new Buffer[numFrames];
        _frameBindGroups = new ResourceBindGroup[numFrames];
        _frameBufferMappedPtrs = new IntPtr[numFrames];

        _lightConstantsBuffers = new Buffer[numFrames];
        _lightBindGroups = new ResourceBindGroup[numFrames];
        _lightBufferMappedPtrs = new IntPtr[numFrames];

        _perFrameBatchData = new Dictionary<RuntimeMeshHandle, BatchInstanceData>[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _frameConstantsBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)Unsafe.SizeOf<FrameConstants>(),
                DebugName = StringView.Create($"FrameConstants_{i}")
            });
            _frameBufferMappedPtrs[i] = _frameConstantsBuffers[i].MapMemory();

            _frameBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature,
                RegisterSpace = 0
            });
            _frameBindGroups[i].BeginUpdate();
            _frameBindGroups[i].Cbv(0, _frameConstantsBuffers[i]);
            _frameBindGroups[i].EndUpdate();

            _lightConstantsBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)Unsafe.SizeOf<LightConstants>(),
                DebugName = StringView.Create($"LightConstants_{i}")
            });
            _lightBufferMappedPtrs[i] = _lightConstantsBuffers[i].MapMemory();

            _lightBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature,
                RegisterSpace = 1
            });
            _lightBindGroups[i].BeginUpdate();
            _lightBindGroups[i].Cbv(0, _lightConstantsBuffers[i]);
            _lightBindGroups[i].EndUpdate();

            _perFrameBatchData[i] = new Dictionary<RuntimeMeshHandle, BatchInstanceData>();
        }

        _shadowSampler = logicalDevice.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToBorder,
            AddressModeV = SamplerAddressMode.ClampToBorder,
            AddressModeW = SamplerAddressMode.ClampToBorder,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest,
            CompareOp = CompareOp.LessOrEqual
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Dispose per-frame resources
        for (var i = 0; i < _frameConstantsBuffers.Length; i++)
        {
            _frameConstantsBuffers[i].UnmapMemory();
            _lightConstantsBuffers[i].UnmapMemory();

            _frameBindGroups[i].Dispose();
            _lightBindGroups[i].Dispose();

            _frameConstantsBuffers[i].Dispose();
            _lightConstantsBuffers[i].Dispose();

            // Dispose per-batch instance data
            foreach (var batchData in _perFrameBatchData[i].Values)
            {
                batchData.Dispose();
            }
        }

        _shadowSampler.Dispose();
        _pipeline.Dispose();
        _rootSignature.Dispose();
        _inputLayout.Dispose();
        _rtAttachments.Dispose();

        GC.SuppressFinalize(this);
    }

    private void CreatePipeline(LogicalDevice logicalDevice, out InputLayout inputLayout,
        out RootSignature rootSignature, out Pipeline pipeline)
    {
        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load("scene_vs.hlsl");
        var psSource = shaderLoader.Load("scene_ps.hlsl");

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(vsSource)),
                    Stage = ShaderStage.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(psSource)),
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
    }

    public ResourceBindGroup CreateShadowBindGroup(Texture shadowAtlas)
    {
        var bindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _rootSignature,
            RegisterSpace = 4
        });
        bindGroup.BeginUpdate();
        bindGroup.SrvTexture(0, shadowAtlas);
        bindGroup.Sampler(0, _shadowSampler);
        bindGroup.EndUpdate();
        return bindGroup;
    }

    public unsafe void Execute(
        ref RenderPassExecuteContext ctx,
        ResourceHandle sceneRt,
        ResourceHandle depthRt,
        ResourceHandle shadowAtlas,
        ResourceBindGroup? shadowBindGroup,
        List<ShadowPass.ShadowData>? shadowData,
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

        if (shadowAtlas.IsValid)
        {
            var atlas = ctx.GetTexture(shadowAtlas);
            ctx.ResourceTracking.TransitionTexture(cmd, atlas,
                (uint)ResourceUsageFlagBits.ShaderResource, QueueType.Graphics);
        }

        var frameIndex = (int)ctx.FrameIndex;

        var frameConstants = new FrameConstants
        {
            ViewProjection = viewProjection,
            CameraPosition = cameraPosition,
            Time = time
        };
        Unsafe.Write(_frameBufferMappedPtrs[frameIndex].ToPointer(), frameConstants);

        UpdateLightConstants(shadowData, frameIndex);

        var batchDataDict = _perFrameBatchData[frameIndex];
        var allInstances = _batcher.AllInstances;

        // Write instance data - material is already cached in batcher, no per-entity lookups!
        foreach (var batch in _batcher.Batches)
        {
            var batchData = GetOrCreateBatchData(batchDataDict, batch.MeshHandle, batch.InstanceCount, frameIndex);
            var instancePtr = (GpuInstanceData*)batchData.MappedPtr.ToPointer();

            for (var i = 0; i < batch.InstanceCount; i++)
            {
                var inst = allInstances[batch.StartInstance + i];
                instancePtr[i] = new GpuInstanceData
                {
                    Model = inst.WorldMatrix,
                    BaseColor = inst.BaseColor,
                    Metallic = inst.Metallic,
                    Roughness = inst.Roughness,
                    AmbientOcclusion = inst.AmbientOcclusion
                };
            }
        }

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = rt,
            LoadOp = LoadOp.Clear,
            ClearColor = new Float4 { X = 0.02f, Y = 0.02f, Z = 0.04f, W = 1.0f }
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

        cmd.BindResourceGroup(_frameBindGroups[frameIndex]);
        cmd.BindResourceGroup(_lightBindGroups[frameIndex]);

        if (shadowBindGroup != null)
        {
            cmd.BindResourceGroup(shadowBindGroup);
        }

        foreach (var batch in _batcher.Batches)
        {
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(batch.MeshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            var batchData = batchDataDict[batch.MeshHandle];
            cmd.BindResourceGroup(batchData.BindGroup);

            var vb = runtimeMesh.VertexBuffer;
            var ib = runtimeMesh.IndexBuffer;

            cmd.BindVertexBuffer(vb.View.Buffer, (uint)vb.View.Offset, 0, 0);
            cmd.BindIndexBuffer(ib.View.Buffer, ib.IndexType, ib.View.Offset);
            cmd.DrawIndexed(ib.Count, (uint)batch.InstanceCount, 0, 0, 0);
        }

        cmd.EndRendering();
    }


    private BatchInstanceData GetOrCreateBatchData(Dictionary<RuntimeMeshHandle, BatchInstanceData> dict, RuntimeMeshHandle meshHandle, int requiredCapacity, int frameIndex)
    {
        if (dict.TryGetValue(meshHandle, out var existing) && existing.Capacity >= requiredCapacity)
        {
            return existing;
        }

        existing?.Dispose();

        var capacity = Math.Max(requiredCapacity, 64);
        var stride = (ulong)Unsafe.SizeOf<GpuInstanceData>();
        var buffer = _ctx.LogicalDevice.CreateBuffer(new BufferDesc
        {
            Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
            HeapType = HeapType.CpuGpu,
            NumBytes = stride * (ulong)capacity,
            StructureDesc = new StructuredBufferDesc
            {
                Offset = 0,
                NumElements = (ulong)capacity,
                Stride = stride
            },
            DebugName = StringView.Create($"BatchInstance_{frameIndex}_{meshHandle.Index}")
        });

        var bindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _rootSignature,
            RegisterSpace = 2
        });
        bindGroup.BeginUpdate();
        bindGroup.SrvBuffer(0, buffer);
        bindGroup.EndUpdate();

        var batchData = new BatchInstanceData
        {
            Buffer = buffer,
            BindGroup = bindGroup,
            MappedPtr = buffer.MapMemory(),
            Capacity = capacity
        };

        dict[meshHandle] = batchData;
        return batchData;
    }

    private unsafe void UpdateLightConstants(List<ShadowPass.ShadowData>? shadowData, int frameIndex)
    {
        var lightConstants = new LightConstants
        {
            AmbientSkyColor = new Vector3(0.4f, 0.5f, 0.6f),
            AmbientGroundColor = new Vector3(0.2f, 0.18f, 0.15f),
            AmbientIntensity = 0.3f,
            NumLights = 0,
            NumShadows = 0
        };

        var lightPtr = (GpuLight*)lightConstants.Lights;
        var shadowPtr = (GpuShadowData*)lightConstants.Shadows;
        uint lightIndex = 0;
        var shadowIndex = 0;

        if (shadowData != null)
        {
            foreach (var shadow in shadowData)
            {
                if (shadowIndex >= MaxShadowLights)
                {
                    break;
                }

                shadowPtr[shadowIndex] = new GpuShadowData
                {
                    LightViewProjection = shadow.LightViewProjection,
                    AtlasScaleOffset = shadow.AtlasScaleOffset,
                    Bias = shadow.Bias,
                    NormalBias = shadow.NormalBias
                };
                shadowIndex++;
            }

            lightConstants.NumShadows = (uint)shadowIndex;
        }

        foreach (var (_, ambient) in _world.Query<AmbientLight>())
        {
            lightConstants.AmbientSkyColor = ambient.SkyColor;
            lightConstants.AmbientGroundColor = ambient.GroundColor;
            lightConstants.AmbientIntensity = ambient.Intensity;
            break;
        }

        var currentShadowIndex = 0;

        foreach (var (_, light) in _world.Query<DirectionalLight>())
        {
            if (lightIndex >= MaxLights)
            {
                break;
            }

            var hasShadow = light.CastShadows && currentShadowIndex < shadowIndex;
            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = light.Direction,
                Type = LightTypeDirectional,
                Color = light.Color,
                Intensity = light.Intensity,
                Radius = 0,
                InnerConeAngle = 0,
                OuterConeAngle = 0,
                ShadowIndex = hasShadow ? currentShadowIndex++ : -1
            };
            lightIndex++;
        }

        foreach (var (_, light, transform) in _world.Query<PointLight, Transform>())
        {
            if (lightIndex >= MaxLights)
            {
                break;
            }

            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = transform.Position,
                Type = LightTypePoint,
                Color = light.Color,
                Intensity = light.Intensity,
                Radius = light.Radius,
                InnerConeAngle = 0,
                OuterConeAngle = 0,
                ShadowIndex = -1
            };
            lightIndex++;
        }

        foreach (var (_, light, transform) in _world.Query<SpotLight, Transform>())
        {
            if (lightIndex >= MaxLights)
            {
                break;
            }

            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = transform.Position,
                Type = LightTypeSpot,
                Color = light.Color,
                Intensity = light.Intensity,
                Radius = light.Radius,
                InnerConeAngle = MathF.Cos(light.InnerConeAngle),
                OuterConeAngle = MathF.Cos(light.OuterConeAngle),
                ShadowIndex = -1
            };
            lightIndex++;
        }

        lightConstants.NumLights = lightIndex;

        Unsafe.Write(_lightBufferMappedPtrs[frameIndex].ToPointer(), lightConstants);
    }

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
        public int ShadowIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpuShadowData
    {
        public Matrix4x4 LightViewProjection;
        public Vector4 AtlasScaleOffset;
        public float Bias;
        public float NormalBias;
        public Vector2 Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct LightConstants
    {
        public fixed byte Lights[MaxLights * 48];
        public fixed byte Shadows[MaxShadowLights * 96];
        public Vector3 AmbientSkyColor;
        public uint NumLights;
        public Vector3 AmbientGroundColor;
        public float AmbientIntensity;
        public uint NumShadows;
        public uint Pad0;
        public uint Pad1;
        public uint Pad2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GpuInstanceData
    {
        public Matrix4x4 Model; // 64 bytes
        public Vector4 BaseColor; // 16 bytes
        public float Metallic; // 4 bytes
        public float Roughness; // 4 bytes
        public float AmbientOcclusion; // 4 bytes
        public float Padding; // 4 bytes -> 96 bytes total
    }

    private sealed class BatchInstanceData : IDisposable
    {
        public Buffer Buffer;
        public ResourceBindGroup BindGroup;
        public IntPtr MappedPtr;
        public int Capacity;

        public void Dispose()
        {
            Buffer.UnmapMemory();
            BindGroup.Dispose();
            Buffer.Dispose();
        }
    }
}