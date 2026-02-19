using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Buffers;
using NiziKit.Particles;

namespace NiziKit.Graphics.Renderer.Forward;

public class ParticleSystemPass : IDisposable
{
    public const int ParticlesPerSystem = 2048;
    public const int MaxSystems = 8;
    private const int ThreadGroupSize = 64;
    private const int TotalParticles = ParticlesPerSystem * MaxSystems;

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemParams
    {
        public Vector3 EmitterPosition;   // 12
        public float _Pad;                // 4  → 16
        public uint EmitStartIndex;       // 4
        public uint EmitCount;            // 4
        public float StartLifetimeMin;    // 4
        public float StartLifetimeMax;    // 4  → 32
        public float StartSpeedMin;       // 4
        public float StartSpeedMax;       // 4
        public float StartSizeMin;        // 4
        public float StartSizeMax;        // 4  → 48
        public float GravityModifier;     // 4
        public float Drag;                // 4
        public float EmitterRadius;       // 4
        public float EmitterAngle;        // 4  → 64
        public Vector4 StartColor;        // 16 → 80
        public Vector4 EndColor;          // 16 → 96
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ParticleConstants
    {
        public Matrix4x4 ViewProjection;  // 64 bytes  (offset 0)
        public Vector3 CameraPosition;    // 12 bytes
        public float DeltaTime;           // 4 bytes   → 80
        public Vector3 CameraRight;       // 12 bytes
        public float TotalTime;           // 4 bytes   → 96
        public Vector3 CameraUp;          // 12 bytes
        public uint ParticlesPerSystem;   // 4 bytes   → 112
        public uint NumActiveSystems;     // 4 bytes
        public uint _Pad0;               // 4 bytes
        public uint _Pad1;               // 4 bytes
        public uint _Pad2;               // 4 bytes   → 128
        // Systems[8] = 8 * 96 = 768     → total 896
        public SystemParams System0;
        public SystemParams System1;
        public SystemParams System2;
        public SystemParams System3;
        public SystemParams System4;
        public SystemParams System5;
        public SystemParams System6;
        public SystemParams System7;
    }

    // Compute resources
    private readonly ShaderProgram _computeProgram;
    private readonly RootSignature _computeRootSignature;
    private readonly Pipeline _computePipeline;
    private readonly BindGroupLayout _computeBindGroupLayout;
    private readonly BindGroup[] _computeBindGroups;

    // Raster resources
    private readonly ShaderProgram _rasterProgram;
    private readonly RootSignature _rasterRootSignature;
    private readonly InputLayout _rasterInputLayout;
    private readonly Pipeline _rasterPipeline;
    private readonly BindGroupLayout _rasterBindGroupLayout;
    private readonly BindGroup[] _rasterBindGroups;

    // Single flat buffers for all particle systems
    private readonly StorageBuffer<Particle> _particles;
    private readonly StorageBuffer<ResolvedParticle> _resolved;
    private readonly MappedBuffer<ParticleConstants> _constantBuffer;

    public ParticleSystemPass()
    {
        var numFrames = (int)GraphicsContext.NumFrames;

        // Single flat buffers: MaxSystems * ParticlesPerSystem particles
        _particles = new StorageBuffer<Particle>(TotalParticles, cycled: false, debugName: "ParticlePool");
        _resolved = new StorageBuffer<ResolvedParticle>(TotalParticles, cycled: false, debugName: "ResolvedParticles");
        _constantBuffer = new MappedBuffer<ParticleConstants>(cycled: true, debugName: "ParticleConstants");

        // --- Compute pipeline ---
        _computeProgram = BuiltinShaderProgram.Load("ParticleSystemComputeShader")
                          ?? throw new InvalidOperationException("ParticleSystemComputeShader not found");

        var computeReflection = _computeProgram.Reflect();
        var computeBindGroupLayoutDescs = computeReflection.BindGroupLayouts.ToArray();
        _computeBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(computeBindGroupLayoutDescs[0]);

        var computeRootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create([_computeBindGroupLayout])
        };
        _computeRootSignature = GraphicsContext.Device.CreateRootSignature(computeRootSigDesc);

        var computePipelineDesc = new PipelineDesc
        {
            RootSignature = _computeRootSignature,
            ShaderProgram = _computeProgram,
            BindPoint = BindPoint.Compute
        };
        _computePipeline = GraphicsContext.Device.CreatePipeline(computePipelineDesc);

        // --- Raster pipeline ---
        _rasterProgram = BuiltinShaderProgram.Load("ParticleSystemRasterShader")
                         ?? throw new InvalidOperationException("ParticleSystemRasterShader not found");

        var rasterReflection = _rasterProgram.Reflect();
        var rasterBindGroupLayoutDescs = rasterReflection.BindGroupLayouts.ToArray();
        _rasterBindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(rasterBindGroupLayoutDescs[0]);

        var rasterRootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create([_rasterBindGroupLayout])
        };
        _rasterRootSignature = GraphicsContext.Device.CreateRootSignature(rasterRootSigDesc);
        _rasterInputLayout = GraphicsContext.Device.CreateInputLayout(rasterReflection.InputLayout);

        var blendDesc = new BlendDesc
        {
            Enable = true,
            SrcBlend = Blend.One,
            DstBlend = Blend.One,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DstBlendAlpha = Blend.One,
            BlendOpAlpha = BlendOp.Add,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = GraphicsContext.BackBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var rasterPipelineDesc = new PipelineDesc
        {
            RootSignature = _rasterRootSignature,
            InputLayout = _rasterInputLayout,
            ShaderProgram = _rasterProgram,
            BindPoint = BindPoint.Graphics,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                DepthTest = new DepthTest
                {
                    Enable = true,
                    CompareOp = CompareOp.LessOrEqual,
                    Write = false
                },
                RenderTargets = renderTargets,
                DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat
            }
        };
        _rasterPipeline = GraphicsContext.Device.CreatePipeline(rasterPipelineDesc);

        // --- Bind groups (one set, shared across all systems) ---
        _computeBindGroups = new BindGroup[numFrames];
        _rasterBindGroups = new BindGroup[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _computeBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _computeBindGroupLayout
            });

            _rasterBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _rasterBindGroupLayout
            });
        }
    }

    /// <summary>
    /// Execute all particle systems in a single compute dispatch + single draw call.
    /// </summary>
    public void Execute(
        RenderFrame frame,
        Resources.CycledTexture sceneColor,
        Resources.CycledTexture sceneDepth,
        List<ParticleSystem> systems,
        Matrix4x4 viewProjection,
        Vector3 camPos,
        Vector3 camRight,
        Vector3 camUp,
        float deltaTime,
        float totalTime)
    {
        var numSystems = Math.Min(systems.Count, MaxSystems);
        if (numSystems == 0)
        {
            return;
        }

        var frameIndex = GraphicsContext.FrameIndex;

        // Build constants with per-system params
        var constants = new ParticleConstants
        {
            ViewProjection = viewProjection,
            CameraPosition = camPos,
            DeltaTime = deltaTime,
            CameraRight = camRight,
            TotalTime = totalTime,
            CameraUp = camUp,
            ParticlesPerSystem = ParticlesPerSystem,
            NumActiveSystems = (uint)numSystems
        };

        // Fill per-system params
        for (var i = 0; i < numSystems; i++)
        {
            var ps = systems[i];

            // Assign stable slot index (systems keep their index across frames)
            if (ps.GpuSlotIndex < 0)
            {
                ps.GpuSlotIndex = i;
            }

            // CPU emission logic: accumulate and calculate ring-buffer range
            uint emitThisFrame = 0;
            var emitStartIndex = (uint)ps.NextEmitIndex;

            if (ps.IsEmitting || ps.BurstCount > 0)
            {
                ps.EmitAccumulator += ps.EmissionRate * deltaTime + ps.BurstCount;
                ps.BurstCount = 0;

                emitThisFrame = (uint)MathF.Floor(ps.EmitAccumulator);
                if (emitThisFrame > ParticlesPerSystem)
                {
                    emitThisFrame = ParticlesPerSystem;
                }

                ps.EmitAccumulator -= emitThisFrame;
                emitStartIndex = (uint)ps.NextEmitIndex;
                ps.NextEmitIndex = (int)((emitStartIndex + emitThisFrame) % ParticlesPerSystem);
            }

            var sysParams = new SystemParams
            {
                EmitterPosition = ps.Position,
                EmitStartIndex = emitStartIndex,
                EmitCount = emitThisFrame,
                StartLifetimeMin = ps.StartLifetimeMin,
                StartLifetimeMax = ps.StartLifetimeMax,
                StartSpeedMin = ps.StartSpeedMin,
                StartSpeedMax = ps.StartSpeedMax,
                StartSizeMin = ps.StartSizeMin,
                StartSizeMax = ps.StartSizeMax,
                GravityModifier = ps.GravityModifier,
                Drag = ps.Drag,
                EmitterRadius = ps.EmitterRadius,
                EmitterAngle = ps.EmitterAngle,
                StartColor = ps.StartColor,
                EndColor = ps.EndColor
            };

            SetSystem(ref constants, i, sysParams);
        }

        _constantBuffer.Write(constants);

        // Update bind groups for this frame
        _computeBindGroups[frameIndex].BeginUpdate();
        _computeBindGroups[frameIndex].CbvWithDesc(new BindBufferDesc
        {
            Binding = 0,
            Resource = _constantBuffer[frameIndex]
        });
        _computeBindGroups[frameIndex].UavBuffer(0, _particles.Buffer);
        _computeBindGroups[frameIndex].UavBuffer(1, _resolved.Buffer);
        _computeBindGroups[frameIndex].EndUpdate();

        _rasterBindGroups[frameIndex].BeginUpdate();
        _rasterBindGroups[frameIndex].CbvWithDesc(new BindBufferDesc
        {
            Binding = 0,
            Resource = _constantBuffer[frameIndex]
        });
        _rasterBindGroups[frameIndex].SrvBufferWithDesc(new BindBufferDesc
        {
            Binding = 0,
            Resource = _resolved.Buffer
        });
        _rasterBindGroups[frameIndex].EndUpdate();

        // Single compute dispatch for all systems
        var totalActiveParticles = (uint)(numSystems * ParticlesPerSystem);
        var compute = frame.BeginComputePass();
        compute.Begin();
        compute.BindPipeline(_computePipeline);
        compute.Bind(_computeBindGroups[frameIndex]);
        compute.Dispatch((totalActiveParticles + ThreadGroupSize - 1) / ThreadGroupSize, 1, 1);
        compute.UavBarrier();
        compute.End();

        // Single draw call for all systems (depends on compute)
        var graphics = frame.BeginGraphicsPass(compute);
        graphics.SetRenderTarget(0, sceneColor, LoadOp.Load);
        graphics.SetDepthTarget(sceneDepth, LoadOp.Load);
        graphics.Begin();
        graphics.BindPipeline(_rasterPipeline);
        graphics.Bind(_rasterBindGroups[frameIndex]);
        graphics.Draw(6, totalActiveParticles);
        graphics.End();
    }

    private static void SetSystem(ref ParticleConstants constants, int index, SystemParams system)
    {
        switch (index)
        {
            case 0: constants.System0 = system; break;
            case 1: constants.System1 = system; break;
            case 2: constants.System2 = system; break;
            case 3: constants.System3 = system; break;
            case 4: constants.System4 = system; break;
            case 5: constants.System5 = system; break;
            case 6: constants.System6 = system; break;
            case 7: constants.System7 = system; break;
        }
    }

    public void Dispose()
    {
        foreach (var bg in _computeBindGroups)
        {
            bg.Dispose();
        }

        foreach (var bg in _rasterBindGroups)
        {
            bg.Dispose();
        }

        _computePipeline.Dispose();
        _computeRootSignature.Dispose();
        _computeBindGroupLayout.Dispose();
        _computeProgram.Dispose();

        _rasterPipeline.Dispose();
        _rasterInputLayout.Dispose();
        _rasterRootSignature.Dispose();
        _rasterBindGroupLayout.Dispose();
        _rasterProgram.Dispose();

        _particles.Dispose();
        _resolved.Dispose();
        _constantBuffer.Dispose();
    }
}
