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
    private const int MaxBones = 128;

    private const uint LightTypeDirectional = 0;
    private const uint LightTypePoint = 1;
    private const uint LightTypeSpot = 2;


    private readonly AssetResource _assets;
    private readonly RenderBatcher _batcher;
    private readonly GraphicsResource _ctx;

    private readonly Buffer[] _frameConstantsBuffers;
    private readonly ResourceBindGroup[] _frameBindGroups;
    private readonly IntPtr[] _frameBufferMappedPtrs;

    private readonly Buffer[] _lightConstantsBuffers;
    private readonly ResourceBindGroup[] _lightBindGroups;
    private readonly IntPtr[] _lightBufferMappedPtrs;

    // Per-batch instance buffers (one per unique mesh, per frame)
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameBatchData;

    // Pipeline variants for different mesh types
    private readonly InputLayout _geometryInputLayout;
    private readonly InputLayout _modelInputLayout;
    private readonly Pipeline _geometryPipeline;
    private readonly Pipeline _modelPipeline;
    private readonly RootSignature _rootSignature;
    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    // Skinned mesh rendering
    private readonly Pipeline _skinnedPipeline;
    private readonly RootSignature _skinnedRootSignature;
    private readonly Buffer[] _boneMatricesBuffers;
    private readonly ResourceBindGroup[] _boneMatricesBindGroups;
    private readonly IntPtr[] _boneMatricesMappedPtrs;

    // Skinned pipeline bind groups (same underlying resources, different root signature)
    private readonly ResourceBindGroup[] _skinnedFrameBindGroups;
    private readonly ResourceBindGroup[] _skinnedLightBindGroups;
    private readonly ResourceBindGroup[] _skinnedTextureBindGroups;
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameSkinnedBatchData;
    private ResourceBindGroup? _skinnedShadowBindGroup;

    private readonly Sampler _shadowSampler;
    private readonly Sampler _textureSampler;
    private readonly World _world;

    // Texture support
    private readonly NullTexture _nullTexture;
    private readonly ResourceBindGroup[] _textureBindGroups;
    private Texture? _activeTexture;

    private bool _disposed;

    public SceneRenderPass(GraphicsResource ctx, AssetResource assets, World world, RenderBatcher batcher)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _batcher = batcher;

        var logicalDevice = ctx.LogicalDevice;

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);

        CreatePipeline(logicalDevice, "scene_vs.hlsl", out _geometryInputLayout, out var rootSig,
            out _geometryPipeline);
        _rootSignature = rootSig!;

        CreatePipeline(logicalDevice, "scene_vs_model.hlsl", out _modelInputLayout, out _, out _modelPipeline);
        CreatePipeline(logicalDevice, "scene_vs_skinned.hlsl", out _, out var skinnedRootSig, out _skinnedPipeline,
            forceNewRootSignature: true);
        _skinnedRootSignature = skinnedRootSig!;

        var numFrames = (int)ctx.NumFrames;

        _boneMatricesBuffers = new Buffer[numFrames];
        _boneMatricesBindGroups = new ResourceBindGroup[numFrames];
        _boneMatricesMappedPtrs = new IntPtr[numFrames];

        _skinnedFrameBindGroups = new ResourceBindGroup[numFrames];
        _skinnedLightBindGroups = new ResourceBindGroup[numFrames];
        _skinnedTextureBindGroups = new ResourceBindGroup[numFrames];
        _perFrameSkinnedBatchData = new Dictionary<RuntimeMeshHandle, BatchInstanceData>[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _boneMatricesBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)(MaxBones * Unsafe.SizeOf<Matrix4x4>()),
                DebugName = StringView.Create($"BoneMatrices_{i}")
            });
            _boneMatricesMappedPtrs[i] = _boneMatricesBuffers[i].MapMemory();

            _boneMatricesBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _skinnedRootSignature,
                RegisterSpace = 5
            });
            _boneMatricesBindGroups[i].BeginUpdate();
            _boneMatricesBindGroups[i].Cbv(0, _boneMatricesBuffers[i]);
            _boneMatricesBindGroups[i].EndUpdate();

            _perFrameSkinnedBatchData[i] = new Dictionary<RuntimeMeshHandle, BatchInstanceData>();
        }

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
        _textureBindGroups = new ResourceBindGroup[numFrames];
        for (var i = 0; i < numFrames; i++)
        {
            _textureBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature,
                RegisterSpace = 3
            });
            _textureBindGroups[i].BeginUpdate();
            _textureBindGroups[i].SrvTexture(0, _nullTexture.Texture);
            _textureBindGroups[i].Sampler(0, _textureSampler);
            _textureBindGroups[i].EndUpdate();
        }

        // Create skinned pipeline bind groups (same resources, different root signature)
        for (var i = 0; i < numFrames; i++)
        {
            _skinnedFrameBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _skinnedRootSignature,
                RegisterSpace = 0
            });
            _skinnedFrameBindGroups[i].BeginUpdate();
            _skinnedFrameBindGroups[i].Cbv(0, _frameConstantsBuffers[i]);
            _skinnedFrameBindGroups[i].EndUpdate();

            _skinnedLightBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _skinnedRootSignature,
                RegisterSpace = 1
            });
            _skinnedLightBindGroups[i].BeginUpdate();
            _skinnedLightBindGroups[i].Cbv(0, _lightConstantsBuffers[i]);
            _skinnedLightBindGroups[i].EndUpdate();

            _skinnedTextureBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _skinnedRootSignature,
                RegisterSpace = 3
            });
            _skinnedTextureBindGroups[i].BeginUpdate();
            _skinnedTextureBindGroups[i].SrvTexture(0, _nullTexture.Texture);
            _skinnedTextureBindGroups[i].Sampler(0, _textureSampler);
            _skinnedTextureBindGroups[i].EndUpdate();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        for (var i = 0; i < _frameConstantsBuffers.Length; i++)
        {
            _frameConstantsBuffers[i].UnmapMemory();
            _lightConstantsBuffers[i].UnmapMemory();
            _boneMatricesBuffers[i].UnmapMemory();

            _frameBindGroups[i].Dispose();
            _lightBindGroups[i].Dispose();
            _textureBindGroups[i].Dispose();
            _boneMatricesBindGroups[i].Dispose();

            // Dispose skinned bind groups
            _skinnedFrameBindGroups[i].Dispose();
            _skinnedLightBindGroups[i].Dispose();
            _skinnedTextureBindGroups[i].Dispose();

            _frameConstantsBuffers[i].Dispose();
            _lightConstantsBuffers[i].Dispose();
            _boneMatricesBuffers[i].Dispose();

            foreach (var batchData in _perFrameBatchData[i].Values)
            {
                batchData.Dispose();
            }

            foreach (var batchData in _perFrameSkinnedBatchData[i].Values)
            {
                batchData.Dispose();
            }
        }

        _skinnedShadowBindGroup?.Dispose();

        _nullTexture.Dispose();
        _textureSampler.Dispose();
        _shadowSampler.Dispose();
        _geometryPipeline.Dispose();
        _modelPipeline.Dispose();
        _skinnedPipeline.Dispose();
        _rootSignature.Dispose();
        _skinnedRootSignature.Dispose();
        _geometryInputLayout.Dispose();
        _modelInputLayout.Dispose();
        _rtAttachments.Dispose();

        GC.SuppressFinalize(this);
    }

    private void CreatePipeline(LogicalDevice logicalDevice, string vsFilename, out InputLayout inputLayout,
        out RootSignature? rootSignature, out Pipeline pipeline, bool forceNewRootSignature = false)
    {
        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load(vsFilename);
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

        rootSignature = (forceNewRootSignature || _rootSignature == null)
            ? logicalDevice.CreateRootSignature(reflection.RootSignature)
            : null;

        var effectiveRootSig = rootSignature ?? _rootSignature!;
        pipeline = logicalDevice.CreatePipeline(new PipelineDesc
        {
            InputLayout = inputLayout,
            RootSignature = effectiveRootSig,
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

        // Also create skinned shadow bind group
        _skinnedShadowBindGroup?.Dispose();
        _skinnedShadowBindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _skinnedRootSignature,
            RegisterSpace = 4
        });
        _skinnedShadowBindGroup.BeginUpdate();
        _skinnedShadowBindGroup.SrvTexture(0, shadowAtlas);
        _skinnedShadowBindGroup.Sampler(0, _shadowSampler);
        _skinnedShadowBindGroup.EndUpdate();

        return bindGroup;
    }

    public void SetActiveTexture(Texture? texture)
    {
        var newTexture = texture ?? _nullTexture.Texture;
        if (_activeTexture == newTexture)
        {
            return;
        }

        _activeTexture = newTexture;
        for (var i = 0; i < _textureBindGroups.Length; i++)
        {
            _textureBindGroups[i].BeginUpdate();
            _textureBindGroups[i].SrvTexture(0, newTexture);
            _textureBindGroups[i].Sampler(0, _textureSampler);
            _textureBindGroups[i].EndUpdate();

            // Also update skinned texture bind groups
            _skinnedTextureBindGroups[i].BeginUpdate();
            _skinnedTextureBindGroups[i].SrvTexture(0, newTexture);
            _skinnedTextureBindGroups[i].Sampler(0, _textureSampler);
            _skinnedTextureBindGroups[i].EndUpdate();
        }
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
                    AmbientOcclusion = inst.AmbientOcclusion,
                    UseAlbedoTexture = inst.AlbedoTexture.IsValid ? 1u : 0u
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
        cmd.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        cmd.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);

        cmd.BindResourceGroup(_frameBindGroups[frameIndex]);
        cmd.BindResourceGroup(_lightBindGroups[frameIndex]);
        cmd.BindResourceGroup(_textureBindGroups[frameIndex]);

        if (shadowBindGroup != null)
        {
            cmd.BindResourceGroup(shadowBindGroup);
        }

        var currentMeshType = (MeshType)255;
        foreach (var batch in _batcher.Batches)
        {
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(batch.MeshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            if (runtimeMesh.MeshType != currentMeshType)
            {
                currentMeshType = runtimeMesh.MeshType;
                var pipeline = currentMeshType == MeshType.Geometry ? _geometryPipeline : _modelPipeline;
                cmd.BindPipeline(pipeline);
            }

            var batchData = batchDataDict[batch.MeshHandle];
            cmd.BindResourceGroup(batchData.BindGroup);

            var vb = runtimeMesh.VertexBuffer;
            var ib = runtimeMesh.IndexBuffer;

            cmd.BindVertexBuffer(vb.View.GetBuffer(), (uint)vb.View.Offset, vb.Stride, 0);
            cmd.BindIndexBuffer(ib.View.GetBuffer(), ib.IndexType, ib.View.Offset);
            cmd.DrawIndexed(ib.Count, (uint)batch.InstanceCount, 0, 0, 0);
        }

        RenderAnimatedInstances(cmd, frameIndex);
        cmd.EndRendering();
    }

    private unsafe void RenderAnimatedInstances(CommandList cmd, int frameIndex)
    {
        var animatedInstances = _batcher.AnimatedInstances;
        if (animatedInstances.Count == 0)
        {
            return;
        }

        cmd.BindPipeline(_skinnedPipeline);

        // Bind all required resources for skinned pipeline
        cmd.BindResourceGroup(_skinnedFrameBindGroups[frameIndex]);
        cmd.BindResourceGroup(_skinnedLightBindGroups[frameIndex]);
        cmd.BindResourceGroup(_skinnedTextureBindGroups[frameIndex]);

        if (_skinnedShadowBindGroup != null)
        {
            cmd.BindResourceGroup(_skinnedShadowBindGroup);
        }

        var boneMatricesPtr = (Matrix4x4*)_boneMatricesMappedPtrs[frameIndex].ToPointer();
        var skinnedBatchDataDict = _perFrameSkinnedBatchData[frameIndex];

        foreach (var animInst in animatedInstances)
        {
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(animInst.MeshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            if (animInst.BoneMatrices != null)
            {
                var numBones = Math.Min(animInst.BoneMatrices.NumBones, MaxBones);
                for (var i = 0; i < numBones; i++)
                {
                    boneMatricesPtr[i] = animInst.BoneMatrices.FinalBoneMatrices[i];
                }
            }
            else
            {
                for (var i = 0; i < MaxBones; i++)
                {
                    boneMatricesPtr[i] = Matrix4x4.Identity;
                }
            }

            cmd.BindResourceGroup(_boneMatricesBindGroups[frameIndex]);

            var batchData = GetOrCreateSkinnedBatchData(skinnedBatchDataDict, animInst.MeshHandle, 1, frameIndex);
            var instancePtr = (GpuInstanceData*)batchData.MappedPtr.ToPointer();
            instancePtr[0] = new GpuInstanceData
            {
                Model = animInst.WorldMatrix,
                BaseColor = animInst.BaseColor,
                Metallic = animInst.Metallic,
                Roughness = animInst.Roughness,
                AmbientOcclusion = animInst.AmbientOcclusion,
                UseAlbedoTexture = animInst.AlbedoTexture.IsValid ? 1u : 0u
            };

            cmd.BindResourceGroup(batchData.BindGroup);

            var vb = runtimeMesh.VertexBuffer;
            var ib = runtimeMesh.IndexBuffer;

            cmd.BindVertexBuffer(vb.View.GetBuffer(), (uint)vb.View.Offset, vb.Stride, 0);
            cmd.BindIndexBuffer(ib.View.GetBuffer(), ib.IndexType, ib.View.Offset);
            cmd.DrawIndexed(ib.Count, 1, 0, 0, 0);
        }
    }


    private BatchInstanceData GetOrCreateBatchData(Dictionary<RuntimeMeshHandle, BatchInstanceData> dict,
        RuntimeMeshHandle meshHandle, int requiredCapacity, int frameIndex)
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
        (
            buffer,
            bindGroup,
            buffer.MapMemory(),
            capacity
        );

        dict[meshHandle] = batchData;
        return batchData;
    }

    private BatchInstanceData GetOrCreateSkinnedBatchData(Dictionary<RuntimeMeshHandle, BatchInstanceData> dict,
        RuntimeMeshHandle meshHandle, int requiredCapacity, int frameIndex)
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
            DebugName = StringView.Create($"SkinnedBatchInstance_{frameIndex}_{meshHandle.Index}")
        });

        // Use skinned root signature for skinned mesh instances
        var bindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _skinnedRootSignature,
            RegisterSpace = 2
        });
        bindGroup.BeginUpdate();
        bindGroup.SrvBuffer(0, buffer);
        bindGroup.EndUpdate();

        var batchData = new BatchInstanceData
        (
            buffer,
            bindGroup,
            buffer.MapMemory(),
            capacity
        );

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
                SpotDirection = Vector3.Zero,
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

            var hasShadow = currentShadowIndex < shadowIndex;
            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = transform.Position,
                Type = LightTypePoint,
                Color = light.Color,
                Intensity = light.Intensity,
                SpotDirection = Vector3.Zero,
                Radius = light.Radius,
                InnerConeAngle = 0,
                OuterConeAngle = 0,
                ShadowIndex = hasShadow ? currentShadowIndex++ : -1
            };
            lightIndex++;
        }

        foreach (var (_, light, transform) in _world.Query<SpotLight, Transform>())
        {
            if (lightIndex >= MaxLights)
            {
                break;
            }

            var hasShadow = currentShadowIndex < shadowIndex;
            lightPtr[lightIndex] = new GpuLight
            {
                PositionOrDirection = transform.Position,
                Type = LightTypeSpot,
                Color = light.Color,
                Intensity = light.Intensity,
                SpotDirection = light.Direction,
                Radius = light.Radius,
                InnerConeAngle = MathF.Cos(light.InnerConeAngle),
                OuterConeAngle = MathF.Cos(light.OuterConeAngle),
                ShadowIndex = hasShadow ? currentShadowIndex++ : -1
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
        public Vector3 SpotDirection;
        public float Radius;
        public float InnerConeAngle;
        public float OuterConeAngle;
        public int ShadowIndex;
        public float Pad0;
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
        public fixed byte Lights[MaxLights * 64];
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
        public uint UseAlbedoTexture; // 4 bytes -> 96 bytes total
    }

    private sealed class BatchInstanceData(Buffer buffer, ResourceBindGroup bindGroup, IntPtr mappedPtr, int capacity)
        : IDisposable
    {
        public readonly Buffer Buffer = buffer;
        public readonly ResourceBindGroup BindGroup = bindGroup;
        public readonly IntPtr MappedPtr = mappedPtr;
        public readonly int Capacity = capacity;

        public void Dispose()
        {
            Buffer.UnmapMemory();
            BindGroup.Dispose();
            Buffer.Dispose();
        }
    }
}