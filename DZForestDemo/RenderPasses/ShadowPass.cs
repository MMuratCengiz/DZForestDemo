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

public sealed class ShadowPass : IDisposable
{
    public const int ShadowMapSize = 1024;
    public const int MaxShadowCastingLights = 4;

    private static readonly Matrix4x4 BiasMatrix = new(
        0.5f, 0.0f, 0.0f, 0.0f,
        0.0f, -0.5f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.5f, 0.5f, 0.0f, 1.0f
    );

    private readonly AssetResource _assets;
    private readonly GraphicsResource _ctx;
    private readonly MyRenderBatcher _batcher;
    private readonly RgCommandList _rgCommandList;
    private readonly World _world;

    // Shader with variants
    private readonly Shader _shader;

    // Per-frame buffers
    private readonly Buffer[][] _lightMatrixBuffers;
    private readonly IntPtr[][] _lightMatrixMappedPtrs;
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameBatchData;

    // Cached light data for building passes
    private readonly List<LightRenderInfo> _lightRenderInfos = [];

    private bool _disposed;

    public ShadowPass(GraphicsResource ctx, AssetResource assets, World world, MyRenderBatcher batcher, RgCommandList rgCommandList)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _batcher = batcher;
        _rgCommandList = rgCommandList;

        var logicalDevice = ctx.LogicalDevice;
        var numFrames = (int)ctx.NumFrames;

        // Create shader with variants
        _shader = CreateShader(logicalDevice);

        // Create per-frame buffers
        _lightMatrixBuffers = new Buffer[numFrames][];
        _lightMatrixMappedPtrs = new IntPtr[numFrames][];
        _perFrameBatchData = new Dictionary<RuntimeMeshHandle, BatchInstanceData>[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _lightMatrixBuffers[i] = new Buffer[MaxShadowCastingLights];
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
            }

            _perFrameBatchData[i] = new Dictionary<RuntimeMeshHandle, BatchInstanceData>();
        }
    }

    private Shader CreateShader(LogicalDevice logicalDevice)
    {
        // Build root signature with bindings
        // space0 (PerFrame): LightMatrix
        // space3 (PerDraw): Instances
        var rootSignature = new ShaderRootSignature.Builder(logicalDevice)
            .AddBinding("LightMatrix", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 0,
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                Stages = (uint)ShaderStageFlagBits.Vertex,
                ArraySize = 1
            })
            .AddBinding("Instances", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = 3,
                Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
                Stages = (uint)ShaderStageFlagBits.Vertex,
                ArraySize = 1
            })
            .Build();

        var shader = new Shader(rootSignature);

        // Create static mesh variant
        AddVariant(shader, logicalDevice, rootSignature, "shadow_vs_model.hlsl", "static");

        return shader;
    }

    private void AddVariant(Shader shader, LogicalDevice logicalDevice, ShaderRootSignature rootSignature,
        string vsFilename, string variantName)
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
                Rasterization =
                {
                    DepthBias = 2000,
                    SlopeScaledDepthBias = 2.0f
                },
                RenderTargets = RenderTargetDescArray.Create([]),
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

        for (var i = 0; i < _lightMatrixBuffers.Length; i++)
        {
            for (var lightIdx = 0; lightIdx < MaxShadowCastingLights; lightIdx++)
            {
                _lightMatrixBuffers[i][lightIdx].UnmapMemory();
                _lightMatrixBuffers[i][lightIdx].Dispose();
            }

            foreach (var batchData in _perFrameBatchData[i].Values)
            {
                batchData.Dispose();
            }
        }

        _shader.Dispose();
    }

    public ResourceHandle CreateShadowAtlas(RenderGraph renderGraph)
    {
        var atlasWidth = (uint)(ShadowMapSize * 2);
        var atlasHeight = (uint)(ShadowMapSize * 2);
        return renderGraph.CreateTransientTexture(new TransientTextureDesc
        {
            Width = atlasWidth,
            Height = atlasHeight,
            Depth = 1,
            Format = Format.D32Float,
            MipLevels = 1,
            ArraySize = 1,
            Usage = (uint)(TextureUsageFlagBits.TextureBinding | TextureUsageFlagBits.RenderAttachment),
            DebugName = "ShadowAtlas",
            ClearDepthStencilHint = new Vector2 { X = 1.0f, Y = 0.0f }
        });
    }

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
                ClearDepthStencil = new Vector2 { X = 1.0f, Y = 0.0f }
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

        // Update instance data for all batches
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

        // Update light matrix
        var lightMatrixConstants = new LightMatrixConstants { LightViewProjection = lightInfo.RenderMatrix };
        Unsafe.Write(_lightMatrixMappedPtrs[frameIndex][lightInfo.ShadowIndex].ToPointer(), lightMatrixConstants);

        // Begin RgCommandList pass
        _rgCommandList.Begin(cmd, frameIndex);

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

        _rgCommandList.BeginRendering(renderingDesc);
        _rgCommandList.BindViewport(lightInfo.AtlasOffset.X, lightInfo.AtlasOffset.Y, ShadowMapSize, ShadowMapSize);
        _rgCommandList.BindScissorRect(lightInfo.AtlasOffset.X, lightInfo.AtlasOffset.Y, ShadowMapSize, ShadowMapSize);

        // Render all batches
        foreach (var batch in staticBatcher.Batches)
        {
            ref readonly var runtimeMesh = ref _assets.GetMeshRef(batch.Key);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in runtimeMesh)))
            {
                continue;
            }

            _rgCommandList.SetShader(_shader, "static");

            _rgCommandList.SetBuffer("LightMatrix", _lightMatrixBuffers[frameIndex][lightInfo.ShadowIndex]);

            var batchData = batchDataDict[batch.Key];
            _rgCommandList.SetBuffer("Instances", batchData.Buffer);

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

        _rgCommandList.End();
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
        var stride = (ulong)Unsafe.SizeOf<ShadowInstanceData>();
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
            DebugName = StringView.Create($"ShadowBatchInstance_{frameIndex}_{meshHandle.Index}")
        });

        var batchData = new BatchInstanceData(buffer, buffer.MapMemory(), capacity);
        dict[meshHandle] = batchData;
        return batchData;
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
