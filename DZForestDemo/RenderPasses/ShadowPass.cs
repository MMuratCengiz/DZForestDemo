using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;
using ECS;
using ECS.Components;
using Graphics;
using Graphics.Batching;
using Graphics.RenderGraph;
using RuntimeAssets;
using Buffer = DenOfIz.Buffer;

namespace DZForestDemo.RenderPasses;

public sealed class ShadowPass : IDisposable
{
    public const int ShadowMapSize = 1024;
    public const int MaxShadowCastingLights = 4;
    public const int PointLightFaces = 6;
    public const int MaxInstancesPerFrame = 4096;

    private static readonly Matrix4x4 BiasMatrix = new(
        0.5f, 0.0f, 0.0f, 0.0f,
        0.0f, -0.5f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.5f, 0.5f, 0.0f, 1.0f
    );

    private readonly AssetResource _assets;
    private readonly GraphicsResource _ctx;
    private readonly MyRenderBatcher _batcher;
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameBatchData;

    // Pipeline variants for different mesh types
    private readonly InputLayout _geometryInputLayout;
    private readonly InputLayout _modelInputLayout;
    private readonly Pipeline _geometryPipeline;
    private readonly Pipeline _modelPipeline;

    // One buffer/bindgroup per light per frame to avoid overwrite during command recording
    private readonly Buffer[][] _lightMatrixBuffers;
    private readonly ResourceBindGroup[][] _lightMatrixBindGroups;
    private readonly IntPtr[][] _lightMatrixMappedPtrs;

    private readonly RootSignature _rootSignature;
    private readonly World _world;
    private bool _disposed;

    // Cached light data for building passes
    private readonly List<LightRenderInfo> _lightRenderInfos = [];
    private ResourceHandle _shadowAtlas;

    public ShadowPass(GraphicsResource ctx, AssetResource assets, World world, MyRenderBatcher batcher)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _batcher = batcher;

        var logicalDevice = ctx.LogicalDevice;

        // Create geometry pipeline (for built-in primitives)
        CreatePipeline(logicalDevice, "shadow_vs.hlsl", out _geometryInputLayout, out var rootSig, out _geometryPipeline);
        _rootSignature = rootSig!;

        // Create model pipeline (for imported models)
        CreatePipeline(logicalDevice, "shadow_vs_model.hlsl", out _modelInputLayout, out _, out _modelPipeline);

        var numFrames = (int)ctx.NumFrames;
        _lightMatrixBuffers = new Buffer[numFrames][];
        _lightMatrixBindGroups = new ResourceBindGroup[numFrames][];
        _lightMatrixMappedPtrs = new IntPtr[numFrames][];

        _perFrameBatchData = new Dictionary<RuntimeMeshHandle, BatchInstanceData>[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _lightMatrixBuffers[i] = new Buffer[MaxShadowCastingLights];
            _lightMatrixBindGroups[i] = new ResourceBindGroup[MaxShadowCastingLights];
            _lightMatrixMappedPtrs[i] = new IntPtr[MaxShadowCastingLights];

            for (var lightIdx = 0; lightIdx < MaxShadowCastingLights; lightIdx++)
            {
                _lightMatrixBuffers[i][lightIdx] = logicalDevice.CreateBuffer(new BufferDesc
                {
                    Usage = (uint)BufferUsageFlagBits.Uniform,
                    HeapType = HeapType.CpuGpu,
                    NumBytes = (ulong)Unsafe.SizeOf<LightMatrixConstants>(),
                    DebugName = StringView.Create($"ShadowLightMatrix_{i}_{lightIdx}")
                });
                _lightMatrixMappedPtrs[i][lightIdx] = _lightMatrixBuffers[i][lightIdx].MapMemory();

                _lightMatrixBindGroups[i][lightIdx] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
                {
                    RootSignature = _rootSignature,
                    RegisterSpace = 0
                });
                _lightMatrixBindGroups[i][lightIdx].BeginUpdate();
                _lightMatrixBindGroups[i][lightIdx].Cbv(0, _lightMatrixBuffers[i][lightIdx]);
                _lightMatrixBindGroups[i][lightIdx].EndUpdate();
            }

            _perFrameBatchData[i] = new Dictionary<RuntimeMeshHandle, BatchInstanceData>();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        for (var i = 0; i < _lightMatrixBuffers.Length; i++)
        {
            for (var lightIdx = 0; lightIdx < MaxShadowCastingLights; lightIdx++)
            {
                _lightMatrixBuffers[i][lightIdx].UnmapMemory();
                _lightMatrixBindGroups[i][lightIdx].Dispose();
                _lightMatrixBuffers[i][lightIdx].Dispose();
            }

            foreach (var batchData in _perFrameBatchData[i].Values)
            {
                batchData.Dispose();
            }
        }

        _geometryPipeline.Dispose();
        _modelPipeline.Dispose();
        _rootSignature.Dispose();
        _geometryInputLayout.Dispose();
        _modelInputLayout.Dispose();

        GC.SuppressFinalize(this);
    }

    private void CreatePipeline(LogicalDevice logicalDevice, string vsFilename, out InputLayout inputLayout,
        out RootSignature? rootSignature, out Pipeline pipeline)
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
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        inputLayout = logicalDevice.CreateInputLayout(reflection.InputLayout);

        // Create root signature only if not already created (all variants share the same root signature)
        rootSignature = _rootSignature == null
            ? logicalDevice.CreateRootSignature(reflection.RootSignature)
            : null;

        pipeline = logicalDevice.CreatePipeline(new PipelineDesc
        {
            InputLayout = inputLayout,
            RootSignature = _rootSignature ?? rootSignature!,
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
                Rasterization =
                {
                    DepthBias = 2000,
                    SlopeScaledDepthBias = 2.0f
                },
                RenderTargets = RenderTargetDescArray.Create([]),
                DepthStencilAttachmentFormat = Format.D32Float
            }
        });
    }

    public ResourceHandle CreateShadowAtlas(RenderGraph renderGraph)
    {
        var atlasWidth = (uint)(ShadowMapSize * 2);
        var atlasHeight = (uint)(ShadowMapSize * 2);

        _shadowAtlas = renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Aspect = TextureAspect.Depth,
            Width = atlasWidth,
            Height = atlasHeight,
            Depth = 1,
            Format = Format.D32Float,
            MipLevels = 1,
            ArraySize = 1,
            Usage = (uint)(TextureUsageFlagBits.TextureBinding | TextureUsageFlagBits.RenderAttachment),
            DebugName = "ShadowAtlas"
        });

        return _shadowAtlas;
    }

    /// <summary>
    /// Adds separate render graph passes for each shadow-casting light.
    /// Each light gets its own pass with dedicated buffers and bind groups.
    /// </summary>
    public void AddPasses(
        RenderGraph renderGraph,
        ResourceHandle shadowAtlas,
        List<ShadowData> shadowDataOut,
        Vector3 sceneCenter,
        float sceneRadius)
    {
        _lightRenderInfos.Clear();
        shadowDataOut.Clear();
        var shadowIndex = 0;

        // Collect all shadow-casting lights and their matrices
        foreach (var (entity, light) in _world.Query<DirectionalLight>())
        {
            if (!light.CastShadows || shadowIndex >= MaxShadowCastingLights)
            {
                continue;
            }

            var (renderMatrix, sampleMatrix) =
                CalculateDirectionalLightMatrices(light.Direction, sceneCenter, sceneRadius);
            var atlasOffset = GetAtlasOffset(shadowIndex);

            _lightRenderInfos.Add(new LightRenderInfo
            {
                RenderMatrix = renderMatrix,
                AtlasOffset = atlasOffset,
                ShadowIndex = shadowIndex,
                LightType = LightType.Directional
            });

            shadowDataOut.Add(new ShadowData
            {
                LightViewProjection = sampleMatrix,
                AtlasScaleOffset = new Vector4(0.5f, 0.5f, atlasOffset.X / (ShadowMapSize * 2f),
                    atlasOffset.Y / (ShadowMapSize * 2f)),
                ShadowIndex = shadowIndex,
                Bias = 0.005f,
                NormalBias = 0.02f
            });

            shadowIndex++;
        }

        foreach (var (entity, light, transform) in _world.Query<PointLight, Transform>())
        {
            if (shadowIndex >= MaxShadowCastingLights)
            {
                continue;
            }

            var lightViewProj = CalculatePointLightMatrix(transform.Position, sceneCenter, light.Radius);
            var atlasOffset = GetAtlasOffset(shadowIndex);

            _lightRenderInfos.Add(new LightRenderInfo
            {
                RenderMatrix = lightViewProj,
                AtlasOffset = atlasOffset,
                ShadowIndex = shadowIndex,
                LightType = LightType.Point
            });

            shadowDataOut.Add(new ShadowData
            {
                LightViewProjection = lightViewProj * BiasMatrix,
                AtlasScaleOffset = new Vector4(0.5f, 0.5f, atlasOffset.X / (ShadowMapSize * 2f),
                    atlasOffset.Y / (ShadowMapSize * 2f)),
                ShadowIndex = shadowIndex,
                Bias = 0.01f,
                NormalBias = 0.05f
            });

            shadowIndex++;
        }

        foreach (var (entity, light, transform) in _world.Query<SpotLight, Transform>())
        {
            if (shadowIndex >= MaxShadowCastingLights)
            {
                continue;
            }

            var lightViewProj =
                CalculateSpotLightMatrix(transform.Position, light.Direction, light.OuterConeAngle, light.Radius);
            var atlasOffset = GetAtlasOffset(shadowIndex);

            _lightRenderInfos.Add(new LightRenderInfo
            {
                RenderMatrix = lightViewProj,
                AtlasOffset = atlasOffset,
                ShadowIndex = shadowIndex,
                LightType = LightType.Spot
            });

            shadowDataOut.Add(new ShadowData
            {
                LightViewProjection = lightViewProj * BiasMatrix,
                AtlasScaleOffset = new Vector4(0.5f, 0.5f, atlasOffset.X / (ShadowMapSize * 2f),
                    atlasOffset.Y / (ShadowMapSize * 2f)),
                ShadowIndex = shadowIndex,
                Bias = 0.005f,
                NormalBias = 0.02f
            });

            shadowIndex++;
        }

        // Add a clear pass first
        renderGraph.AddPass("Shadow_Clear",
            (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
            {
                builder.WriteTexture(shadowAtlas, (uint)ResourceUsageFlagBits.DepthWrite);
                builder.HasSideEffects();
            },
            (ref RenderPassExecuteContext ctx) =>
            {
                ExecuteClearPass(ref ctx, shadowAtlas);
            });

        // Add separate pass for each light
        // Use ReadWriteTexture to create proper dependency chain - each pass preserves
        // existing content (renders to different atlas region) so depends on previous writes
        for (var i = 0; i < _lightRenderInfos.Count; i++)
        {
            var lightInfo = _lightRenderInfos[i];
            var passName = $"Shadow_{lightInfo.LightType}_{lightInfo.ShadowIndex}";

            renderGraph.AddPass(passName,
                (ref RenderPassSetupContext ctx, ref PassBuilder builder) =>
                {
                    builder.ReadWriteTexture(shadowAtlas, (uint)ResourceUsageFlagBits.DepthWrite);
                    builder.HasSideEffects();
                },
                (ref RenderPassExecuteContext ctx) =>
                {
                    ExecuteLightPass(ref ctx, shadowAtlas, lightInfo);
                });
        }
    }

    private void ExecuteClearPass(ref RenderPassExecuteContext ctx, ResourceHandle shadowAtlas)
    {
        var cmd = ctx.CommandList;
        var atlas = ctx.GetTexture(shadowAtlas);

        ctx.ResourceTracking.TransitionTexture(cmd, atlas,
            (uint)ResourceUsageFlagBits.DepthWrite, QueueType.Graphics);

        var clearDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.Create([]),
            DepthAttachment = new RenderingAttachmentDesc
            {
                Resource = atlas,
                LoadOp = LoadOp.Clear,
                ClearDepthStencil = new Float2 { X = 1.0f, Y = 0.0f }
            },
            NumLayers = 1,
            RenderAreaWidth = ShadowMapSize * 2,
            RenderAreaHeight = ShadowMapSize * 2
        };
        cmd.BeginRendering(clearDesc);
        cmd.EndRendering();
    }

    private unsafe void ExecuteLightPass(ref RenderPassExecuteContext ctx, ResourceHandle shadowAtlas, LightRenderInfo lightInfo)
    {
        var cmd = ctx.CommandList;
        var atlas = ctx.GetTexture(shadowAtlas);
        var frameIndex = (int)ctx.FrameIndex;

        ctx.ResourceTracking.TransitionTexture(cmd, atlas,
            (uint)ResourceUsageFlagBits.DepthWrite, QueueType.Graphics);

        var batchDataDict = _perFrameBatchData[frameIndex];
        var staticBatcher = _batcher.StaticBatcher;
        var allInstances = staticBatcher.Instances;

        foreach (var batch in staticBatcher.Batches)
        {
            var batchData = GetOrCreateBatchData(batchDataDict, batch.Key, batch.Count, frameIndex);
            var instancePtr = (ShadowInstanceData*)batchData.MappedPtr.ToPointer();

            for (var i = 0; i < batch.Count; i++)
            {
                instancePtr[i] = new ShadowInstanceData { Model = allInstances[batch.StartIndex + i].WorldMatrix };
            }
        }

        RenderShadowMap(ref ctx, atlas, lightInfo.RenderMatrix, lightInfo.AtlasOffset, frameIndex, lightInfo.ShadowIndex, batchDataDict, staticBatcher);
    }

    private unsafe void RenderShadowMap(
        ref RenderPassExecuteContext ctx,
        Texture atlas,
        Matrix4x4 lightViewProj,
        Vector2 atlasOffset,
        int frameIndex,
        int shadowIndex,
        Dictionary<RuntimeMeshHandle, BatchInstanceData> batchDataDict,
        RenderBatcher<RuntimeMeshHandle, StaticInstance> staticBatcher)
    {
        var cmd = ctx.CommandList;

        var lightMatrixConstants = new LightMatrixConstants { LightViewProjection = lightViewProj };
        Unsafe.Write(_lightMatrixMappedPtrs[frameIndex][shadowIndex].ToPointer(), lightMatrixConstants);

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.Create([]),
            DepthAttachment = new RenderingAttachmentDesc
            {
                Resource = atlas,
                LoadOp = LoadOp.Load
            },
            NumLayers = 1,
            RenderAreaWidth = ShadowMapSize * 2,
            RenderAreaHeight = ShadowMapSize * 2
        };

        cmd.BeginRendering(renderingDesc);
        cmd.BindViewport(atlasOffset.X, atlasOffset.Y, ShadowMapSize, ShadowMapSize);
        cmd.BindScissorRect((int)atlasOffset.X, (int)atlasOffset.Y, ShadowMapSize, ShadowMapSize);
        cmd.BindResourceGroup(_lightMatrixBindGroups[frameIndex][shadowIndex]);

        var currentMeshType = (MeshType)255;

        foreach (var batch in staticBatcher.Batches)
        {
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(batch.Key);
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

            var batchData = batchDataDict[batch.Key];
            cmd.BindResourceGroup(batchData.BindGroup);

            var vb = runtimeMesh.VertexBuffer;
            var ib = runtimeMesh.IndexBuffer;

            cmd.BindVertexBuffer(vb.View.GetBuffer(), (uint)vb.View.Offset, vb.Stride, 0);
            cmd.BindIndexBuffer(ib.View.GetBuffer(), ib.IndexType, ib.View.Offset);
            cmd.DrawIndexed(ib.Count, (uint)batch.Count, 0, 0, 0);
        }

        cmd.EndRendering();
    }

    private static Vector2 GetAtlasOffset(int shadowIndex)
    {
        var x = shadowIndex % 2 * ShadowMapSize;
        var y = shadowIndex / 2 * ShadowMapSize;
        return new Vector2(x, y);
    }

    private static (Matrix4x4 renderMatrix, Matrix4x4 sampleMatrix) CalculateDirectionalLightMatrices(Vector3 direction,
        Vector3 sceneCenter, float sceneRadius)
    {
        var lightDir = Vector3.Normalize(direction);
        var lightDistance = sceneRadius * 1.5f;
        var lightPos = sceneCenter - lightDir * lightDistance;
        var up = ComputeStableUpVector(lightDir);

        var view = Matrix4x4.CreateLookAtLeftHanded(lightPos, sceneCenter, up);
        var size = sceneRadius * 2.0f;
        var nearPlane = MathF.Max(0.1f, lightDistance - sceneRadius);
        var farPlane = lightDistance + sceneRadius;
        var proj = Matrix4x4.CreateOrthographicLeftHanded(size, size, nearPlane, farPlane);

        var viewProj = view * proj;
        return (viewProj, viewProj * BiasMatrix);
    }

    private static Matrix4x4 CalculatePointLightMatrix(Vector3 position, Vector3 sceneCenter, float radius)
    {
        var forward = Vector3.Normalize(sceneCenter - position);
        var up = ComputeStableUpVector(forward);
        var view = Matrix4x4.CreateLookAtLeftHanded(position, sceneCenter, up);
        var nearPlane = 0.01f;
        var farPlane = MathF.Max(radius * 2f, 1.0f);
        var proj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 2f, 1f, nearPlane, farPlane);
        return view * proj;
    }

    private static Matrix4x4 CalculateSpotLightMatrix(Vector3 position, Vector3 direction, float outerAngle,
        float radius)
    {
        var forward = Vector3.Normalize(direction);
        var target = position + forward;
        var up = ComputeStableUpVector(forward);
        var view = Matrix4x4.CreateLookAtLeftHanded(position, target, up);
        var fov = MathF.Min(outerAngle * 2f + 0.1f, MathF.PI * 0.99f);
        var nearPlane = 0.01f;
        var farPlane = MathF.Max(radius, 1.0f);
        var proj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fov, 1f, nearPlane, farPlane);
        return view * proj;
    }

    private static Vector3 ComputeStableUpVector(Vector3 forward)
    {
        if (MathF.Abs(forward.Y) > 0.999f)
        {
            return Vector3.UnitZ;
        }
        var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
        return Vector3.Cross(forward, right);
    }

    private BatchInstanceData GetOrCreateBatchData(Dictionary<RuntimeMeshHandle, BatchInstanceData> dict, RuntimeMeshHandle meshHandle, int requiredCapacity, int frameIndex)
    {
        if (dict.TryGetValue(meshHandle, out var existing) && existing.Capacity >= requiredCapacity)
        {
            return existing;
        }

        existing?.Dispose();

        var capacity = Math.Max(requiredCapacity, 64);
        var stride = (ulong)Unsafe.SizeOf<ShadowInstanceData>();
        var buffer = _ctx.LogicalDevice.CreateBuffer(new BufferDesc
        {
            HeapType = HeapType.CpuGpu,
            NumBytes = stride * (ulong)capacity,
            Usage = (int)BufferUsageFlagBits.Uniform,
            DebugName = StringView.Create($"ShadowBatchInstance_{frameIndex}_{meshHandle.Index}")
        });

        var bindGroup = _ctx.LogicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _rootSignature,
            RegisterSpace = 1
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

    [StructLayout(LayoutKind.Sequential)]
    private struct LightMatrixConstants
    {
        public Matrix4x4 LightViewProjection;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShadowInstanceData
    {
        public Matrix4x4 Model;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShadowData
    {
        public Matrix4x4 LightViewProjection;
        public Vector4 AtlasScaleOffset;
        public int ShadowIndex;
        public float Bias;
        public float NormalBias;
        public float Padding;
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

    private enum LightType
    {
        Directional,
        Point,
        Spot
    }

    private struct LightRenderInfo
    {
        public Matrix4x4 RenderMatrix;
        public Vector2 AtlasOffset;
        public int ShadowIndex;
        public LightType LightType;
    }
}
