using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Buffers;
using NiziKit.Graphics.Renderer.Pass;
using NiziKit.Graphics.Resources;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Editor.Gizmos;

public sealed class GizmoPass : IDisposable
{
    private const int MaxVertices = 16384;

    private readonly GizmoShaders _shaders;
    private readonly Buffer[] _vertexBuffers;
    private readonly IntPtr[] _vertexBufferPtrs;
    private readonly MappedBuffer<GizmoConstants> _constantBuffer;
    private readonly BindGroup[] _bindGroups;
    private readonly Texture?[] _boundDepthTextures;

    private readonly List<GizmoVertex> _vertices = new(MaxVertices);

    public TransformGizmo Gizmo { get; } = new();

    public GizmoPass()
    {
        _shaders = new GizmoShaders();

        var numFrames = (int)GraphicsContext.NumFrames;
        _vertexBuffers = new Buffer[numFrames];
        _vertexBufferPtrs = new IntPtr[numFrames];
        _bindGroups = new BindGroup[numFrames];
        _boundDepthTextures = new Texture?[numFrames];

        var vertexBufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * MaxVertices);

        for (var i = 0; i < numFrames; i++)
        {
            _vertexBuffers[i] = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = vertexBufferSize,
                Usage = (uint)BufferUsageFlagBits.Vertex,
                HeapType = HeapType.CpuGpu,
                DebugName = StringView.Create($"GizmoVertexBuffer_{i}")
            });
            _vertexBufferPtrs[i] = _vertexBuffers[i].MapMemory();

            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _shaders.BindGroupLayout
            });
        }

        _constantBuffer = new MappedBuffer<GizmoConstants>(true, "GizmoConstants");
    }

    public void BeginFrame()
    {
        _vertices.Clear();
    }

    public void BuildGizmoGeometry(CameraComponent camera)
    {
        var target = Gizmo.Target;
        if (target == null)
        {
            return;
        }

        var origin = target.WorldPosition;
        var rotation = Gizmo.Space == GizmoSpace.Local ? target.LocalRotation : Quaternion.Identity;
        var scale = Gizmo.GetGizmoScale(camera);

        switch (Gizmo.Mode)
        {
            case GizmoMode.Translate:
                GizmoGeometry.BuildTranslateGizmo(_vertices, origin, rotation, scale, Gizmo.HoveredAxis, Gizmo.ActiveAxis);
                break;
            case GizmoMode.Rotate:
                GizmoGeometry.BuildRotateGizmo(_vertices, origin, rotation, scale, Gizmo.HoveredAxis, Gizmo.ActiveAxis);
                break;
            case GizmoMode.Scale:
                GizmoGeometry.BuildScaleGizmo(_vertices, origin, rotation, scale, Gizmo.HoveredAxis, Gizmo.ActiveAxis);
                break;
        }
    }

    public void AddSelectionBox(GameObject obj)
    {
        var meshComponent = obj.GetComponent<MeshComponent>();
        if (meshComponent?.Mesh == null)
        {
            return;
        }

        var boxVerts = GizmoGeometry.BuildSelectionBox(
            meshComponent.Mesh.Bounds,
            obj.WorldMatrix,
            GizmoGeometry.SelectionBoxColor);

        if (_vertices.Count + boxVerts.Length > MaxVertices)
        {
            return;
        }

        _vertices.AddRange(boxVerts);
    }

    public void Render(GraphicsPass pass, ViewData viewData, CycledTexture sceneDepth)
    {
        if (_vertices.Count == 0)
        {
            return;
        }

        var frameIndex = GraphicsContext.FrameIndex;

        UploadVertices(frameIndex);
        UpdateConstants(viewData);
        UpdateBindGroup(frameIndex, sceneDepth[frameIndex]);

        pass.BindPipeline(_shaders.Pipeline);
        pass.Bind(_bindGroups[frameIndex]);
        pass.BindVertexBuffer(_vertexBuffers[frameIndex], 0, (uint)Marshal.SizeOf<GizmoVertex>());
        pass.Draw((uint)_vertices.Count, 1, 0, 0);
    }

    private void UploadVertices(int frameIndex)
    {
        var size = Marshal.SizeOf<GizmoVertex>() * _vertices.Count;
        unsafe
        {
            var span = CollectionsMarshal.AsSpan(_vertices);
            fixed (GizmoVertex* src = span)
            {
                System.Buffer.MemoryCopy(src, (void*)_vertexBufferPtrs[frameIndex], size, size);
            }
        }
    }

    private void UpdateConstants(ViewData viewData)
    {
        var camera = viewData.Camera ?? viewData.Scene?.GetActiveCamera();
        var viewProjection = camera?.ViewProjectionMatrix ?? Matrix4x4.Identity;
        var cameraPosition = camera?.WorldPosition ?? Vector3.Zero;

        var constants = new GizmoConstants
        {
            ViewProjection = viewProjection,
            CameraPosition = cameraPosition,
            DepthBias = 0.0001f,
            SelectionColor = GizmoGeometry.SelectionBoxColor,
            Opacity = 1f,
            Time = viewData.TotalTime,
            ScreenSize = new Vector2(GraphicsContext.Width, GraphicsContext.Height)
        };

        _constantBuffer.Write(in constants);
    }

    private void UpdateBindGroup(int frameIndex, Texture depthTexture)
    {
        if (_boundDepthTextures[frameIndex] == depthTexture)
        {
            return;
        }

        var bindGroup = _bindGroups[frameIndex];
        bindGroup.BeginUpdate();
        bindGroup.Cbv(0, _constantBuffer[frameIndex]);
        bindGroup.SrvTexture(0, depthTexture);
        bindGroup.EndUpdate();

        _boundDepthTextures[frameIndex] = depthTexture;
    }

    public void Dispose()
    {
        foreach (var buffer in _vertexBuffers)
        {
            buffer.UnmapMemory();
            buffer.Dispose();
        }

        foreach (var bindGroup in _bindGroups)
        {
            bindGroup.Dispose();
        }

        _constantBuffer.Dispose();
        _shaders.Dispose();
    }
}
