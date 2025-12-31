using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;
using Graphics;
using Graphics.Batching;
using Graphics.Binding;
using Graphics.RenderGraph;
using RuntimeAssets;
using Buffer = DenOfIz.Buffer;

namespace DZForestDemo.RenderPasses;

public sealed class SceneRenderPass : IDisposable
{
    private const int MaxLights = 8;
    private const int MaxShadowLights = 4;
    private const int MaxBones = 128;
    private const int MaxDrawsPerFrame = 4096;

    private const uint LightTypeDirectional = 0;
    private const uint LightTypePoint = 1;
    private const uint LightTypeSpot = 2;

    private readonly AssetResource _assets;
    private readonly IGraphicsContext _ctx;

    private readonly Shader _shader;
    private readonly Shader _skinnedShader;

    private readonly Buffer[] _frameConstantsBuffers;
    private readonly IntPtr[] _frameBufferMappedPtrs;

    private readonly Buffer[] _lightConstantsBuffers;
    private readonly IntPtr[] _lightBufferMappedPtrs;

    private readonly Buffer[] _boneMatricesBuffers;
    private readonly IntPtr[] _boneMatricesMappedPtrs;

    private readonly Sampler _shadowSampler;
    private readonly Sampler _textureSampler;

    private readonly NullTexture _nullTexture;
    private Texture? _activeTexture;
    private Texture? _shadowAtlas;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;

    private readonly FrameBindings _frameBindings;
    private readonly FrameBindings _textureBindings;
    private readonly FrameBindings _samplerBindings;
    private readonly InstanceBuffer<GpuInstanceData> _instanceBuffer;

    private readonly FrameBindings _skinnedFrameBindings;
    private readonly FrameBindings _skinnedTextureBindings;
    private readonly FrameBindings _skinnedSamplerBindings;
    private readonly InstanceBuffer<GpuInstanceData> _skinnedInstanceBuffer;
    private readonly FrameBindings _skinnedBoneBindings;

    private readonly Texture?[] _boundShadowAtlas;
    private readonly Texture?[] _boundActiveTexture;

    private bool _disposed;

    public SceneRenderPass(IGraphicsContext ctx, AssetResource assets)
    {
        _ctx = ctx;
        _assets = assets;

        var logicalDevice = ctx.LogicalDevice;
        var numFrames = (int)ctx.NumFrames;

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);
        _boundShadowAtlas = new Texture?[numFrames];
        _boundActiveTexture = new Texture?[numFrames];

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

        _shader = CreateShader(logicalDevice, false);
        _skinnedShader = CreateShader(logicalDevice, true);

        _frameConstantsBuffers = new Buffer[numFrames];
        _frameBufferMappedPtrs = new IntPtr[numFrames];
        _lightConstantsBuffers = new Buffer[numFrames];
        _lightBufferMappedPtrs = new IntPtr[numFrames];
        _boneMatricesBuffers = new Buffer[numFrames];
        _boneMatricesMappedPtrs = new IntPtr[numFrames];

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
        }

        _frameBindings = new FrameBindings(logicalDevice, _shader.RootSignature.Instance, ctx.NumFrames, 0);
        _textureBindings = new FrameBindings(logicalDevice, _shader.RootSignature.Instance, ctx.NumFrames, 1);
        _samplerBindings = new FrameBindings(logicalDevice, _shader.RootSignature.Instance, ctx.NumFrames, 5);
        _instanceBuffer = new InstanceBuffer<GpuInstanceData>(logicalDevice, _shader.RootSignature.Instance, 3, 0, MaxDrawsPerFrame, numFrames);

        _skinnedFrameBindings = new FrameBindings(logicalDevice, _skinnedShader.RootSignature.Instance, ctx.NumFrames, 0);
        _skinnedTextureBindings = new FrameBindings(logicalDevice, _skinnedShader.RootSignature.Instance, ctx.NumFrames, 1);
        _skinnedSamplerBindings = new FrameBindings(logicalDevice, _skinnedShader.RootSignature.Instance, ctx.NumFrames, 5);
        _skinnedBoneBindings = new FrameBindings(logicalDevice, _skinnedShader.RootSignature.Instance, ctx.NumFrames, 3);
        _skinnedInstanceBuffer = new InstanceBuffer<GpuInstanceData>(logicalDevice, _skinnedShader.RootSignature.Instance, 3, 0, MaxDrawsPerFrame, numFrames);

        _frameBindings.Setup((bg, i) =>
        {
            bg.Cbv(0, _frameConstantsBuffers[i]);
            bg.Cbv(1, _lightConstantsBuffers[i]);
        });

        _skinnedFrameBindings.Setup((bg, i) =>
        {
            bg.Cbv(0, _frameConstantsBuffers[i]);
            bg.Cbv(1, _lightConstantsBuffers[i]);
        });

        _samplerBindings.Setup((bg, _) =>
        {
            bg.Sampler(0, _shadowSampler);
            bg.Sampler(1, _textureSampler);
        });

        _skinnedSamplerBindings.Setup((bg, _) =>
        {
            bg.Sampler(0, _shadowSampler);
            bg.Sampler(1, _textureSampler);
        });

        _skinnedBoneBindings.Setup((bg, i) =>
        {
            bg.Cbv(1, _boneMatricesBuffers[i]);
        });
    }

    private Shader CreateShader(LogicalDevice logicalDevice, bool isSkinned)
    {
        var shaderLoader = new ShaderLoader();
        var psSource = shaderLoader.Load("scene_ps.hlsl");

        var rootSigBuilder = new ShaderRootSignature.Builder(logicalDevice)
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
            .AddBinding("Instances", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 3,
                Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
                Stages = (uint)ShaderStageFlagBits.AllGraphics,
                ArraySize = 1
            })
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
                Binding = 1,
                RegisterSpace = 3,
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                Stages = (uint)ShaderStageFlagBits.Vertex,
                ArraySize = 1
            });
        }

        var rootSignature = rootSigBuilder.Build();
        var shader = new Shader(rootSignature);

        if (isSkinned)
        {
            AddVariant(shader, logicalDevice, rootSignature, "scene_vs_skinned.hlsl", psSource, "skinned");
        }
        else
        {
            AddVariant(shader, logicalDevice, rootSignature, "scene_vs_model.hlsl", psSource, "static");
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
        }

        _frameBindings.Dispose();
        _textureBindings.Dispose();
        _samplerBindings.Dispose();
        _instanceBuffer.Dispose();

        _skinnedFrameBindings.Dispose();
        _skinnedTextureBindings.Dispose();
        _skinnedSamplerBindings.Dispose();
        _skinnedBoneBindings.Dispose();
        _skinnedInstanceBuffer.Dispose();

        _nullTexture.Dispose();
        _textureSampler.Dispose();
        _shadowSampler.Dispose();
        _shader.Dispose();
        _skinnedShader.Dispose();
        _rtAttachments.Dispose();
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
        RenderScene renderScene,
        ResourceHandle sceneRt,
        ResourceHandle depthRt,
        Viewport viewport,
        float time)
    {
        var cmd = ctx.CommandList;
        var rt = ctx.GetTexture(sceneRt);
        var depth = ctx.GetTexture(depthRt);
        var frameIndex = (int)ctx.FrameIndex;

        ctx.ResourceTracking.TransitionTexture(cmd, rt,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);
        ctx.ResourceTracking.TransitionTexture(cmd, depth,
            (uint)ResourceUsageFlagBits.DepthWrite, QueueType.Graphics);


        ref readonly var mainView = ref renderScene.MainView;
        var frameConstants = new FrameConstants
        {
            ViewProjection = mainView.ViewProjection,
            CameraPosition = mainView.Position,
            Time = time
        };
        Unsafe.Write(_frameBufferMappedPtrs[frameIndex].ToPointer(), frameConstants);

        UpdateLightConstants(renderScene, frameIndex);

        var activeTexture = _activeTexture ?? _nullTexture.Texture;
        var shadowTex = _shadowAtlas ?? _nullTexture.Texture;

        if (_boundShadowAtlas[frameIndex] != shadowTex || _boundActiveTexture[frameIndex] != activeTexture)
        {
            _boundShadowAtlas[frameIndex] = shadowTex;
            _boundActiveTexture[frameIndex] = activeTexture;

            var texBg = _textureBindings[frameIndex];
            texBg.BeginUpdate();
            texBg.SrvTexture(0, shadowTex);
            texBg.SrvTexture(1, activeTexture);
            texBg.EndUpdate();

            var skinnedTexBg = _skinnedTextureBindings[frameIndex];
            skinnedTexBg.BeginUpdate();
            skinnedTexBg.SrvTexture(0, shadowTex);
            skinnedTexBg.SrvTexture(1, activeTexture);
            skinnedTexBg.EndUpdate();
        }

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

        RenderStaticBatches(cmd, frameIndex, renderScene);
        RenderSkinnedInstances(cmd, frameIndex, renderScene);

        cmd.EndRendering();
    }

    private void RenderStaticBatches(CommandList cmd, int frameIndex, RenderScene renderScene)
    {
        _shader.TryGetPipeline("static", out var staticPipeline);
        cmd.BindPipeline(staticPipeline);
        cmd.BindResourceGroup(_frameBindings[frameIndex]);
        cmd.BindResourceGroup(_textureBindings[frameIndex]);
        cmd.BindResourceGroup(_samplerBindings[frameIndex]);
        cmd.BindResourceGroup(_instanceBuffer.GetBindGroup(frameIndex));

        var batches = renderScene.StaticBatches;
        var instances = renderScene.StaticInstances;

        // Write ALL instances once upfront (more efficient than per-batch writes)
        for (var i = 0; i < instances.Length; i++)
        {
            var inst = instances[i];
            _instanceBuffer.WriteInstance(frameIndex, i, new GpuInstanceData
            {
                Model = inst.Transform,
                BaseColor = inst.BaseColor,
                Metallic = inst.Metallic,
                Roughness = inst.Roughness,
                AmbientOcclusion = inst.AmbientOcclusion,
                UseAlbedoTexture = inst.AlbedoTextureIndex >= 0 ? 1u : 0u
            });
        }

        // Render each batch using firstInstance offset
        foreach (var batch in batches)
        {
            // Look up the RuntimeMesh by MeshId (reinterpret cast - same layout)
            var meshId = batch.Mesh;
            var meshHandle = Unsafe.As<MeshId, RuntimeMeshHandle>(ref meshId);
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(meshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            cmd.BindVertexBuffer(runtimeMesh.VertexBuffer.View.Buffer, runtimeMesh.VertexBuffer.View.Offset,
                runtimeMesh.VertexBuffer.Stride, 0);
            cmd.BindIndexBuffer(runtimeMesh.IndexBuffer.View.Buffer, runtimeMesh.IndexBuffer.IndexType,
                runtimeMesh.IndexBuffer.View.Offset);
            // Use firstInstance = batch.StartIndex to reference the correct instances
            cmd.DrawIndexed(runtimeMesh.IndexBuffer.Count, (uint)batch.Count, 0, 0, (uint)batch.StartIndex);
        }
    }

    private unsafe void RenderSkinnedInstances(CommandList cmd, int frameIndex, RenderScene renderScene)
    {
        var skinnedInstances = renderScene.SkinnedInstances;
        if (skinnedInstances.Length == 0)
        {
            return;
        }

        var boneMatricesPtr = (Matrix4x4*)_boneMatricesMappedPtrs[frameIndex].ToPointer();
        var allBoneMatrices = renderScene.SkinnedBoneMatrices;

        _skinnedShader.TryGetPipeline("skinned", out var skinnedPipeline);
        cmd.BindPipeline(skinnedPipeline);
        cmd.BindResourceGroup(_skinnedFrameBindings[frameIndex]);
        cmd.BindResourceGroup(_skinnedTextureBindings[frameIndex]);
        cmd.BindResourceGroup(_skinnedSamplerBindings[frameIndex]);
        cmd.BindResourceGroup(_skinnedBoneBindings[frameIndex]);
        cmd.BindResourceGroup(_skinnedInstanceBuffer.GetBindGroup(frameIndex));

        foreach (var skinned in skinnedInstances)
        {
            var meshId = skinned.Mesh;
            var meshHandle = Unsafe.As<MeshId, RuntimeMeshHandle>(ref meshId);
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(meshHandle);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            // Copy bone matrices
            var boneCount = Math.Min(skinned.BoneCount, MaxBones);
            if (boneCount > 0 && skinned.BoneMatricesOffset + boneCount <= allBoneMatrices.Length)
            {
                for (var i = 0; i < boneCount; i++)
                {
                    boneMatricesPtr[i] = allBoneMatrices[skinned.BoneMatricesOffset + i];
                }
            }
            for (var i = boneCount; i < MaxBones; i++)
            {
                boneMatricesPtr[i] = Matrix4x4.Identity;
            }

            // Write instance
            _skinnedInstanceBuffer.WriteInstance(frameIndex, 0, new GpuInstanceData
            {
                Model = skinned.Transform,
                BaseColor = skinned.Material.BaseColor,
                Metallic = skinned.Material.Metallic,
                Roughness = skinned.Material.Roughness,
                AmbientOcclusion = skinned.Material.AmbientOcclusion,
                UseAlbedoTexture = skinned.Material.AlbedoTexture.IsValid ? 1u : 0u
            });

            cmd.BindVertexBuffer(runtimeMesh.VertexBuffer.View.Buffer, runtimeMesh.VertexBuffer.View.Offset,
                runtimeMesh.VertexBuffer.Stride, 0);
            cmd.BindIndexBuffer(runtimeMesh.IndexBuffer.View.Buffer, runtimeMesh.IndexBuffer.IndexType,
                runtimeMesh.IndexBuffer.View.Offset);
            cmd.DrawIndexed(runtimeMesh.IndexBuffer.Count, 1, 0, 0, 0);
        }
    }

    private unsafe void UpdateLightConstants(RenderScene renderScene, int frameIndex)
    {
        var ambientSkyColor = new Vector3(0.4f, 0.5f, 0.6f);
        var ambientGroundColor = new Vector3(0.2f, 0.18f, 0.15f);
        var ambientIntensity = 0.3f;

        var lights = stackalloc GpuLight[MaxLights];
        var shadows = stackalloc GpuShadowData[MaxShadowLights];
        var lightIndex = 0;

        var sceneLights = renderScene.Lights;
        foreach (var light in sceneLights)
        {
            if (lightIndex >= MaxLights)
            {
                break;
            }

            uint lightType = light.Type switch
            {
                LightType.Directional => LightTypeDirectional,
                LightType.Point => LightTypePoint,
                LightType.Spot => LightTypeSpot,
                _ => LightTypeDirectional
            };

            lights[lightIndex] = new GpuLight
            {
                PositionOrDirection = light.Type == LightType.Directional ? light.Direction : light.Position,
                Type = lightType,
                Color = light.Color,
                Intensity = light.Intensity,
                SpotDirection = light.Direction,
                Radius = light.Range,
                InnerConeAngle = MathF.Cos(light.SpotInnerAngle),
                OuterConeAngle = MathF.Cos(light.SpotOuterAngle),
                ShadowIndex = -1
            };
            lightIndex++;
        }

        var lightConstants = new LightConstants
        {
            AmbientSkyColor = ambientSkyColor,
            AmbientGroundColor = ambientGroundColor,
            AmbientIntensity = ambientIntensity,
            NumLights = (uint)lightIndex,
            NumShadows = 0
        };

        var lightPtr = (GpuLight*)lightConstants.Lights;
        var shadowPtr = (GpuShadowData*)lightConstants.Shadows;

        for (var i = 0; i < lightIndex; i++)
        {
            lightPtr[i] = lights[i];
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
    public struct GpuInstanceData
    {
        public Matrix4x4 Model;
        public Vector4 BaseColor;
        public float Metallic;
        public float Roughness;
        public float AmbientOcclusion;
        public uint UseAlbedoTexture;
    }
}
