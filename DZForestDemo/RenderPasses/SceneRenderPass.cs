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
    private const int MaxLights = 8;
    private const int MaxShadowLights = 4;
    private const int MaxBones = 128;

    private const uint LightTypeDirectional = 0;
    private const uint LightTypePoint = 1;
    private const uint LightTypeSpot = 2;

    private readonly AssetResource _assets;
    private readonly MyRenderBatcher _batcher;
    private readonly GraphicsResource _ctx;
    private readonly RgCommandList _rgCommandList;
    private readonly World _world;

    // Shader with variants
    private readonly Shader _shader;
    private readonly Shader _skinnedShader;

    // Per-frame buffers
    private readonly Buffer[] _frameConstantsBuffers;
    private readonly IntPtr[] _frameBufferMappedPtrs;

    private readonly Buffer[] _lightConstantsBuffers;
    private readonly IntPtr[] _lightBufferMappedPtrs;

    // Per-batch instance buffers (one per unique mesh, per frame)
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameBatchData;
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameSkinnedBatchData;

    // Skinned mesh rendering
    private readonly Buffer[] _boneMatricesBuffers;
    private readonly IntPtr[] _boneMatricesMappedPtrs;

    // Samplers
    private readonly Sampler _shadowSampler;
    private readonly Sampler _textureSampler;

    // Textures
    private readonly NullTexture _nullTexture;
    private Texture? _activeTexture;
    private Texture? _shadowAtlas;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    private bool _disposed;

    public SceneRenderPass(GraphicsResource ctx, AssetResource assets, World world, MyRenderBatcher batcher, RgCommandList rgCommandList)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _batcher = batcher;
        _rgCommandList = rgCommandList;

        var logicalDevice = ctx.LogicalDevice;
        var numFrames = (int)ctx.NumFrames;

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);

        // Create shaders with proper root signatures
        _shader = CreateShader(logicalDevice, false);
        _skinnedShader = CreateShader(logicalDevice, true);

        // Create per-frame buffers
        _frameConstantsBuffers = new Buffer[numFrames];
        _frameBufferMappedPtrs = new IntPtr[numFrames];
        _lightConstantsBuffers = new Buffer[numFrames];
        _lightBufferMappedPtrs = new IntPtr[numFrames];
        _boneMatricesBuffers = new Buffer[numFrames];
        _boneMatricesMappedPtrs = new IntPtr[numFrames];
        _perFrameBatchData = new Dictionary<RuntimeMeshHandle, BatchInstanceData>[numFrames];
        _perFrameSkinnedBatchData = new Dictionary<RuntimeMeshHandle, BatchInstanceData>[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _frameConstantsBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Usage = (uint)BufferUsageFlagBits.Uniform,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)Unsafe.SizeOf<FrameConstants>(),
                DebugName = StringView.Create($"FrameConstants_{i}")
            });
            _frameBufferMappedPtrs[i] = _frameConstantsBuffers[i].MapMemory();

            _lightConstantsBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Usage = (uint)BufferUsageFlagBits.Uniform,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)Unsafe.SizeOf<LightConstants>(),
                DebugName = StringView.Create($"LightConstants_{i}")
            });
            _lightBufferMappedPtrs[i] = _lightConstantsBuffers[i].MapMemory();

            _boneMatricesBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Usage = (uint)BufferUsageFlagBits.Uniform,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)(MaxBones * Unsafe.SizeOf<Matrix4x4>()),
                DebugName = StringView.Create($"BoneMatrices_{i}")
            });
            _boneMatricesMappedPtrs[i] = _boneMatricesBuffers[i].MapMemory();

            _perFrameBatchData[i] = new Dictionary<RuntimeMeshHandle, BatchInstanceData>();
            _perFrameSkinnedBatchData[i] = new Dictionary<RuntimeMeshHandle, BatchInstanceData>();
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
    }

    private Shader CreateShader(LogicalDevice logicalDevice, bool isSkinned)
    {
        var shaderLoader = new ShaderLoader();
        var psSource = shaderLoader.Load("scene_ps.hlsl");

        // Build root signature with all bindings
        // space0 (Never): FrameConstants (b0), LightConstants (b1)
        // space1 (PerCamera): ShadowAtlas (t0), AlbedoTexture (t1)
        // space3 (PerDraw): Instances (t0), BoneMatrices (b0)
        // space5 (Samplers): ShadowSampler (s0), AlbedoSampler (s1)
        var rootSigBuilder = new ShaderRootSignature.Builder(logicalDevice)
            // space0: Frame constants
            .AddBinding("FrameConstants", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 0,
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                Stages = (uint)ShaderStageFlagBits.AllGraphics,
                ArraySize = 1
            })
            .AddBinding("LightConstants", new ResourceBindingDesc
            {
                Binding = 1,
                RegisterSpace = 0,
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                Stages = (uint)ShaderStageFlagBits.AllGraphics,
                ArraySize = 1
            })
            // space1: Textures (SRVs only)
            .AddBinding("ShadowAtlas", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 1,
                Descriptor = (uint)ResourceDescriptorFlagBits.Texture,
                Stages = (uint)ShaderStageFlagBits.Pixel,
                ArraySize = 1
            })
            .AddBinding("AlbedoTexture", new ResourceBindingDesc
            {
                Binding = 1,
                RegisterSpace = 1,
                Descriptor = (uint)ResourceDescriptorFlagBits.Texture,
                Stages = (uint)ShaderStageFlagBits.Pixel,
                ArraySize = 1
            })
            // space3: Per-draw data
            .AddBinding("Instances", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 3,
                Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
                Stages = (uint)ShaderStageFlagBits.AllGraphics,
                ArraySize = 1
            })
            // space5: All samplers
            .AddBinding("ShadowSampler", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 5,
                Descriptor = (uint)ResourceDescriptorFlagBits.Sampler,
                Stages = (uint)ShaderStageFlagBits.Pixel,
                ArraySize = 1
            })
            .AddBinding("AlbedoSampler", new ResourceBindingDesc
            {
                Binding = 1,
                RegisterSpace = 5,
                Descriptor = (uint)ResourceDescriptorFlagBits.Sampler,
                Stages = (uint)ShaderStageFlagBits.Pixel,
                ArraySize = 1
            });

        if (isSkinned)
        {
            rootSigBuilder.AddBinding("BoneMatrices", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 3,
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                Stages = (uint)ShaderStageFlagBits.Vertex,
                ArraySize = 1
            });
        }

        var rootSignature = rootSigBuilder.Build();

        var shader = new Shader(rootSignature);

        // Create variants for geometry and model (or skinned)
        if (isSkinned)
        {
            AddVariant(shader, logicalDevice, rootSignature, "scene_vs_skinned.hlsl", psSource, "skinned");
        }
        else
        {
            AddVariant(shader, logicalDevice, rootSignature, "scene_vs.hlsl", psSource, "geometry");
            AddVariant(shader, logicalDevice, rootSignature, "scene_vs_model.hlsl", psSource, "model");
        }

        return shader;
    }

    private void AddVariant(Shader shader, LogicalDevice logicalDevice, ShaderRootSignature rootSignature,
        string vsFilename, string psSource, string variantName)
    {
        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load(vsFilename);

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
        var inputLayout = logicalDevice.CreateInputLayout(reflection.InputLayout);

        var pipeline = logicalDevice.CreatePipeline(new PipelineDesc
        {
            InputLayout = inputLayout,
            RootSignature = rootSignature.Instance,
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

        shader.AddVariant(variantName, new ShaderVariant(pipeline, program));
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

        _nullTexture.Dispose();
        _textureSampler.Dispose();
        _shadowSampler.Dispose();
        _shader.Dispose();
        _skinnedShader.Dispose();
        _rtAttachments.Dispose();

        GC.SuppressFinalize(this);
    }

    public void SetShadowAtlas(Texture shadowAtlas)
    {
        _shadowAtlas = shadowAtlas;
    }

    public void SetActiveTexture(Texture? texture)
    {
        _activeTexture = texture ?? _nullTexture.Texture;
    }

    public unsafe void Execute(
        ref RenderPassExecuteContext ctx,
        ResourceHandle sceneRt,
        ResourceHandle depthRt,
        ResourceHandle shadowAtlas,
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
            _shadowAtlas = atlas;
        }

        var frameIndex = (int)ctx.FrameIndex;

        // Update frame constants
        var frameConstants = new FrameConstants
        {
            ViewProjection = viewProjection,
            CameraPosition = cameraPosition,
            Time = time
        };
        Unsafe.Write(_frameBufferMappedPtrs[frameIndex].ToPointer(), frameConstants);

        // Update light constants
        UpdateLightConstants(shadowData, frameIndex);

        // Update batch instance data
        var batchDataDict = _perFrameBatchData[frameIndex];
        var staticBatcher = _batcher.StaticBatcher;
        var allInstances = staticBatcher.Instances;

        foreach (var batch in staticBatcher.Batches)
        {
            var batchData = GetOrCreateBatchData(batchDataDict, batch.Key, batch.Count, frameIndex);
            var instancePtr = (GpuInstanceData*)batchData.MappedPtr.ToPointer();

            for (var i = 0; i < batch.Count; i++)
            {
                var inst = allInstances[batch.StartIndex + i];
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

        // Begin RgCommandList pass
        _rgCommandList.Begin(cmd, frameIndex);

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

        _rgCommandList.BeginRendering(renderingDesc);
        _rgCommandList.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        _rgCommandList.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);

        var activeTexture = _activeTexture ?? _nullTexture.Texture;

        // Render static batches
        foreach (var batch in staticBatcher.Batches)
        {
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(batch.Key);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            var variant = runtimeMesh.MeshType == MeshType.Geometry ? "geometry" : "model";
            _rgCommandList.SetShader(_shader, variant);

            _rgCommandList.SetBuffer("FrameConstants", _frameConstantsBuffers[frameIndex]);
            _rgCommandList.SetBuffer("LightConstants", _lightConstantsBuffers[frameIndex]);

            var batchData = batchDataDict[batch.Key];
            _rgCommandList.SetBuffer("Instances", batchData.Buffer);

            _rgCommandList.SetTexture("AlbedoTexture", activeTexture);
            _rgCommandList.SetSampler("AlbedoSampler", _textureSampler);

            if (_shadowAtlas != null)
            {
                _rgCommandList.SetTexture("ShadowAtlas", _shadowAtlas);
                _rgCommandList.SetSampler("ShadowSampler", _shadowSampler);
            }

            var gpuMesh = new GPUMesh
            {
                IndexType = runtimeMesh.IndexBuffer.IndexType,
                VertexBuffer = runtimeMesh.VertexBuffer.View,
                IndexBuffer = runtimeMesh.IndexBuffer.View,
                VertexStride = runtimeMesh.VertexBuffer.Stride,
                NumVertices = runtimeMesh.VertexBuffer.Count,
                NumIndices = runtimeMesh.IndexBuffer.Count
            };

            _rgCommandList.DrawMesh(gpuMesh, (uint)batch.Count);
        }

        // Render animated instances
        RenderAnimatedInstances(frameIndex);

        _rgCommandList.End();
    }

    private unsafe void RenderAnimatedInstances(int frameIndex)
    {
        var animatedInstances = _batcher.AnimatedInstances;
        if (animatedInstances.Count == 0)
        {
            return;
        }

        var boneMatricesPtr = (Matrix4x4*)_boneMatricesMappedPtrs[frameIndex].ToPointer();
        var skinnedBatchDataDict = _perFrameSkinnedBatchData[frameIndex];
        var activeTexture = _activeTexture ?? _nullTexture.Texture;

        foreach (var animInst in animatedInstances)
        {
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(animInst.MeshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            // Update bone matrices
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

            // Update instance data
            var batchData = GetOrCreateBatchData(skinnedBatchDataDict, animInst.MeshHandle, 1, frameIndex);
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

            _rgCommandList.SetShader(_skinnedShader, "skinned");

            _rgCommandList.SetBuffer("FrameConstants", _frameConstantsBuffers[frameIndex]);
            _rgCommandList.SetBuffer("LightConstants", _lightConstantsBuffers[frameIndex]);
            _rgCommandList.SetBuffer("Instances", batchData.Buffer);
            _rgCommandList.SetBuffer("BoneMatrices", _boneMatricesBuffers[frameIndex]);

            _rgCommandList.SetTexture("AlbedoTexture", activeTexture);
            _rgCommandList.SetSampler("AlbedoSampler", _textureSampler);

            if (_shadowAtlas != null)
            {
                _rgCommandList.SetTexture("ShadowAtlas", _shadowAtlas);
                _rgCommandList.SetSampler("ShadowSampler", _shadowSampler);
            }

            var gpuMesh = new GPUMesh
            {
                IndexType = runtimeMesh.IndexBuffer.IndexType,
                VertexBuffer = runtimeMesh.VertexBuffer.View,
                IndexBuffer = runtimeMesh.IndexBuffer.View,
                VertexStride = runtimeMesh.VertexBuffer.Stride,
                NumVertices = runtimeMesh.VertexBuffer.Count,
                NumIndices = runtimeMesh.IndexBuffer.Count
            };

            _rgCommandList.DrawMesh(gpuMesh, 1);
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

        var batchData = new BatchInstanceData(buffer, buffer.MapMemory(), capacity);
        dict[meshHandle] = batchData;
        return batchData;
    }

    private unsafe void UpdateLightConstants(List<ShadowPass.ShadowData>? shadowData, int frameIndex)
    {
        var ambientSkyColor = new Vector3(0.4f, 0.5f, 0.6f);
        var ambientGroundColor = new Vector3(0.2f, 0.18f, 0.15f);
        var ambientIntensity = 0.3f;

        var lights = stackalloc GpuLight[MaxLights];
        var shadows = stackalloc GpuShadowData[MaxShadowLights];
        var lightIndex = 0;
        var shadowIndex = 0;

        if (shadowData != null)
        {
            foreach (var shadow in shadowData)
            {
                if (shadowIndex >= MaxShadowLights)
                {
                    break;
                }

                shadows[shadowIndex] = new GpuShadowData
                {
                    LightViewProjection = shadow.LightViewProjection,
                    AtlasScaleOffset = shadow.AtlasScaleOffset,
                    Bias = shadow.Bias,
                    NormalBias = shadow.NormalBias
                };
                shadowIndex++;
            }
        }

        foreach (var (_, ambient) in _world.Query<AmbientLight>())
        {
            ambientSkyColor = ambient.SkyColor;
            ambientGroundColor = ambient.GroundColor;
            ambientIntensity = ambient.Intensity;
        }

        var currentShadowIndex = 0;
        var numShadows = shadowIndex;

        foreach (var (_, light) in _world.Query<DirectionalLight>())
        {
            if (lightIndex >= MaxLights)
            {
                break;
            }

            var hasShadow = light.CastShadows && currentShadowIndex < numShadows;
            lights[lightIndex] = new GpuLight
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

            var hasShadow = currentShadowIndex < numShadows;
            lights[lightIndex] = new GpuLight
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

            var hasShadow = currentShadowIndex < numShadows;
            lights[lightIndex] = new GpuLight
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

        var lightConstants = new LightConstants
        {
            AmbientSkyColor = ambientSkyColor,
            AmbientGroundColor = ambientGroundColor,
            AmbientIntensity = ambientIntensity,
            NumLights = (uint)lightIndex,
            NumShadows = (uint)shadowIndex
        };

        var lightPtr = (GpuLight*)lightConstants.Lights;
        var shadowPtr = (GpuShadowData*)lightConstants.Shadows;

        for (var i = 0; i < lightIndex; i++)
        {
            lightPtr[i] = lights[i];
        }

        for (var i = 0; i < shadowIndex; i++)
        {
            shadowPtr[i] = shadows[i];
        }

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
        public Matrix4x4 Model;
        public Vector4 BaseColor;
        public float Metallic;
        public float Roughness;
        public float AmbientOcclusion;
        public uint UseAlbedoTexture;
    }

    private sealed class BatchInstanceData(Buffer buffer, IntPtr mappedPtr, int capacity) : IDisposable
    {
        public readonly Buffer Buffer = buffer;
        public readonly IntPtr MappedPtr = mappedPtr;
        public readonly int Capacity = capacity;

        public void Dispose()
        {
            Buffer.UnmapMemory();
            Buffer.Dispose();
        }
    }
}
