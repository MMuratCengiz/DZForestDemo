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
    private const int MaxSelectionBoxes = 64;
    private const int NumModes = 3;
    private const int NumAxes = 8;
    private const int UnitCubeVertexCount = 24;

    private static readonly int[] BoxEdges =
    [
        0, 1, 1, 2, 2, 3, 3, 0,
        4, 5, 5, 6, 6, 7, 7, 4,
        0, 4, 1, 5, 2, 6, 3, 7
    ];

    private readonly GizmoShaders _shaders;
    private readonly MappedBuffer<GizmoConstants> _gizmoConstantBuffer;
    private readonly MappedBuffer<GizmoConstants> _selectionConstantBuffer;
    private readonly BindGroup[] _gizmoBindGroups;
    private readonly BindGroup[] _selectionBindGroups;
    private readonly Texture?[] _boundDepthTextures;

    private readonly Buffer[,] _gizmoBuffers;
    private readonly int[,] _gizmoVertexCounts;

    private readonly Buffer _unitCubeBuffer;
    private readonly List<Matrix4x4> _selectionBoxMatrices = new(MaxSelectionBoxes);

    private Vector3 _gizmoOrigin;
    private Quaternion _gizmoRotation;
    private float _gizmoScale;
    private bool _hasGizmo;

    public TransformGizmo Gizmo { get; } = new();

    public GizmoPass()
    {
        _shaders = new GizmoShaders();

        var numFrames = (int)GraphicsContext.NumFrames;
        _gizmoBindGroups = new BindGroup[numFrames];
        _selectionBindGroups = new BindGroup[numFrames];
        _boundDepthTextures = new Texture?[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _gizmoBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _shaders.BindGroupLayout
            });
            _selectionBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _shaders.BindGroupLayout
            });
        }

        _gizmoConstantBuffer = new MappedBuffer<GizmoConstants>(true, "GizmoConstants");
        _selectionConstantBuffer = new MappedBuffer<GizmoConstants>(true, "SelectionConstants");

        _gizmoBuffers = new Buffer[NumModes, NumAxes];
        _gizmoVertexCounts = new int[NumModes, NumAxes];
        _unitCubeBuffer = BuildUnitCubeBuffer();
        BuildAllGizmoBuffers();
    }

    private Buffer BuildUnitCubeBuffer()
    {
        Vector3[] corners =
        [
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1),
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1)
        ];

        var vertices = new GizmoVertex[UnitCubeVertexCount];
        for (var i = 0; i < UnitCubeVertexCount; i++)
            vertices[i] = new GizmoVertex(corners[BoxEdges[i]], GizmoGeometry.SelectionBoxColor);

        var bufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * UnitCubeVertexCount);
        var buffer = GraphicsContext.Device.CreateBuffer(new BufferDesc
        {
            NumBytes = bufferSize,
            Usage = (uint)BufferUsageFlagBits.Vertex,
            HeapType = HeapType.Gpu,
            DebugName = StringView.Create("UnitCubeBuffer")
        });

        lock (GraphicsContext.GpuLock)
        {
            var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
            {
                Device = GraphicsContext.Device,
                IssueBarriers = false
            });
            batchCopy.Begin();

            var handle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            try
            {
                batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
                {
                    Data = new ByteArrayView
                    {
                        Elements = handle.AddrOfPinnedObject(),
                        NumElements = bufferSize
                    },
                    DstBuffer = buffer,
                    DstBufferOffset = 0
                });
            }
            finally
            {
                handle.Free();
            }

            batchCopy.Submit(null);
            batchCopy.Dispose();
        }

        return buffer;
    }

    private void BuildAllGizmoBuffers()
    {
        var vertices = new List<GizmoVertex>(8192);
        var modes = new[] { GizmoMode.Translate, GizmoMode.Rotate, GizmoMode.Scale };
        var axes = new[] { GizmoAxis.None, GizmoAxis.X, GizmoAxis.Y, GizmoAxis.Z, GizmoAxis.XY, GizmoAxis.XZ, GizmoAxis.YZ, GizmoAxis.All };

        lock (GraphicsContext.GpuLock)
        {
            var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
            {
                Device = GraphicsContext.Device,
                IssueBarriers = false
            });
            batchCopy.Begin();

            for (var m = 0; m < NumModes; m++)
            {
                for (var a = 0; a < NumAxes; a++)
                {
                    vertices.Clear();
                    var mode = modes[m];
                    var axis = axes[a];

                    switch (mode)
                    {
                        case GizmoMode.Translate:
                            GizmoGeometry.BuildTranslateGizmoLocal(vertices, axis, axis);
                            break;
                        case GizmoMode.Rotate:
                            GizmoGeometry.BuildRotateGizmoLocal(vertices, axis, axis);
                            break;
                        case GizmoMode.Scale:
                            GizmoGeometry.BuildScaleGizmoLocal(vertices, axis, axis);
                            break;
                    }

                    _gizmoVertexCounts[m, a] = vertices.Count;

                    if (vertices.Count == 0)
                    {
                        continue;
                    }

                    var bufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * vertices.Count);
                    var buffer = GraphicsContext.Device.CreateBuffer(new BufferDesc
                    {
                        NumBytes = bufferSize,
                        Usage = (uint)BufferUsageFlagBits.Vertex,
                        HeapType = HeapType.Gpu,
                        DebugName = StringView.Create($"Gizmo_{mode}_{axis}")
                    });

                    var array = CollectionsMarshal.AsSpan(vertices).ToArray();
                    var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                    try
                    {
                        batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
                        {
                            Data = new ByteArrayView
                            {
                                Elements = handle.AddrOfPinnedObject(),
                                NumElements = bufferSize
                            },
                            DstBuffer = buffer,
                            DstBufferOffset = 0
                        });
                    }
                    finally
                    {
                        handle.Free();
                    }

                    _gizmoBuffers[m, a] = buffer;
                }
            }

            batchCopy.Submit(null);
            batchCopy.Dispose();
        }
    }

    private (int modeIndex, int axisIndex) GetGizmoIndices()
    {
        var modeIndex = Gizmo.Mode switch
        {
            GizmoMode.Translate => 0,
            GizmoMode.Rotate => 1,
            GizmoMode.Scale => 2,
            _ => 0
        };

        var highlightAxis = Gizmo.ActiveAxis != GizmoAxis.None ? Gizmo.ActiveAxis : Gizmo.HoveredAxis;
        var axisIndex = highlightAxis switch
        {
            GizmoAxis.None => 0,
            GizmoAxis.X => 1,
            GizmoAxis.Y => 2,
            GizmoAxis.Z => 3,
            GizmoAxis.XY => 4,
            GizmoAxis.XZ => 5,
            GizmoAxis.YZ => 6,
            GizmoAxis.All => 7,
            _ => 0
        };

        return (modeIndex, axisIndex);
    }

    public void BeginFrame()
    {
        _selectionBoxMatrices.Clear();
        _hasGizmo = false;
    }

    public void BuildGizmoGeometry(CameraComponent camera)
    {
        var target = Gizmo.Target;
        if (target == null)
        {
            return;
        }

        _gizmoOrigin = target.WorldPosition;
        _gizmoRotation = Gizmo.Space == GizmoSpace.Local ? target.LocalRotation : Quaternion.Identity;
        _gizmoScale = Gizmo.GetGizmoScale(camera);
        _hasGizmo = true;
    }

    public void AddSelectionBox(GameObject obj)
    {
        var meshComponent = obj.GetComponent<MeshComponent>();
        if (meshComponent?.Mesh == null)
        {
            return;
        }

        if (_selectionBoxMatrices.Count >= MaxSelectionBoxes)
        {
            return;
        }

        var bounds = meshComponent.Mesh.Bounds;
        var scale = bounds.Max - bounds.Min;
        var boundsMatrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(bounds.Min);
        _selectionBoxMatrices.Add(boundsMatrix * obj.WorldMatrix);
    }

    public void Render(GraphicsPass pass, ViewData viewData, CycledTexture sceneDepth)
    {
        if (!_hasGizmo && _selectionBoxMatrices.Count == 0)
        {
            return;
        }

        var frameIndex = GraphicsContext.FrameIndex;
        var depthTexture = sceneDepth[frameIndex];
        UpdateBindGroups(frameIndex, depthTexture);

        var vertexStride = (uint)Marshal.SizeOf<GizmoVertex>();

        if (_hasGizmo)
        {
            var (modeIndex, axisIndex) = GetGizmoIndices();
            var buffer = _gizmoBuffers[modeIndex, axisIndex];
            var vertexCount = _gizmoVertexCounts[modeIndex, axisIndex];

            if (buffer != null && vertexCount > 0)
            {
                UpdateGizmoConstants(viewData);
                pass.BindPipeline(_shaders.TrianglePipeline);
                pass.Bind(_gizmoBindGroups[frameIndex]);
                pass.BindVertexBuffer(buffer, 0, vertexStride);
                pass.Draw((uint)vertexCount, 1, 0, 0);
            }
        }

        if (_selectionBoxMatrices.Count > 0)
        {
            pass.BindPipeline(_shaders.LinePipeline);
            pass.Bind(_selectionBindGroups[frameIndex]);
            pass.BindVertexBuffer(_unitCubeBuffer, 0, vertexStride);

            foreach (var matrix in _selectionBoxMatrices)
            {
                UpdateSelectionConstants(viewData, matrix);
                pass.Draw(UnitCubeVertexCount, 1, 0, 0);
            }
        }
    }

    private Matrix4x4 GetGizmoModelMatrix()
    {
        return Matrix4x4.CreateScale(_gizmoScale) *
               Matrix4x4.CreateFromQuaternion(_gizmoRotation) *
               Matrix4x4.CreateTranslation(_gizmoOrigin);
    }

    private void UpdateGizmoConstants(ViewData viewData)
    {
        var camera = viewData.Camera ?? viewData.Scene?.GetActiveCamera();
        var viewProjection = camera?.ViewProjectionMatrix ?? Matrix4x4.Identity;
        var cameraPosition = camera?.WorldPosition ?? Vector3.Zero;

        var constants = new GizmoConstants
        {
            ViewProjection = viewProjection,
            ModelMatrix = GetGizmoModelMatrix(),
            CameraPosition = cameraPosition,
            DepthBias = 0.0001f,
            SelectionColor = GizmoGeometry.SelectionBoxColor,
            Opacity = 1f,
            Time = viewData.TotalTime,
            ScreenSize = new Vector2(GraphicsContext.Width, GraphicsContext.Height)
        };

        _gizmoConstantBuffer.Write(in constants);
    }

    private void UpdateSelectionConstants(ViewData viewData, Matrix4x4 modelMatrix)
    {
        var camera = viewData.Camera ?? viewData.Scene?.GetActiveCamera();
        var viewProjection = camera?.ViewProjectionMatrix ?? Matrix4x4.Identity;
        var cameraPosition = camera?.WorldPosition ?? Vector3.Zero;

        var constants = new GizmoConstants
        {
            ViewProjection = viewProjection,
            ModelMatrix = modelMatrix,
            CameraPosition = cameraPosition,
            DepthBias = 0.0001f,
            SelectionColor = GizmoGeometry.SelectionBoxColor,
            Opacity = 1f,
            Time = viewData.TotalTime,
            ScreenSize = new Vector2(GraphicsContext.Width, GraphicsContext.Height)
        };

        _selectionConstantBuffer.Write(in constants);
    }

    private void UpdateBindGroups(int frameIndex, Texture depthTexture)
    {
        if (_boundDepthTextures[frameIndex] == depthTexture)
        {
            return;
        }

        _gizmoBindGroups[frameIndex].BeginUpdate();
        _gizmoBindGroups[frameIndex].Cbv(0, _gizmoConstantBuffer[frameIndex]);
        _gizmoBindGroups[frameIndex].SrvTexture(0, depthTexture);
        _gizmoBindGroups[frameIndex].EndUpdate();

        _selectionBindGroups[frameIndex].BeginUpdate();
        _selectionBindGroups[frameIndex].Cbv(0, _selectionConstantBuffer[frameIndex]);
        _selectionBindGroups[frameIndex].SrvTexture(0, depthTexture);
        _selectionBindGroups[frameIndex].EndUpdate();

        _boundDepthTextures[frameIndex] = depthTexture;
    }

    public void Dispose()
    {
        for (var m = 0; m < NumModes; m++)
        {
            for (var a = 0; a < NumAxes; a++)
                _gizmoBuffers[m, a]?.Dispose();
        }

        _unitCubeBuffer.Dispose();

        foreach (var bindGroup in _gizmoBindGroups)
            bindGroup.Dispose();

        foreach (var bindGroup in _selectionBindGroups)
            bindGroup.Dispose();

        _gizmoConstantBuffer.Dispose();
        _selectionConstantBuffer.Dispose();
        _shaders.Dispose();
    }
}
