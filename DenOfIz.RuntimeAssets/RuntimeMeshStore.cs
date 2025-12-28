using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;

namespace RuntimeAssets;

public readonly struct RuntimeMesh
{
    public readonly VertexBufferView VertexBuffer;
    public readonly IndexBufferView IndexBuffer;
    public readonly MeshType MeshType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeMesh(VertexBufferView vertexBuffer, IndexBufferView indexBuffer, MeshType meshType)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
        MeshType = meshType;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => VertexBuffer.IsValid && IndexBuffer.IsValid;
    }
}

public sealed class RuntimeMeshStore(
    LogicalDevice device,
    ulong vertexPoolSize = 64 * 1024 * 1024,
    ulong indexPoolSize = 32 * 1024 * 1024)
    : IDisposable
{
    private readonly LogicalDevice _device = device;
    private readonly Queue<uint> _freeIndices = new();
    private readonly BufferPool _indexPool = new(device, (uint)(BufferUsageFlagBits.Index | BufferUsageFlagBits.CopyDst), indexPoolSize);
    private readonly List<MeshSlot> _slots = [];
    private readonly List<GCHandle> _pendingHandles = [];

    private readonly BufferPool _vertexPool =
        new(device, (uint)(BufferUsageFlagBits.Vertex | BufferUsageFlagBits.CopyDst), vertexPoolSize);

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ReleasePendingHandles();
        _vertexPool.Dispose();
        _indexPool.Dispose();
        _slots.Clear();
    }

    public void ReleasePendingHandles()
    {
        foreach (var handle in _pendingHandles)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
        _pendingHandles.Clear();
    }

    public RuntimeMeshHandle Add(MeshData meshData, BatchResourceCopy batchCopy, MeshType meshType = MeshType.Static)
    {
        var allVertices = new List<Vertex>();
        var allIndices = new List<uint>();

        foreach (var primitive in meshData.Primitives)
        {
            var baseVertex = (uint)allVertices.Count;
            allVertices.AddRange(primitive.Vertices);

            foreach (var index in primitive.Indices)
            {
                allIndices.Add(index + baseVertex);
            }
        }

        return AddRaw(allVertices.ToArray(), allIndices.ToArray(), batchCopy, meshType);
    }

    public RuntimeMeshHandle AddRaw(Vertex[] vertices, uint[] indices, BatchResourceCopy batchCopy, MeshType meshType = MeshType.Static)
    {
        var vertexStride = (uint)Unsafe.SizeOf<Vertex>();
        var vertexSize = (ulong)(vertices.Length * vertexStride);
        var indexSize = (ulong)(indices.Length * sizeof(uint));

        const ulong minAlignment = 256;
        var vertexAlignment = Math.Max((ulong)vertexStride, minAlignment);
        var indexAlignment = minAlignment;

        var vertexView = _vertexPool.Allocate(vertexSize, vertexAlignment);
        var indexView = _indexPool.Allocate(indexSize, indexAlignment);

        if (vertexView.Buffer == 0 || indexView.Buffer == 0)
        {
            return RuntimeMeshHandle.Invalid;
        }

        var vertexHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
        batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
        {
            Data = new ByteArrayView
            {
                Elements = vertexHandle.AddrOfPinnedObject(),
                NumElements = vertexSize
            },
            DstBuffer = vertexView.Buffer,
            DstBufferOffset = vertexView.Offset
        });
        _pendingHandles.Add(vertexHandle);

        var indexHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
        batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
        {
            Data = new ByteArrayView
            {
                Elements = indexHandle.AddrOfPinnedObject(),
                NumElements = indexSize
            },
            DstBuffer = indexView.Buffer,
            DstBufferOffset = indexView.Offset
        });
        _pendingHandles.Add(indexHandle);

        var mesh = new RuntimeMesh(
            new VertexBufferView(vertexView, vertexStride, (uint)vertices.Length),
            new IndexBufferView(indexView, IndexType.Uint32, (uint)indices.Length),
            meshType
        );

        return AllocateSlot(mesh);
    }

    public unsafe RuntimeMeshHandle AddGeometry(GeometryData geometry, BatchResourceCopy batchCopy)
    {
        var vertexCount = (int)geometry.GetVertexCount();
        var indexCount = (int)geometry.GetIndexCount();

        // Read geometry vertex data (32 bytes per vertex: Position, Normal, TextureCoordinate)
        var geometryStride = Unsafe.SizeOf<GeometryVertexData>();
        var geometryData = new byte[vertexCount * geometryStride];
        geometry.GetVertexData(geometryData);

        // Convert to full Vertex format (80 bytes per vertex)
        var vertices = new Vertex[vertexCount];
        fixed (byte* srcPtr = geometryData)
        {
            var geoPtr = (GeometryVertexData*)srcPtr;
            for (var i = 0; i < vertexCount; i++)
            {
                ref var geo = ref geoPtr[i];
                vertices[i] = new Vertex
                {
                    Position = new System.Numerics.Vector3(geo.Position.X, geo.Position.Y, geo.Position.Z),
                    Normal = new System.Numerics.Vector3(geo.Normal.X, geo.Normal.Y, geo.Normal.Z),
                    TexCoord = new System.Numerics.Vector2(geo.TextureCoordinate.U, geo.TextureCoordinate.V),
                    Tangent = new System.Numerics.Vector4(1, 0, 0, 1), // Default tangent
                    BoneWeights = System.Numerics.Vector4.Zero,
                    BoneIndices = default
                };
            }
        }

        // Read indices
        var indexData = new byte[indexCount * sizeof(uint)];
        geometry.GetIndexData(indexData);
        var indices = new uint[indexCount];
        fixed (byte* srcPtr = indexData)
        fixed (uint* dstPtr = indices)
        {
            System.Buffer.MemoryCopy(srcPtr, dstPtr, indexCount * sizeof(uint), indexCount * sizeof(uint));
        }

        return AddRaw(vertices, indices, batchCopy, MeshType.Static);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(RuntimeMeshHandle handle, out RuntimeMesh mesh)
    {
        var slots = CollectionsMarshal.AsSpan(_slots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            mesh = default;
            return false;
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            mesh = default;
            return false;
        }

        mesh = slot.Mesh;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly RuntimeMesh GetRef(RuntimeMeshHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_slots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            return ref Unsafe.NullRef<RuntimeMesh>();
        }

        ref readonly var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            return ref Unsafe.NullRef<RuntimeMesh>();
        }

        return ref slot.Mesh;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeMesh Get(RuntimeMeshHandle handle)
    {
        if (!TryGet(handle, out var mesh))
        {
            ThrowInvalidHandle();
        }

        return mesh;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowInvalidHandle()
    {
        throw new InvalidOperationException("Invalid mesh handle.");
    }

    public void Remove(RuntimeMeshHandle handle)
    {
        var slots = CollectionsMarshal.AsSpan(_slots);
        var index = (int)handle.Index;

        if (!handle.IsValid || index >= slots.Length)
        {
            return;
        }

        ref var slot = ref slots[index];
        if (slot.Generation != handle.Generation || !slot.IsOccupied)
        {
            return;
        }

        slot = new MeshSlot(default, slot.Generation + 1, false);
        _freeIndices.Enqueue(handle.Index);
    }

    private RuntimeMeshHandle AllocateSlot(RuntimeMesh mesh)
    {
        if (_freeIndices.TryDequeue(out var freeIndex))
        {
            var slots = CollectionsMarshal.AsSpan(_slots);
            ref var slot = ref slots[(int)freeIndex];
            var newGeneration = slot.Generation + 1;
            slot = new MeshSlot(mesh, newGeneration, true);
            return new RuntimeMeshHandle(freeIndex, newGeneration);
        }

        var index = (uint)_slots.Count;
        const uint initialGeneration = 1;
        _slots.Add(new MeshSlot(mesh, initialGeneration, true));
        return new RuntimeMeshHandle(index, initialGeneration);
    }

    [StructLayout(LayoutKind.Sequential)]
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    private struct MeshSlot(RuntimeMesh mesh, uint generation, bool isOccupied)
    {
        public RuntimeMesh Mesh = mesh;
        public uint Generation = generation;
        public bool IsOccupied = isOccupied;
    }
}