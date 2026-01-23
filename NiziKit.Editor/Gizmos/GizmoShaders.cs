using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics;

namespace NiziKit.Editor.Gizmos;

[StructLayout(LayoutKind.Sequential)]
public struct GizmoConstants
{
    public Matrix4x4 ViewProjection;
    public Matrix4x4 ModelMatrix;
    public Vector3 CameraPosition;
    public float DepthBias;
    public Vector4 SelectionColor;
    public float Opacity;
    public float Time;
    public Vector2 ScreenSize;
}

public sealed class GizmoShaders : IDisposable
{
    private readonly ShaderProgram _program;
    private readonly ShaderProgram _gridProgram;
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly BindGroupLayout _bindGroupLayout;
    private readonly Pipeline _trianglePipeline;
    private readonly Pipeline _linePipeline;
    private readonly Pipeline _gridPipeline;

    public Pipeline TrianglePipeline => _trianglePipeline;
    public Pipeline LinePipeline => _linePipeline;
    public Pipeline GridPipeline => _gridPipeline;
    public RootSignature RootSignature => _rootSignature;
    public BindGroupLayout BindGroupLayout => _bindGroupLayout;

    public GizmoShaders()
    {
        _program = BuiltinShaderProgram.Load("GizmoShader")
                   ?? throw new InvalidOperationException("GizmoShader not found");
        _gridProgram = BuiltinShaderProgram.Load("GridShader")
                       ?? throw new InvalidOperationException("GridShader not found");

        var reflection = _program.Reflect();
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _bindGroupLayout = GraphicsContext.Device.CreateBindGroupLayout(bindGroupLayoutDescs[0]);

        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create([_bindGroupLayout])
        };
        _rootSignature = GraphicsContext.Device.CreateRootSignature(rootSigDesc);
        _inputLayout = GraphicsContext.Device.CreateInputLayout(reflection.InputLayout);

        var blendDesc = new BlendDesc
        {
            Enable = true,
            SrcBlend = Blend.SrcAlpha,
            DstBlend = Blend.InvSrcAlpha,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DstBlendAlpha = Blend.InvSrcAlpha,
            BlendOpAlpha = BlendOp.Add,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = GraphicsContext.BackBufferFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var trianglePipelineDesc = new PipelineDesc
        {
            RootSignature = _rootSignature,
            InputLayout = _inputLayout,
            ShaderProgram = _program,
            BindPoint = BindPoint.Graphics,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                DepthTest = new DepthTest
                {
                    Enable = false,
                    CompareOp = CompareOp.Always,
                    Write = false
                },
                RenderTargets = renderTargets,
                DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat
            }
        };

        _trianglePipeline = GraphicsContext.Device.CreatePipeline(trianglePipelineDesc);

        var linePipelineDesc = new PipelineDesc
        {
            RootSignature = _rootSignature,
            InputLayout = _inputLayout,
            ShaderProgram = _program,
            BindPoint = BindPoint.Graphics,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Line,
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                DepthTest = new DepthTest
                {
                    Enable = false,
                    CompareOp = CompareOp.Always,
                    Write = false
                },
                RenderTargets = renderTargets,
                DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat
            }
        };

        _linePipeline = GraphicsContext.Device.CreatePipeline(linePipelineDesc);

        var gridPipelineDesc = new PipelineDesc
        {
            RootSignature = _rootSignature,
            InputLayout = _inputLayout,
            ShaderProgram = _gridProgram,
            BindPoint = BindPoint.Graphics,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Line,
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                DepthTest = new DepthTest
                {
                    Enable = false,
                    CompareOp = CompareOp.Always,
                    Write = false
                },
                RenderTargets = renderTargets,
                DepthStencilAttachmentFormat = GraphicsContext.DepthBufferFormat
            }
        };

        _gridPipeline = GraphicsContext.Device.CreatePipeline(gridPipelineDesc);
    }

    public void Dispose()
    {
        _trianglePipeline.Dispose();
        _linePipeline.Dispose();
        _gridPipeline.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        _bindGroupLayout.Dispose();
        _program.Dispose();
        _gridProgram.Dispose();
    }
}
