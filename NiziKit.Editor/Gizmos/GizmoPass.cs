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
    private const int MaxTriangleVertices = 32768;
    private const int MaxLineVertices = 1024;

    private readonly GizmoShaders _shaders;
    private readonly Buffer[] _triangleVertexBuffers;
    private readonly IntPtr[] _triangleVertexBufferPtrs;
    private readonly Buffer[] _lineVertexBuffers;
    private readonly IntPtr[] _lineVertexBufferPtrs;
    private readonly MappedBuffer<GizmoConstants> _constantBuffer;
    private readonly BindGroup[] _bindGroups;
    private readonly Texture?[] _boundDepthTextures;

    private readonly List<GizmoVertex> _triangleVertices = new(MaxTriangleVertices);
    private readonly List<GizmoVertex> _lineVertices = new(MaxLineVertices);

    public TransformGizmo Gizmo { get; } = new();

    public GizmoPass()
    {
        _shaders = new GizmoShaders();

        var numFrames = (int)GraphicsContext.NumFrames;
        _triangleVertexBuffers = new Buffer[numFrames];
        _triangleVertexBufferPtrs = new IntPtr[numFrames];
        _lineVertexBuffers = new Buffer[numFrames];
        _lineVertexBufferPtrs = new IntPtr[numFrames];
        _bindGroups = new BindGroup[numFrames];
        _boundDepthTextures = new Texture?[numFrames];

        var triangleBufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * MaxTriangleVertices);
        var lineBufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * MaxLineVertices);

        for (var i = 0; i < numFrames; i++)
        {
            _triangleVertexBuffers[i] = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = triangleBufferSize,
                Usage = (uint)BufferUsageFlagBits.Vertex,
                HeapType = HeapType.CpuGpu,
                DebugName = StringView.Create($"GizmoTriangleVertexBuffer_{i}")
            });
            _triangleVertexBufferPtrs[i] = _triangleVertexBuffers[i].MapMemory();

            _lineVertexBuffers[i] = GraphicsContext.Device.CreateBuffer(new BufferDesc
            {
                NumBytes = lineBufferSize,
                Usage = (uint)BufferUsageFlagBits.Vertex,
                HeapType = HeapType.CpuGpu,
                DebugName = StringView.Create($"GizmoLineVertexBuffer_{i}")
            });
            _lineVertexBufferPtrs[i] = _lineVertexBuffers[i].MapMemory();

            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _shaders.BindGroupLayout
            });
        }

        _constantBuffer = new MappedBuffer<GizmoConstants>(true, "GizmoConstants");
    }

    public void BeginFrame()
    {
        _triangleVertices.Clear();
        _lineVertices.Clear();
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
                GizmoGeometry.BuildTranslateGizmo(_triangleVertices, origin, rotation, scale, Gizmo.HoveredAxis, Gizmo.ActiveAxis);
                break;
            case GizmoMode.Rotate:
                GizmoGeometry.BuildRotateGizmo(_triangleVertices, origin, rotation, scale, Gizmo.HoveredAxis, Gizmo.ActiveAxis);
                break;
            case GizmoMode.Scale:
                GizmoGeometry.BuildScaleGizmo(_triangleVertices, origin, rotation, scale, Gizmo.HoveredAxis, Gizmo.ActiveAxis);
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

        if (_lineVertices.Count + boxVerts.Length > MaxLineVertices)
        {
            return;
        }

        _lineVertices.AddRange(boxVerts);
    }

    public void Render(GraphicsPass pass, ViewData viewData, CycledTexture sceneDepth)
    {
        if (_triangleVertices.Count == 0 && _lineVertices.Count == 0)
        {
            return;
        }

        var frameIndex = GraphicsContext.FrameIndex;

        UpdateConstants(viewData);
        UpdateBindGroup(frameIndex, sceneDepth[frameIndex]);

        var vertexStride = (uint)Marshal.SizeOf<GizmoVertex>();

        // Render filled triangles (gizmo geometry)
        if (_triangleVertices.Count > 0)
        {
            UploadTriangleVertices(frameIndex);
            pass.BindPipeline(_shaders.TrianglePipeline);
            pass.Bind(_bindGroups[frameIndex]);
            pass.BindVertexBuffer(_triangleVertexBuffers[frameIndex], 0, vertexStride);
            pass.Draw((uint)_triangleVertices.Count, 1, 0, 0);
        }

        // Render lines (selection box)
        if (_lineVertices.Count > 0)
        {
            UploadLineVertices(frameIndex);
            pass.BindPipeline(_shaders.LinePipeline);
            pass.Bind(_bindGroups[frameIndex]);
            pass.BindVertexBuffer(_lineVertexBuffers[frameIndex], 0, vertexStride);
            pass.Draw((uint)_lineVertices.Count, 1, 0, 0);
        }
    }

    private void UploadTriangleVertices(int frameIndex)
    {
        var size = Marshal.SizeOf<GizmoVertex>() * _triangleVertices.Count;
        unsafe
        {
            var span = CollectionsMarshal.AsSpan(_triangleVertices);
            fixed (GizmoVertex* src = span)
            {
                System.Buffer.MemoryCopy(src, (void*)_triangleVertexBufferPtrs[frameIndex], size, size);
            }
        }
    }

    private void UploadLineVertices(int frameIndex)
    {
        var size = Marshal.SizeOf<GizmoVertex>() * _lineVertices.Count;
        unsafe
        {
            var span = CollectionsMarshal.AsSpan(_lineVertices);
            fixed (GizmoVertex* src = span)
            {
                System.Buffer.MemoryCopy(src, (void*)_lineVertexBufferPtrs[frameIndex], size, size);
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
        foreach (var buffer in _triangleVertexBuffers)
        {
            buffer.UnmapMemory();
            buffer.Dispose();
        }

        foreach (var buffer in _lineVertexBuffers)
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
