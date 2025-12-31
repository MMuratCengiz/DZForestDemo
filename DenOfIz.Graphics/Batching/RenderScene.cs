using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Graphics.Batching;

/// <summary>
/// Render-side mesh identifier. Matches RuntimeMeshHandle layout for zero-cost conversion.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct MeshId(uint Index, uint Generation)
{
    public static readonly MeshId Invalid = new(uint.MaxValue, 0);

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Index != uint.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MeshId(uint index) : this(index, 0) { }
}

/// <summary>
/// Render-side texture identifier. Matches RuntimeTextureHandle layout for zero-cost conversion.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct TextureId(uint Index, uint Generation)
{
    public static readonly TextureId Invalid = new(uint.MaxValue, 0);

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Index != uint.MaxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextureId(uint index) : this(index, 0) { }
}

/// <summary>
/// Stable handle to a render object. Survives across frames until explicitly removed.
/// Uses generational index pattern for safe reuse of slots.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct RenderObjectHandle(int Index, int Generation)
{
    public static readonly RenderObjectHandle Invalid = new(-1, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Index >= 0;
}

/// <summary>
/// Flags controlling render object behavior.
/// </summary>
[Flags]
public enum RenderFlags : byte
{
    None = 0,
    CastsShadow = 1 << 0,
    Skinned = 1 << 1,
    Transparent = 1 << 2,
    Static = 1 << 3,  // Hint: transform rarely changes
}

/// <summary>
/// Material data for rendering. Renderer-owned, no external dependency.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RenderMaterial
{
    public Vector4 BaseColor;
    public float Metallic;
    public float Roughness;
    public float AmbientOcclusion;
    public TextureId AlbedoTexture;

    public static readonly RenderMaterial Default = new()
    {
        BaseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
        Metallic = 0.0f,
        Roughness = 0.5f,
        AmbientOcclusion = 1.0f,
        AlbedoTexture = TextureId.Invalid
    };
}

/// <summary>
/// Description for creating a render object.
/// </summary>
public struct RenderObjectDesc
{
    public MeshId Mesh;
    public Matrix4x4 Transform;
    public RenderMaterial Material;
    public RenderFlags Flags;
}

/// <summary>
/// View/camera data for rendering.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RenderView
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public Vector3 Position;
    public float NearPlane;
    public float FarPlane;
    public float FieldOfView;
    private float _padding;
}

/// <summary>
/// Light types supported by the renderer.
/// </summary>
public enum LightType : byte
{
    Directional = 0,
    Point = 1,
    Spot = 2,
}

/// <summary>
/// Light data for rendering.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RenderLight
{
    public Vector3 Position;
    public LightType Type;
    public Vector3 Direction;
    public float Range;
    public Vector3 Color;
    public float Intensity;
    public float SpotInnerAngle;
    public float SpotOuterAngle;
    public bool CastsShadows;
    private byte _padding1;
    private byte _padding2;
    private byte _padding3;
}

/// <summary>
/// Internal storage for a render object.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RenderObjectData
{
    public MeshId Mesh;
    public Matrix4x4 Transform;
    public Matrix4x4 PreviousTransform;
    public RenderMaterial Material;
    public RenderFlags Flags;
    public byte Dirty;  // 0 = clean, 1 = dirty
    public short Generation;

    // Skinning data index (-1 if not skinned)
    public int SkinningDataIndex;
}

/// <summary>
/// Batch of instances sharing the same mesh, ready for instanced rendering.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct RenderBatch
{
    public readonly MeshId Mesh;
    public readonly int StartIndex;
    public readonly int Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal RenderBatch(MeshId mesh, int startIndex, int count)
    {
        Mesh = mesh;
        StartIndex = startIndex;
        Count = count;
    }
}

/// <summary>
/// Instance data ready for GPU upload.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RenderInstance
{
    public Matrix4x4 Transform;
    public Vector4 BaseColor;
    public float Metallic;
    public float Roughness;
    public float AmbientOcclusion;
    public int AlbedoTextureIndex;
}

/// <summary>
/// Skinned instance data - rendered individually (not batched).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct SkinnedRenderInstance
{
    public MeshId Mesh;
    public Matrix4x4 Transform;
    public RenderMaterial Material;
    public int BoneMatricesOffset;  // Offset into bone matrices buffer
    public int BoneCount;
}

/// <summary>
/// Double-buffered render scene for frame-parallel rendering.
/// Game thread writes to one buffer while render thread reads from the other.
/// Zero-allocation design - all buffers are pre-allocated.
/// </summary>
public sealed class RenderScene : IDisposable
{
    private const int DefaultMaxObjects = 16384;
    private const int DefaultMaxLights = 64;
    private const int DefaultMaxSkinnedObjects = 256;
    private const int MaxBonesPerObject = 128;

    // Double-buffered state
    private readonly FrameState[] _frames;
    private int _writeFrameIndex;
    private int _readFrameIndex;

    // Object storage (shared between frames, generation-protected)
    private readonly RenderObjectData[] _objects;
    private readonly int[] _freeList;
    private int _freeListHead;
    private int _objectCount;
    private readonly int _maxObjects;

    // Skinning data pool
    private readonly Matrix4x4[][] _boneMatricesPool;
    private readonly int[] _boneMatricesFreeList;
    private int _boneMatricesFreeHead;

    private bool _disposed;

    /// <summary>
    /// Per-frame state that gets double-buffered.
    /// </summary>
    private sealed class FrameState(int maxObjects, int maxLights, int maxSkinnedObjects)
    {
        // View data
        public RenderView MainView;
        public readonly RenderView[] ShadowViews = new RenderView[maxLights];
        public int ShadowViewCount;

        // Lights
        public readonly RenderLight[] Lights = new RenderLight[maxLights];
        public int LightCount;

        // Batching output (pre-allocated)
        public readonly RenderBatch[] StaticBatches = new RenderBatch[maxObjects];
        public int StaticBatchCount;
        public readonly RenderInstance[] StaticInstances = new RenderInstance[maxObjects];
        public int StaticInstanceCount;

        // Skinned objects (not batched)
        public readonly SkinnedRenderInstance[] SkinnedInstances = new SkinnedRenderInstance[maxSkinnedObjects];
        public int SkinnedInstanceCount;
        public readonly Matrix4x4[] SkinnedBoneMatrices = new Matrix4x4[maxSkinnedObjects * MaxBonesPerObject];
        public int SkinnedBoneMatricesCount;

        // Sorted indices for batching (avoid sorting objects array directly)
        public readonly int[] SortedIndices = new int[maxObjects];
        public int SortedCount;

        // Dirty tracking
        public bool NeedsRebatch = true;

        public void Reset()
        {
            ShadowViewCount = 0;
            LightCount = 0;
            NeedsRebatch = true;
        }
    }

    public RenderScene(int maxObjects = DefaultMaxObjects, int maxLights = DefaultMaxLights, int maxSkinnedObjects = DefaultMaxSkinnedObjects)
    {
        _maxObjects = maxObjects;
        _objects = new RenderObjectData[maxObjects];
        _freeList = new int[maxObjects];

        // Initialize free list
        for (int i = 0; i < maxObjects; i++)
        {
            _freeList[i] = i + 1;
            _objects[i].Generation = 0;
            _objects[i].SkinningDataIndex = -1;
        }
        _freeList[maxObjects - 1] = -1;  // End of list
        _freeListHead = 0;

        // Bone matrices pool
        _boneMatricesPool = new Matrix4x4[maxSkinnedObjects][];
        _boneMatricesFreeList = new int[maxSkinnedObjects];
        for (int i = 0; i < maxSkinnedObjects; i++)
        {
            _boneMatricesPool[i] = new Matrix4x4[MaxBonesPerObject];
            _boneMatricesFreeList[i] = i + 1;
        }
        _boneMatricesFreeList[maxSkinnedObjects - 1] = -1;
        _boneMatricesFreeHead = 0;

        // Double-buffered frame state
        _frames = new FrameState[2];
        _frames[0] = new FrameState(maxObjects, maxLights, maxSkinnedObjects);
        _frames[1] = new FrameState(maxObjects, maxLights, maxSkinnedObjects);

        _writeFrameIndex = 0;
        _readFrameIndex = 1;
    }

    /// <summary>
    /// Add a new render object to the scene.
    /// Thread-safe for game thread during write phase.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderObjectHandle Add(in RenderObjectDesc desc)
    {
        if (_freeListHead < 0)
        {
            return RenderObjectHandle.Invalid;  // Pool exhausted
        }

        int index = _freeListHead;
        _freeListHead = _freeList[index];

        ref var obj = ref _objects[index];
        obj.Mesh = desc.Mesh;
        obj.Transform = desc.Transform;
        obj.PreviousTransform = desc.Transform;
        obj.Material = desc.Material;
        obj.Flags = desc.Flags;
        obj.Dirty = 1;
        obj.Generation++;
        obj.SkinningDataIndex = -1;

        // Allocate skinning data if needed
        if ((desc.Flags & RenderFlags.Skinned) != 0 && _boneMatricesFreeHead >= 0)
        {
            obj.SkinningDataIndex = _boneMatricesFreeHead;
            _boneMatricesFreeHead = _boneMatricesFreeList[obj.SkinningDataIndex];
        }

        _objectCount++;
        _frames[_writeFrameIndex].NeedsRebatch = true;

        return new RenderObjectHandle(index, obj.Generation);
    }

    /// <summary>
    /// Remove a render object from the scene.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(RenderObjectHandle handle)
    {
        if (!IsValid(handle))
        {
            return;
        }

        ref var obj = ref _objects[handle.Index];

        // Return skinning data to pool
        if (obj.SkinningDataIndex >= 0)
        {
            _boneMatricesFreeList[obj.SkinningDataIndex] = _boneMatricesFreeHead;
            _boneMatricesFreeHead = obj.SkinningDataIndex;
            obj.SkinningDataIndex = -1;
        }

        // Invalidate and return to free list
        obj.Generation++;
        obj.Mesh = MeshId.Invalid;
        _freeList[handle.Index] = _freeListHead;
        _freeListHead = handle.Index;

        _objectCount--;
        _frames[_writeFrameIndex].NeedsRebatch = true;
    }

    /// <summary>
    /// Check if a handle is still valid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(RenderObjectHandle handle)
    {
        return handle.Index >= 0 &&
               handle.Index < _maxObjects &&
               _objects[handle.Index].Generation == handle.Generation &&
               _objects[handle.Index].Mesh.IsValid;
    }

    /// <summary>
    /// Update the transform of a render object.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTransform(RenderObjectHandle handle, in Matrix4x4 transform)
    {
        if (!IsValid(handle))
        {
            return;
        }

        ref var obj = ref _objects[handle.Index];
        obj.PreviousTransform = obj.Transform;
        obj.Transform = transform;
        obj.Dirty = 1;
        _frames[_writeFrameIndex].NeedsRebatch = true;
    }

    /// <summary>
    /// Update the material of a render object.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMaterial(RenderObjectHandle handle, in RenderMaterial material)
    {
        if (!IsValid(handle))
        {
            return;
        }

        ref var obj = ref _objects[handle.Index];
        obj.Material = material;
        obj.Dirty = 1;
        _frames[_writeFrameIndex].NeedsRebatch = true;
    }

    /// <summary>
    /// Update bone matrices for a skinned object.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBoneMatrices(RenderObjectHandle handle, ReadOnlySpan<Matrix4x4> matrices)
    {
        if (!IsValid(handle))
        {
            return;
        }

        ref var obj = ref _objects[handle.Index];
        if (obj.SkinningDataIndex < 0)
        {
            return;
        }

        var dest = _boneMatricesPool[obj.SkinningDataIndex].AsSpan();
        var copyCount = Math.Min(matrices.Length, MaxBonesPerObject);
        matrices.Slice(0, copyCount).CopyTo(dest);
        obj.Dirty = 1;
    }

    /// <summary>
    /// Get bone matrices for a skinned object (read access).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<Matrix4x4> GetBoneMatrices(RenderObjectHandle handle)
    {
        if (!IsValid(handle))
        {
            return ReadOnlySpan<Matrix4x4>.Empty;
        }

        ref var obj = ref _objects[handle.Index];
        if (obj.SkinningDataIndex < 0)
        {
            return ReadOnlySpan<Matrix4x4>.Empty;
        }

        return _boneMatricesPool[obj.SkinningDataIndex];
    }

    /// <summary>
    /// Set the main camera view.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMainView(in RenderView view)
    {
        _frames[_writeFrameIndex].MainView = view;
    }

    /// <summary>
    /// Clear all lights for this frame.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearLights()
    {
        _frames[_writeFrameIndex].LightCount = 0;
        _frames[_writeFrameIndex].ShadowViewCount = 0;
    }

    /// <summary>
    /// Add a light to the scene.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddLight(in RenderLight light)
    {
        ref var frame = ref _frames[_writeFrameIndex];
        if (frame.LightCount >= frame.Lights.Length)
        {
            return;
        }

        frame.Lights[frame.LightCount++] = light;
    }

    /// <summary>
    /// Add a shadow view (light's view for shadow mapping).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddShadowView(in RenderView view)
    {
        ref var frame = ref _frames[_writeFrameIndex];
        if (frame.ShadowViewCount >= frame.ShadowViews.Length)
        {
            return;
        }

        frame.ShadowViews[frame.ShadowViewCount++] = view;
    }

    /// <summary>
    /// Begin a new frame. Call at start of game update.
    /// For single-threaded rendering, we reuse the same frame buffer.
    /// </summary>
    public void BeginFrame()
    {
        // For single-threaded: just reset the current frame state
        // For multi-threaded: would need to swap _readFrameIndex/_writeFrameIndex
        _frames[_writeFrameIndex].Reset();
    }

    /// <summary>
    /// Commit the frame for rendering. Call after game update, before render.
    /// Builds batches if needed.
    /// </summary>
    public void CommitFrame()
    {
        ref var frame = ref _frames[_writeFrameIndex];

        if (frame.NeedsRebatch)
        {
            BuildBatches(ref frame);
            frame.NeedsRebatch = false;
        }

        // Clear dirty flags
        for (int i = 0; i < _maxObjects; i++)
        {
            _objects[i].Dirty = 0;
        }
    }

    /// <summary>
    /// Build batches for the current frame. Zero-allocation.
    /// </summary>
    private void BuildBatches(ref FrameState frame)
    {
        // First pass: collect valid static and skinned object indices
        int staticCount = 0;
        frame.SkinnedInstanceCount = 0;
        frame.SkinnedBoneMatricesCount = 0;

        for (int i = 0; i < _maxObjects; i++)
        {
            ref var obj = ref _objects[i];
            if (!obj.Mesh.IsValid)
            {
                continue;
            }

            if ((obj.Flags & RenderFlags.Skinned) != 0)
            {
                // Skinned object - add directly to skinned instances
                if (frame.SkinnedInstanceCount < frame.SkinnedInstances.Length)
                {
                    ref var skinned = ref frame.SkinnedInstances[frame.SkinnedInstanceCount];
                    skinned.Mesh = obj.Mesh;
                    skinned.Transform = obj.Transform;
                    skinned.Material = obj.Material;
                    skinned.BoneMatricesOffset = frame.SkinnedBoneMatricesCount;

                    // Copy bone matrices if available
                    if (obj.SkinningDataIndex >= 0)
                    {
                        var bones = _boneMatricesPool[obj.SkinningDataIndex];
                        int boneCount = Math.Min(bones.Length, MaxBonesPerObject);
                        skinned.BoneCount = boneCount;

                        var dest = frame.SkinnedBoneMatrices.AsSpan(frame.SkinnedBoneMatricesCount, boneCount);
                        bones.AsSpan(0, boneCount).CopyTo(dest);
                        frame.SkinnedBoneMatricesCount += boneCount;
                    }
                    else
                    {
                        skinned.BoneCount = 0;
                    }

                    frame.SkinnedInstanceCount++;
                }
            }
            else
            {
                // Static object - add to sort list
                frame.SortedIndices[staticCount++] = i;
            }
        }

        frame.SortedCount = staticCount;

        // Sort by mesh handle for batching (insertion sort - good for nearly-sorted data)
        SortByMesh(frame.SortedIndices.AsSpan(0, staticCount));

        // Build batches from sorted indices
        frame.StaticBatchCount = 0;
        frame.StaticInstanceCount = 0;

        if (staticCount == 0)
        {
            return;
        }

        int batchStart = 0;
        var currentMesh = _objects[frame.SortedIndices[0]].Mesh;

        for (int i = 0; i <= staticCount; i++)
        {
            bool endBatch = i == staticCount;
            MeshId nextMesh = default;

            if (!endBatch)
            {
                nextMesh = _objects[frame.SortedIndices[i]].Mesh;
                endBatch = nextMesh.Index != currentMesh.Index;
            }

            if (endBatch)
            {
                // Emit batch
                int batchCount = i - batchStart;
                frame.StaticBatches[frame.StaticBatchCount++] = new RenderBatch(
                    currentMesh,
                    frame.StaticInstanceCount,
                    batchCount);

                // Emit instances for this batch
                for (int j = batchStart; j < i; j++)
                {
                    ref var obj = ref _objects[frame.SortedIndices[j]];
                    ref var inst = ref frame.StaticInstances[frame.StaticInstanceCount++];
                    inst.Transform = obj.Transform;
                    inst.BaseColor = obj.Material.BaseColor;
                    inst.Metallic = obj.Material.Metallic;
                    inst.Roughness = obj.Material.Roughness;
                    inst.AmbientOcclusion = obj.Material.AmbientOcclusion;
                    inst.AlbedoTextureIndex = (int)obj.Material.AlbedoTexture.Index;
                }

                batchStart = i;
                currentMesh = nextMesh;
            }
        }
    }

    /// <summary>
    /// In-place insertion sort by mesh handle. Good for nearly-sorted data.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SortByMesh(Span<int> indices)
    {
        for (int i = 1; i < indices.Length; i++)
        {
            int key = indices[i];
            uint keyMesh = _objects[key].Mesh.Index;
            int j = i - 1;

            while (j >= 0 && _objects[indices[j]].Mesh.Index > keyMesh)
            {
                indices[j + 1] = indices[j];
                j--;
            }
            indices[j + 1] = key;
        }
    }

    // ==================== Read Access (Render Thread) ====================
    // NOTE: For single-threaded rendering, we read from _writeFrameIndex after CommitFrame().
    // For multi-threaded, this would need to be _readFrameIndex with proper synchronization.

    /// <summary>
    /// Get the main view for rendering.
    /// </summary>
    public ref readonly RenderView MainView
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _frames[_writeFrameIndex].MainView;
    }

    /// <summary>
    /// Get shadow views for shadow mapping.
    /// </summary>
    public ReadOnlySpan<RenderView> ShadowViews
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].ShadowViews.AsSpan(0, _frames[_writeFrameIndex].ShadowViewCount);
    }

    /// <summary>
    /// Get lights for rendering.
    /// </summary>
    public ReadOnlySpan<RenderLight> Lights
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].Lights.AsSpan(0, _frames[_writeFrameIndex].LightCount);
    }

    /// <summary>
    /// Get static batches for instanced rendering.
    /// </summary>
    public ReadOnlySpan<RenderBatch> StaticBatches
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].StaticBatches.AsSpan(0, _frames[_writeFrameIndex].StaticBatchCount);
    }

    /// <summary>
    /// Get static instances (GPU-ready data).
    /// </summary>
    public ReadOnlySpan<RenderInstance> StaticInstances
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].StaticInstances.AsSpan(0, _frames[_writeFrameIndex].StaticInstanceCount);
    }

    /// <summary>
    /// Get skinned instances (rendered individually).
    /// </summary>
    public ReadOnlySpan<SkinnedRenderInstance> SkinnedInstances
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].SkinnedInstances.AsSpan(0, _frames[_writeFrameIndex].SkinnedInstanceCount);
    }

    /// <summary>
    /// Get bone matrices for all skinned objects (packed).
    /// </summary>
    public ReadOnlySpan<Matrix4x4> SkinnedBoneMatrices
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].SkinnedBoneMatrices.AsSpan(0, _frames[_writeFrameIndex].SkinnedBoneMatricesCount);
    }

    /// <summary>
    /// Number of active render objects.
    /// </summary>
    public int ObjectCount => _objectCount;

    /// <summary>
    /// Number of static batches.
    /// </summary>
    public int StaticBatchCount => _frames[_writeFrameIndex].StaticBatchCount;

    /// <summary>
    /// Number of skinned instances.
    /// </summary>
    public int SkinnedInstanceCount => _frames[_writeFrameIndex].SkinnedInstanceCount;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
