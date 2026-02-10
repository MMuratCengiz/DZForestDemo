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
using NiziKit.Light;
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

    private static readonly Vector4 GridColorMajor = new(0.5f, 0.5f, 0.5f, 0.8f);
    private static readonly Vector4 GridColorMinor = new(0.35f, 0.35f, 0.35f, 0.5f);
    private static readonly Vector4 GridColorAxisX = new(0.7f, 0.2f, 0.2f, 0.9f);
    private static readonly Vector4 GridColorAxisZ = new(0.2f, 0.4f, 0.7f, 0.9f);

    private readonly GizmoShaders _shaders;
    private readonly MappedBuffer<GizmoConstants> _gizmoConstantBuffer;
    private readonly MappedBuffer<GizmoConstants> _selectionConstantBuffer;
    private readonly MappedBuffer<GizmoConstants> _gridConstantBuffer;
    private readonly BindGroup[] _gizmoBindGroups;
    private readonly BindGroup[] _selectionBindGroups;
    private readonly BindGroup[] _gridBindGroups;
    private readonly Texture?[] _boundDepthTextures;

    private readonly Buffer[,] _gizmoBuffers;
    private readonly int[,] _gizmoVertexCounts;

    private readonly Buffer _unitCubeBuffer;
    private readonly List<Matrix4x4> _selectionBoxMatrices = new(MaxSelectionBoxes);

    private Buffer? _gridBuffer;
    private int _gridVertexCount;
    private float _currentGridSize;
    private float _currentGridSpacing;

    private Vector3 _gizmoOrigin;
    private Quaternion _gizmoRotation;
    private float _gizmoScale;
    private bool _hasGizmo;

    // Scene icons (cameras/lights)
    private Buffer? _sceneIconBuffer;
    private int _sceneIconVertexCount;
    private readonly List<GizmoVertex> _sceneIconVertices = new(1024);
    private readonly MappedBuffer<GizmoConstants> _sceneIconConstantBuffer;
    private readonly BindGroup[] _sceneIconBindGroups;

    public bool ShowSceneIcons { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public float GridSize { get; set; } = 50f;
    public float GridSpacing { get; set; } = 1f;
    public int GridSubdivisions { get; set; } = 5;
    public Matrix4x4 GridOrientation { get; set; } = Matrix4x4.Identity;

    public TransformGizmo Gizmo { get; } = new();

    public GizmoPass()
    {
        _shaders = new GizmoShaders();

        var numFrames = (int)GraphicsContext.NumFrames;
        _gizmoBindGroups = new BindGroup[numFrames];
        _selectionBindGroups = new BindGroup[numFrames];
        _gridBindGroups = new BindGroup[numFrames];
        _sceneIconBindGroups = new BindGroup[numFrames];
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
            _gridBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _shaders.BindGroupLayout
            });
            _sceneIconBindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = _shaders.BindGroupLayout
            });
        }

        _gizmoConstantBuffer = new MappedBuffer<GizmoConstants>(true, "GizmoConstants");
        _selectionConstantBuffer = new MappedBuffer<GizmoConstants>(true, "SelectionConstants");
        _gridConstantBuffer = new MappedBuffer<GizmoConstants>(true, "GridConstants");
        _sceneIconConstantBuffer = new MappedBuffer<GizmoConstants>(true, "SceneIconConstants");

        _gizmoBuffers = new Buffer[NumModes, NumAxes];
        _gizmoVertexCounts = new int[NumModes, NumAxes];
        _unitCubeBuffer = BuildUnitCubeBuffer();
        BuildAllGizmoBuffers();
        BuildGridBuffer();
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
        {
            vertices[i] = new GizmoVertex(corners[BoxEdges[i]], GizmoGeometry.SelectionBoxColor);
        }

        var bufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * UnitCubeVertexCount);
        var buffer = GraphicsContext.Device.CreateBuffer(new BufferDesc
        {
            NumBytes = bufferSize,
            Usage = (uint)BufferUsageFlagBits.Vertex,
            HeapType = HeapType.Gpu,
            DebugName = StringView.Create("UnitCubeBuffer")
        });

        lock (GraphicsContext.TransferLock)
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

        lock (GraphicsContext.TransferLock)
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

    private void BuildGridBuffer()
    {
        if (MathF.Abs(_currentGridSize - GridSize) < 0.001f &&
            MathF.Abs(_currentGridSpacing - GridSpacing) < 0.001f &&
            _gridBuffer != null)
        {
            return;
        }

        _gridBuffer?.Dispose();
        _currentGridSize = GridSize;
        _currentGridSpacing = GridSpacing;

        var vertices = new List<GizmoVertex>();
        var halfSize = GridSize;
        var majorSpacing = GridSpacing * GridSubdivisions;

        for (var i = -halfSize; i <= halfSize; i += GridSpacing)
        {
            var isMajor = MathF.Abs(i % majorSpacing) < 0.001f;
            var isAxis = MathF.Abs(i) < 0.001f;

            Vector4 colorX, colorZ;
            if (isAxis)
            {
                colorX = GridColorAxisZ;
                colorZ = GridColorAxisX;
            }
            else
            {
                colorX = isMajor ? GridColorMajor : GridColorMinor;
                colorZ = isMajor ? GridColorMajor : GridColorMinor;
            }

            vertices.Add(new GizmoVertex(new Vector3(i, 0, -halfSize), colorX));
            vertices.Add(new GizmoVertex(new Vector3(i, 0, halfSize), colorX));

            vertices.Add(new GizmoVertex(new Vector3(-halfSize, 0, i), colorZ));
            vertices.Add(new GizmoVertex(new Vector3(halfSize, 0, i), colorZ));
        }

        _gridVertexCount = vertices.Count;

        if (_gridVertexCount == 0)
        {
            return;
        }

        var bufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * _gridVertexCount);
        _gridBuffer = GraphicsContext.Device.CreateBuffer(new BufferDesc
        {
            NumBytes = bufferSize,
            Usage = (uint)BufferUsageFlagBits.Vertex,
            HeapType = HeapType.Gpu,
            DebugName = StringView.Create("GridBuffer")
        });

        lock (GraphicsContext.TransferLock)
        {
            var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
            {
                Device = GraphicsContext.Device,
                IssueBarriers = false
            });
            batchCopy.Begin();

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
                    DstBuffer = _gridBuffer,
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

    public void BuildSceneIcons(Scene scene, CameraComponent editorCamera, GameObject? selectedObject)
    {
        if (!ShowSceneIcons)
        {
            return;
        }

        _sceneIconVertices.Clear();

        var iconScale = 1.0f;

        // Add camera icons (skip the editor camera)
        foreach (var obj in scene.RootObjects)
        {
            BuildSceneIconsRecursive(obj, editorCamera, selectedObject, iconScale);
        }

        // Also add icons for root-level lights (they're GameObjects, not components)
        foreach (var obj in scene.RootObjects)
        {
            AddLightIconIfApplicable(obj, selectedObject, iconScale);
        }

        // Build the vertex buffer if we have vertices
        if (_sceneIconVertices.Count > 0)
        {
            BuildSceneIconBuffer();
        }
        else
        {
            _sceneIconVertexCount = 0;
        }
    }

    private void BuildSceneIconsRecursive(GameObject obj, CameraComponent editorCamera, GameObject? selectedObject, float scale)
    {
        // Skip if this is the selected object (it has its own gizmo)
        var isSelected = obj == selectedObject;

        // Check for camera component
        var cameraComp = obj.GetComponent<CameraComponent>();
        if (cameraComp != null && cameraComp != editorCamera)
        {
            var color = isSelected ? GizmoGeometry.HighlightColor : GizmoGeometry.CameraIconColor;
            GizmoGeometry.BuildCameraIcon(_sceneIconVertices, obj.WorldPosition, obj.LocalRotation, scale, color);
        }

        // Recurse to children
        foreach (var child in obj.Children)
        {
            BuildSceneIconsRecursive(child, editorCamera, selectedObject, scale);
            AddLightIconIfApplicable(child, selectedObject, scale);
        }
    }

    private void AddLightIconIfApplicable(GameObject obj, GameObject? selectedObject, float scale)
    {
        var isSelected = obj == selectedObject;

        if (obj is DirectionalLight directional)
        {
            var color = isSelected ? GizmoGeometry.HighlightColor : GizmoGeometry.DirectionalLightIconColor;
            GizmoGeometry.BuildDirectionalLightIcon(_sceneIconVertices, directional.WorldPosition, directional.Direction, scale, color);
        }
        else if (obj is PointLight point)
        {
            var color = isSelected ? GizmoGeometry.HighlightColor : GizmoGeometry.PointLightIconColor;
            GizmoGeometry.BuildPointLightIcon(_sceneIconVertices, point.WorldPosition, scale, color);
        }
        else if (obj is SpotLight spot)
        {
            var color = isSelected ? GizmoGeometry.HighlightColor : GizmoGeometry.SpotLightIconColor;
            GizmoGeometry.BuildSpotLightIcon(_sceneIconVertices, spot.WorldPosition, spot.Direction, spot.OuterConeAngle, scale, color);
        }
    }

    private void BuildSceneIconBuffer()
    {
        _sceneIconBuffer?.Dispose();

        var vertexCount = _sceneIconVertices.Count;
        if (vertexCount == 0)
        {
            _sceneIconBuffer = null;
            _sceneIconVertexCount = 0;
            return;
        }

        var bufferSize = (uint)(Marshal.SizeOf<GizmoVertex>() * vertexCount);
        _sceneIconBuffer = GraphicsContext.Device.CreateBuffer(new BufferDesc
        {
            NumBytes = bufferSize,
            Usage = (uint)BufferUsageFlagBits.Vertex,
            HeapType = HeapType.Gpu,
            DebugName = StringView.Create("SceneIconBuffer")
        });

        lock (GraphicsContext.TransferLock)
        {
            var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
            {
                Device = GraphicsContext.Device,
                IssueBarriers = false
            });
            batchCopy.Begin();

            var vertices = _sceneIconVertices.ToArray();
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
                    DstBuffer = _sceneIconBuffer,
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

        _sceneIconVertexCount = vertexCount;
    }

    public void Render(GraphicsPass pass, ViewData viewData, CycledTexture sceneDepth)
    {
        var frameIndex = GraphicsContext.FrameIndex;
        var depthTexture = sceneDepth[frameIndex];
        UpdateBindGroups(frameIndex, depthTexture);

        var vertexStride = (uint)Marshal.SizeOf<GizmoVertex>();

        if (ShowGrid && _gridBuffer != null && _gridVertexCount > 0)
        {
            BuildGridBuffer();
            UpdateGridConstants(viewData);
            pass.BindPipeline(_shaders.GridPipeline);
            pass.Bind(_gridBindGroups[frameIndex]);
            pass.BindVertexBuffer(_gridBuffer, 0, vertexStride);
            pass.Draw((uint)_gridVertexCount, 1, 0, 0);
        }

        if (ShowSceneIcons && _sceneIconBuffer != null && _sceneIconVertexCount > 0)
        {
            UpdateSceneIconConstants(viewData);
            pass.BindPipeline(_shaders.TrianglePipeline);
            pass.Bind(_sceneIconBindGroups[frameIndex]);
            pass.BindVertexBuffer(_sceneIconBuffer, 0, vertexStride);
            pass.Draw((uint)_sceneIconVertexCount, 1, 0, 0);
        }

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

    private void UpdateGridConstants(ViewData viewData)
    {
        var camera = viewData.Camera ?? viewData.Scene?.GetActiveCamera();
        var viewProjection = camera?.ViewProjectionMatrix ?? Matrix4x4.Identity;
        var cameraPosition = camera?.WorldPosition ?? Vector3.Zero;

        var constants = new GizmoConstants
        {
            ViewProjection = viewProjection,
            ModelMatrix = GridOrientation,
            CameraPosition = cameraPosition,
            DepthBias = 0.0f,
            SelectionColor = GridColorMajor,
            Opacity = 1f,
            Time = viewData.TotalTime,
            ScreenSize = new Vector2(GraphicsContext.Width, GraphicsContext.Height)
        };

        _gridConstantBuffer.Write(in constants);
    }

    private void UpdateSceneIconConstants(ViewData viewData)
    {
        var camera = viewData.Camera ?? viewData.Scene?.GetActiveCamera();
        var viewProjection = camera?.ViewProjectionMatrix ?? Matrix4x4.Identity;
        var cameraPosition = camera?.WorldPosition ?? Vector3.Zero;

        var constants = new GizmoConstants
        {
            ViewProjection = viewProjection,
            ModelMatrix = Matrix4x4.Identity,
            CameraPosition = cameraPosition,
            DepthBias = 0.0001f,
            SelectionColor = GizmoGeometry.CameraIconColor,
            Opacity = 1f,
            Time = viewData.TotalTime,
            ScreenSize = new Vector2(GraphicsContext.Width, GraphicsContext.Height)
        };

        _sceneIconConstantBuffer.Write(in constants);
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

        _gridBindGroups[frameIndex].BeginUpdate();
        _gridBindGroups[frameIndex].Cbv(0, _gridConstantBuffer[frameIndex]);
        _gridBindGroups[frameIndex].SrvTexture(0, depthTexture);
        _gridBindGroups[frameIndex].EndUpdate();

        _sceneIconBindGroups[frameIndex].BeginUpdate();
        _sceneIconBindGroups[frameIndex].Cbv(0, _sceneIconConstantBuffer[frameIndex]);
        _sceneIconBindGroups[frameIndex].SrvTexture(0, depthTexture);
        _sceneIconBindGroups[frameIndex].EndUpdate();

        _boundDepthTextures[frameIndex] = depthTexture;
    }

    public void Dispose()
    {
        for (var m = 0; m < NumModes; m++)
        {
            for (var a = 0; a < NumAxes; a++)
            {
                _gizmoBuffers[m, a]?.Dispose();
            }
        }

        _unitCubeBuffer.Dispose();
        _gridBuffer?.Dispose();
        _sceneIconBuffer?.Dispose();

        foreach (var bindGroup in _gizmoBindGroups)
        {
            bindGroup.Dispose();
        }

        foreach (var bindGroup in _selectionBindGroups)
        {
            bindGroup.Dispose();
        }

        foreach (var bindGroup in _gridBindGroups)
        {
            bindGroup.Dispose();
        }

        foreach (var bindGroup in _sceneIconBindGroups)
        {
            bindGroup.Dispose();
        }

        _gizmoConstantBuffer.Dispose();
        _selectionConstantBuffer.Dispose();
        _gridConstantBuffer.Dispose();
        _sceneIconConstantBuffer.Dispose();
        _shaders.Dispose();
    }
}
