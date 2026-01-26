using DenOfIz;
using NiziKit.Assets;

namespace NiziKit.Graphics;

public class GpuShader : IDisposable
{
    public Pipeline Pipeline { get; }
    public ShaderProgram ShaderProgram { get; private set; }
    public RootSignature RootSignature { get; private set; }
    public InputLayout InputLayout { get; private set; }
    public VertexFormat VertexFormat { get; private set; }
    public LocalRootSignature[] LocalRootSignatures { get; private set; } = [];

    private readonly bool _ownsProgram;

    private GpuShader(ShaderProgram program, GraphicsPipelineDesc? graphicsDesc,
        RayTracingPipelineDesc? rayTracingPipelineDesc, BindPoint? explicitBindPoint = null,
        bool ownsProgram = true, int[]? explicitLocalRootSigIndices = null)
    {
        ShaderProgram = program;
        _ownsProgram = ownsProgram;
        var reflection = program.Reflect();

        var store = GraphicsContext.BindGroupLayoutStore;
        var bindGroupLayouts = new[]
        {
            store.Camera,
            store.Surface,
            store.Draw
        };

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create(bindGroupLayouts),
            RootConstants = reflection.RootConstants
        };
        RootSignature = GraphicsContext.Device.CreateRootSignature(rootSigDesc);
        InputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);
        VertexFormat = VertexFormat.FromInputLayout(reflection.InputLayout);

        var bindPoint = explicitBindPoint ?? DetermineBindPoint(graphicsDesc, rayTracingPipelineDesc);

        if (bindPoint == BindPoint.Raytracing && rayTracingPipelineDesc != null)
        {
            LocalRootSignatures = CreateLocalRootSignatures(reflection);

            if (LocalRootSignatures.Length > 0)
            {
                rayTracingPipelineDesc = AssignLocalRootSignaturesToHitGroups(
                    rayTracingPipelineDesc.Value, LocalRootSignatures, explicitLocalRootSigIndices);
            }
        }

        var pipelineDesc = new PipelineDesc
        {
            BindPoint = bindPoint,
            ShaderProgram = program,
            RootSignature = RootSignature,
            InputLayout = InputLayout,
            Graphics = graphicsDesc ?? new GraphicsPipelineDesc(),
            RayTracing = rayTracingPipelineDesc ?? new RayTracingPipelineDesc(),
        };

        Pipeline = GraphicsContext.Device.CreatePipeline(pipelineDesc);
    }

    private static LocalRootSignature[] CreateLocalRootSignatures(ShaderReflectDesc reflection)
    {
        var localRootSigDescs = reflection.LocalRootSignatures.ToArray();
        if (localRootSigDescs.Length == 0)
        {
            return [];
        }

        var signatures = new LocalRootSignature[localRootSigDescs.Length];
        for (uint i = 0; i < localRootSigDescs.Length; i++)
        {
            var desc = localRootSigDescs[i];
            signatures[i] = GraphicsContext.Device.CreateLocalRootSignature(desc);
        }

        return signatures;
    }

    private static RayTracingPipelineDesc AssignLocalRootSignaturesToHitGroups(
        RayTracingPipelineDesc rtDesc, LocalRootSignature[] localRootSigs, int[]? explicitIndices = null)
    {
        var hitGroups = rtDesc.HitGroups.ToArray();
        if (hitGroups.Length == 0)
        {
            return new RayTracingPipelineDesc
            {
                HitGroups = rtDesc.HitGroups,
                LocalRootSignatures = LocalRootSignatureArray.Create(localRootSigs)
            };
        }

        var updatedHitGroups = new HitGroupDesc[hitGroups.Length];
        for (var i = 0; i < hitGroups.Length; i++)
        {
            var hg = hitGroups[i];

            var explicitIndex = explicitIndices != null && i < explicitIndices.Length
                ? explicitIndices[i]
                : -1;

            int sigIndex;
            if (explicitIndex >= 0)
            {
                sigIndex = explicitIndex;
            }
            else
            {
                sigIndex = hg.ClosestHitShaderIndex >= 0 ? hg.ClosestHitShaderIndex :
                           hg.AnyHitShaderIndex >= 0 ? hg.AnyHitShaderIndex :
                           hg.IntersectionShaderIndex >= 0 ? hg.IntersectionShaderIndex : -1;
            }

            if (sigIndex >= 0 && sigIndex < localRootSigs.Length)
            {
                hg.LocalRootSignature = localRootSigs[sigIndex];
            }

            updatedHitGroups[i] = hg;
        }

        return new RayTracingPipelineDesc
        {
            HitGroups = HitGroupDescArray.Create(updatedHitGroups),
            LocalRootSignatures = LocalRootSignatureArray.Create(localRootSigs)
        };
    }

    private static BindPoint DetermineBindPoint(GraphicsPipelineDesc? graphicsDesc, RayTracingPipelineDesc? rayTracingPipelineDesc)
    {
        if (rayTracingPipelineDesc != null)
        {
            return BindPoint.Raytracing;
        }

        if (graphicsDesc != null)
        {
            return BindPoint.Graphics;
        }

        return BindPoint.Compute;
    }

    public static GpuShader Compute(ShaderProgram program, bool ownsProgram = true)
    {
        return new GpuShader(program, null, null, BindPoint.Compute, ownsProgram);
    }

    public static GpuShader Graphics(ShaderProgram program, GraphicsPipelineDesc graphicsDesc, bool ownsProgram = true)
    {
        return new GpuShader(program, graphicsDesc, null, BindPoint.Graphics, ownsProgram);
    }

    public static GpuShader RayTracing(ShaderProgram program, RayTracingPipelineDesc rayTracingPipelineDesc,
        bool ownsProgram = true, int[]? explicitLocalRootSigIndices = null)
    {
        return new GpuShader(program, null, rayTracingPipelineDesc, BindPoint.Raytracing, ownsProgram, explicitLocalRootSigIndices);
    }

    public static GpuShader Mesh(ShaderProgram program, GraphicsPipelineDesc graphicsDesc, bool ownsProgram = true)
    {
        return new GpuShader(program, graphicsDesc, null, BindPoint.Mesh, ownsProgram);
    }

    public void Dispose()
    {
        Pipeline.Dispose();
        RootSignature.Dispose();
        InputLayout.Dispose();

        foreach (var localRootSig in LocalRootSignatures)
        {
            localRootSig.Dispose();
        }

        if (_ownsProgram)
        {
            ShaderProgram.Dispose();
        }
    }
}
