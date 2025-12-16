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
    public const int PointLightFaces = 6;
    public const int MaxInstancesPerFrame = 4096;

    private static readonly Matrix4x4 BiasMatrix = new(
        0.5f, 0.0f, 0.0f, 0.0f,
        0.0f, -0.5f, 0.0f, 0.0f,
        0.0f, 0.0f, 1.0f, 0.0f,
        0.5f, 0.5f, 0.0f, 1.0f
    );

    private readonly AssetContext _assets;
    private readonly GraphicsContext _ctx;
    private readonly RenderBatcher _batcher;
    private readonly Dictionary<RuntimeMeshHandle, BatchInstanceData>[] _perFrameBatchData;

    private readonly InputLayout _inputLayout;

    private readonly Buffer[] _lightMatrixBuffers;
    private readonly ResourceBindGroup[] _lightMatrixBindGroups;
    private readonly IntPtr[] _lightMatrixMappedPtrs;

    private readonly Pipeline _pipeline;
    private readonly RootSignature _rootSignature;
    private readonly World _world;
    private bool _disposed;

    private ResourceHandle _shadowAtlas;

    public ShadowPass(GraphicsContext ctx, AssetContext assets, World world, RenderBatcher batcher)
    {
        _ctx = ctx;
        _assets = assets;
        _world = world;
        _batcher = batcher;

        var logicalDevice = ctx.LogicalDevice;

        CreatePipeline(logicalDevice, out _inputLayout, out _rootSignature, out _pipeline);

        // Create per-frame light matrix buffers and bind groups
        var numFrames = (int)ctx.NumFrames;
        _lightMatrixBuffers = new Buffer[numFrames];
        _lightMatrixBindGroups = new ResourceBindGroup[numFrames];
        _lightMatrixMappedPtrs = new IntPtr[numFrames];

        _perFrameBatchData = new Dictionary<RuntimeMeshHandle, BatchInstanceData>[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _lightMatrixBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)Unsafe.SizeOf<LightMatrixConstants>(),
                DebugName = StringView.Create($"ShadowLightMatrix_{i}")
            });
            _lightMatrixMappedPtrs[i] = _lightMatrixBuffers[i].MapMemory();

            _lightMatrixBindGroups[i] = logicalDevice.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = _rootSignature,
                RegisterSpace = 0
            });
            _lightMatrixBindGroups[i].BeginUpdate();
            _lightMatrixBindGroups[i].Cbv(0, _lightMatrixBuffers[i]);
            _lightMatrixBindGroups[i].EndUpdate();

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
            _lightMatrixBuffers[i].UnmapMemory();
            _lightMatrixBindGroups[i].Dispose();
            _lightMatrixBuffers[i].Dispose();

            foreach (var batchData in _perFrameBatchData[i].Values)
            {
                batchData.Dispose();
            }
        }

        _pipeline.Dispose();
        _rootSignature.Dispose();
        _inputLayout.Dispose();

        GC.SuppressFinalize(this);
    }

    private void CreatePipeline(LogicalDevice logicalDevice, out InputLayout inputLayout,
        out RootSignature rootSignature, out Pipeline pipeline)
    {
        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load("shadow_vs.hlsl");

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(vsSource)),
                    Stage = ShaderStage.Vertex
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
            Usages = (uint)(ResourceUsageFlagBits.DepthWrite | ResourceUsageFlagBits.ShaderResource),
            Descriptor = (uint)(ResourceDescriptorFlagBits.DepthStencil | ResourceDescriptorFlagBits.Texture),
            DebugName = "ShadowAtlas"
        });

        return _shadowAtlas;
    }

    public unsafe void Execute(
        ref RenderPassExecuteContext ctx,
        ResourceHandle shadowAtlas,
        List<ShadowData> shadowDataOut,
        Vector3 sceneCenter,
        float sceneRadius)
    {
        var cmd = ctx.CommandList;
        var atlas = ctx.GetTexture(shadowAtlas);
        var frameIndex = (int)ctx.FrameIndex;

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

        var batchDataDict = _perFrameBatchData[frameIndex];
        var allInstances = _batcher.AllInstances;

        foreach (var batch in _batcher.Batches)
        {
            var batchData = GetOrCreateBatchData(batchDataDict, batch.MeshHandle, batch.InstanceCount, frameIndex);
            var instancePtr = (ShadowInstanceData*)batchData.MappedPtr.ToPointer();

            for (var i = 0; i < batch.InstanceCount; i++)
            {
                instancePtr[i] = new ShadowInstanceData { Model = allInstances[batch.StartInstance + i].WorldMatrix };
            }
        }

        shadowDataOut.Clear();
        var shadowIndex = 0;

        foreach (var (entity, light) in _world.Query<DirectionalLight>())
        {
            if (!light.CastShadows || shadowIndex >= MaxShadowCastingLights)
            {
                continue;
            }

            var (renderMatrix, sampleMatrix) =
                CalculateDirectionalLightMatrices(light.Direction, sceneCenter, sceneRadius);
            var atlasOffset = GetAtlasOffset(shadowIndex);

            RenderShadowMap(ref ctx, atlas, renderMatrix, atlasOffset, frameIndex, batchDataDict);

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

            var lightViewProj = CalculatePointLightMatrix(transform.Position, 0);
            var atlasOffset = GetAtlasOffset(shadowIndex);

            RenderShadowMap(ref ctx, atlas, lightViewProj, atlasOffset, frameIndex, batchDataDict);

            shadowDataOut.Add(new ShadowData
            {
                LightViewProjection = lightViewProj,
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

            RenderShadowMap(ref ctx, atlas, lightViewProj, atlasOffset, frameIndex, batchDataDict);

            shadowDataOut.Add(new ShadowData
            {
                LightViewProjection = lightViewProj,
                AtlasScaleOffset = new Vector4(0.5f, 0.5f, atlasOffset.X / (ShadowMapSize * 2f),
                    atlasOffset.Y / (ShadowMapSize * 2f)),
                ShadowIndex = shadowIndex,
                Bias = 0.005f,
                NormalBias = 0.02f
            });

            shadowIndex++;
        }
    }

    private unsafe void RenderShadowMap(ref RenderPassExecuteContext ctx, Texture atlas, Matrix4x4 lightViewProj,
        Vector2 atlasOffset, int frameIndex, Dictionary<RuntimeMeshHandle, BatchInstanceData> batchDataDict)
    {
        var cmd = ctx.CommandList;

        var lightMatrixConstants = new LightMatrixConstants { LightViewProjection = lightViewProj };
        Unsafe.Write(_lightMatrixMappedPtrs[frameIndex].ToPointer(), lightMatrixConstants);

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
        cmd.BindPipeline(_pipeline);
        cmd.BindViewport(atlasOffset.X, atlasOffset.Y, ShadowMapSize, ShadowMapSize);
        cmd.BindScissorRect((int)atlasOffset.X, (int)atlasOffset.Y, ShadowMapSize, ShadowMapSize);
        cmd.BindResourceGroup(_lightMatrixBindGroups[frameIndex]);

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
        var up = MathF.Abs(lightDir.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;

        var view = Matrix4x4.CreateLookAtLeftHanded(lightPos, sceneCenter, up);
        var size = sceneRadius * 2.0f;
        var nearPlane = MathF.Max(0.1f, lightDistance - sceneRadius);
        var farPlane = lightDistance + sceneRadius;
        var proj = Matrix4x4.CreateOrthographicLeftHanded(size, size, nearPlane, farPlane);

        var viewProj = view * proj;
        return (viewProj, viewProj * BiasMatrix);
    }

    private static Matrix4x4 CalculatePointLightMatrix(Vector3 position, int face)
    {
        var targets = new[]
        {
            position + Vector3.UnitX,
            position - Vector3.UnitX,
            position + Vector3.UnitY,
            position - Vector3.UnitY,
            position + Vector3.UnitZ,
            position - Vector3.UnitZ
        };

        var ups = new[]
        {
            Vector3.UnitY, Vector3.UnitY,
            Vector3.UnitZ, -Vector3.UnitZ,
            Vector3.UnitY, Vector3.UnitY
        };

        var view = Matrix4x4.CreateLookAtLeftHanded(position, targets[face], ups[face]);
        var proj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(MathF.PI / 2f, 1f, 0.1f, 50f);

        return view * proj;
    }

    private static Matrix4x4 CalculateSpotLightMatrix(Vector3 position, Vector3 direction, float outerAngle,
        float radius)
    {
        var target = position + Vector3.Normalize(direction);
        var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

        var view = Matrix4x4.CreateLookAtLeftHanded(position, target, up);
        var proj = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(outerAngle * 2f, 1f, 0.1f, radius);

        return view * proj;
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
            Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
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
}
