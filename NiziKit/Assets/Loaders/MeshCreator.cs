using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Graphics.Binding;

namespace NiziKit.Assets.Loaders;

internal static class MeshCreator
{
    private static readonly MeshLoader DzMeshLoader = new();

    public static Mesh LoadDzMesh(string path, LogicalDevice device, BufferPool vertexPool, BufferPool indexPool)
    {
        var resolvedPath = AssetPaths.ResolveMesh(path);
        var result = DzMeshLoader.Load(resolvedPath);

        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to load mesh: {result.ErrorMessage}");
        }

        return CreateMeshFromData(
            Path.GetFileNameWithoutExtension(path),
            result.Vertices!,
            result.Indices!,
            result.MeshType,
            result.Material?.Name != null ? 0 : -1,
            device,
            vertexPool,
            indexPool
        );
    }

    public static Mesh CreateFromGeometry(string name, GeometryData geometry, LogicalDevice device, BufferPool vertexPool, BufferPool indexPool)
    {
        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = false
        });
        batchCopy.Begin();

        var vertexBuffer = batchCopy.CreateGeometryVertexBuffer(geometry);
        var indexBuffer = batchCopy.CreateGeometryIndexBuffer(geometry);

        batchCopy.Submit(null);
        batchCopy.Dispose();

        var vertexCount = geometry.GetVertexCount();
        var indexCount = geometry.GetIndexCount();

        var vertexStride = (uint)Marshal.SizeOf<Vertex>();
        var vertexView = new VertexBufferView(
            new GpuBufferView { Buffer = vertexBuffer, Offset = 0, NumBytes = vertexCount * vertexStride },
            vertexStride,
            vertexCount
        );

        var indexView = new IndexBufferView(
            new GpuBufferView { Buffer = indexBuffer, Offset = 0, NumBytes = indexCount * sizeof(uint) },
            IndexType.Uint32,
            indexCount
        );

        return new Mesh
        {
            Name = name,
            VertexCount = (int)vertexCount,
            IndexCount = (int)indexCount,
            MeshType = MeshType.Static,
            MaterialIndex = -1,
            VertexBuffer = vertexView,
            IndexBuffer = indexView
        };
    }

    public static Mesh CreateMeshFromData(
        string name,
        Vertex[] vertices,
        uint[] indices,
        MeshType meshType,
        int materialIndex,
        LogicalDevice device,
        BufferPool vertexPool,
        BufferPool indexPool)
    {
        var bounds = BoundingBox.FromVertices(vertices);
        var vertexBufferView = UploadVertices(vertices, device, vertexPool);
        var indexBufferView = UploadIndices(indices, device, indexPool);

        return new Mesh
        {
            Name = name,
            VertexCount = vertices.Length,
            IndexCount = indices.Length,
            Bounds = bounds,
            MeshType = meshType,
            MaterialIndex = materialIndex,
            VertexBuffer = vertexBufferView,
            IndexBuffer = indexBufferView
        };
    }

    private static VertexBufferView UploadVertices(Vertex[] vertices, LogicalDevice device, BufferPool vertexPool)
    {
        var stride = (uint)Marshal.SizeOf<Vertex>();
        var numBytes = stride * (uint)vertices.Length;
        var gpuView = vertexPool.Allocate(numBytes);

        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
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
                    NumElements = numBytes
                },
                DstBuffer = gpuView.Buffer,
                DstBufferOffset = gpuView.Offset
            });
        }
        finally
        {
            handle.Free();
        }

        batchCopy.Submit(null);
        batchCopy.Dispose();

        return new VertexBufferView(gpuView, stride, (uint)vertices.Length);
    }

    private static IndexBufferView UploadIndices(uint[] indices, LogicalDevice device, BufferPool indexPool)
    {
        var numBytes = sizeof(uint) * (uint)indices.Length;
        var gpuView = indexPool.Allocate(numBytes);

        var batchCopy = new BatchResourceCopy(new BatchResourceCopyDesc
        {
            Device = device,
            IssueBarriers = false
        });
        batchCopy.Begin();

        var handle = GCHandle.Alloc(indices, GCHandleType.Pinned);
        try
        {
            batchCopy.CopyToGPUBuffer(new CopyToGpuBufferDesc
            {
                Data = new ByteArrayView
                {
                    Elements = handle.AddrOfPinnedObject(),
                    NumElements = numBytes
                },
                DstBuffer = gpuView.Buffer,
                DstBufferOffset = gpuView.Offset
            });
        }
        finally
        {
            handle.Free();
        }

        batchCopy.Submit(null);
        batchCopy.Dispose();

        return new IndexBufferView(gpuView, IndexType.Uint32, (uint)indices.Length);
    }
}
