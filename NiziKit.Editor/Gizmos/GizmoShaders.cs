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
    private readonly RootSignature _rootSignature;
    private readonly InputLayout _inputLayout;
    private readonly BindGroupLayout _bindGroupLayout;
    private readonly Pipeline _pipeline;

    public Pipeline Pipeline => _pipeline;
    public RootSignature RootSignature => _rootSignature;
    public BindGroupLayout BindGroupLayout => _bindGroupLayout;

    public GizmoShaders()
    {
        _program = BuiltinShaderProgram.Load("GizmoShader")
                   ?? throw new InvalidOperationException("GizmoShader not found");

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

        var pipelineDesc = new PipelineDesc
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

        _pipeline = GraphicsContext.Device.CreatePipeline(pipelineDesc);
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        _inputLayout.Dispose();
        _rootSignature.Dispose();
        _bindGroupLayout.Dispose();
        _program.Dispose();
    }
}
