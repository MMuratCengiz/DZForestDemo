using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DenOfIz;

namespace RuntimeAssets;

public readonly struct RuntimeMesh
{
    public readonly VertexBufferView VertexBuffer;
    public readonly IndexBufferView IndexBuffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RuntimeMesh(VertexBufferView vertexBuffer, IndexBufferView indexBuffer)
    {
        VertexBuffer = vertexBuffer;
        IndexBuffer = indexBuffer;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => VertexBuffer.IsValid && IndexBuffer.IsValid;
    }
}

public sealed class RuntimeMeshStore : IDisposable
{
    private readonly LogicalDevice _device;
    private readonly BufferPool _vertexPool;
    private readonly BufferPool _indexPool;
    private readonly List<MeshSlot> _slots = [];
    private readonly Queue<uint> _freeIndices = new();
    private bool _disposed;

    public RuntimeMeshStore(LogicalDevice device, ulong vertexPoolSize = 64 * 1024 * 1024, ulong indexPoolSize = 32 * 1024 * 1024)
    {
        _device = device;
        _vertexPool = new BufferPool(device, (uint)ResourceUsageFlagBits.VertexAndConstantBuffer, vertexPoolSize);
        _indexPool = new BufferPool(device, (uint)ResourceUsageFlagBits.IndexBuffer, indexPoolSize);
    }

    public RuntimeMeshHandle Add(MeshData meshData, BatchResourceCopy batchCopy)
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

        return AddRaw(allVertices.ToArray(), allIndices.ToArray(), batchCopy);
    }

    public RuntimeMeshHandle AddRaw(Vertex[] vertices, uint[] indices, BatchResourceCopy batchCopy)
    {
        var vertexStride = (uint)Unsafe.SizeOf<Vertex>();
        var vertexSize = (ulong)(vertices.Length * vertexStride);
        var indexSize = (ulong)(indices.Length * sizeof(uint));

        var vertexView = _vertexPool.Allocate(vertexSize, vertexStride);
        var indexView = _indexPool.Allocate(indexSize, sizeof(uint));

        if (!vertexView.IsValid || !indexView.IsValid)
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
        vertexHandle.Free();

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
        indexHandle.Free();

        var mesh = new RuntimeMesh(
            new VertexBufferView(vertexView, vertexStride, (uint)vertices.Length),
            new IndexBufferView(indexView, IndexType.Uint32, (uint)indices.Length)
        );

        return AllocateSlot(mesh);
    }

    public RuntimeMeshHandle AddGeometry(GeometryData geometry, BatchResourceCopy batchCopy)
    {
        var vertexCount = geometry.GetVertexCount();
        var indexCount = geometry.GetIndexCount();
        var vertexStride = (uint)Unsafe.SizeOf<GeometryVertexData>();
        var vertexSize = (ulong)(vertexCount * vertexStride);
        var indexSize = (ulong)(indexCount * sizeof(uint));

        var vertexView = _vertexPool.Allocate(vertexSize, vertexStride);
        var indexView = _indexPool.Allocate(indexSize, sizeof(uint));

        if (!vertexView.IsValid || !indexView.IsValid)
        {
            return RuntimeMeshHandle.Invalid;
        }

        var vertexData = new byte[vertexSize];
        var indexData = new byte[indexSize];

        geometry.GetVertexData(vertexData);
        geometry.GetIndexData(indexData);

        var vertexHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
        batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
        {
            Data = new ByteArrayView
            {
                Elements = vertexHandle.AddrOfPinnedObject(),
                NumElements = (ulong)vertexData.Length
            },
            DstBuffer = vertexView.Buffer,
            DstBufferOffset = vertexView.Offset
        });
        vertexHandle.Free();

        var indexHandle = GCHandle.Alloc(indexData, GCHandleType.Pinned);
        batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
        {
            Data = new ByteArrayView
            {
                Elements = indexHandle.AddrOfPinnedObject(),
                NumElements = (ulong)indexData.Length
            },
            DstBuffer = indexView.Buffer,
            DstBufferOffset = indexView.Offset
        });
        indexHandle.Free();

        var mesh = new RuntimeMesh(
            new VertexBufferView(vertexView, vertexStride, vertexCount),
            new IndexBufferView(indexView, IndexType.Uint32, indexCount)
        );

        return AllocateSlot(mesh);
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _vertexPool.Dispose();
        _indexPool.Dispose();
        _slots.Clear();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MeshSlot
    {
        public RuntimeMesh Mesh;
        public uint Generation;
        public bool IsOccupied;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MeshSlot(RuntimeMesh mesh, uint generation, bool isOccupied)
        {
            Mesh = mesh;
            Generation = generation;
            IsOccupied = isOccupied;
        }
    }
}
