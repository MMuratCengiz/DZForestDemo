using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Graphics.Batching;

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

[StructLayout(LayoutKind.Sequential)]
public readonly record struct RenderObjectHandle(int Index, int Generation)
{
    public static readonly RenderObjectHandle Invalid = new(-1, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Index >= 0;
}

[Flags]
public enum RenderFlags : byte
{
    None = 0,
    CastsShadow = 1 << 0,
    Skinned = 1 << 1,
    Transparent = 1 << 2,
    Static = 1 << 3,  // Hint: transform rarely changes
}

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

public struct RenderObjectDesc
{
    public MeshId Mesh;
    public Matrix4x4 Transform;
    public RenderMaterial Material;
    public RenderFlags Flags;
}

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

public enum LightType : byte
{
    Directional = 0,
    Point = 1,
    Spot = 2,
}

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

[StructLayout(LayoutKind.Sequential)]
public struct SkinnedRenderInstance
{
    public MeshId Mesh;
    public Matrix4x4 Transform;
    public RenderMaterial Material;
    public int BoneMatricesOffset;  // Offset into bone matrices buffer
    public int BoneCount;
}

public sealed class RenderScene : IDisposable
{
    private const int DefaultMaxObjects = 16384;
    private const int DefaultMaxLights = 64;
    private const int DefaultMaxSkinnedObjects = 256;
    private const int MaxBonesPerObject = 128;

    private readonly FrameState[] _frames;
    private int _writeFrameIndex;
    private int _readFrameIndex;

    private readonly RenderObjectData[] _objects;
    private readonly int[] _freeList;
    private int _freeListHead;
    private int _objectCount;
    private readonly int _maxObjects;
    private readonly Matrix4x4[][] _boneMatricesPool;
    private readonly int[] _boneMatricesFreeList;
    private int _boneMatricesFreeHead;

    private bool _disposed;

    private sealed class FrameState(int maxObjects, int maxLights, int maxSkinnedObjects)
    {
        public RenderView MainView;
        public readonly RenderView[] ShadowViews = new RenderView[maxLights];
        public int ShadowViewCount;

        public readonly RenderLight[] Lights = new RenderLight[maxLights];
        public int LightCount;

        public readonly RenderBatch[] StaticBatches = new RenderBatch[maxObjects];
        public int StaticBatchCount;
        public readonly RenderInstance[] StaticInstances = new RenderInstance[maxObjects];
        public int StaticInstanceCount;

        public readonly SkinnedRenderInstance[] SkinnedInstances = new SkinnedRenderInstance[maxSkinnedObjects];
        public int SkinnedInstanceCount;
        public readonly Matrix4x4[] SkinnedBoneMatrices = new Matrix4x4[maxSkinnedObjects * MaxBonesPerObject];
        public int SkinnedBoneMatricesCount;

        public readonly int[] SortedIndices = new int[maxObjects];
        public int SortedCount;

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

        for (var i = 0; i < maxObjects; i++)
        {
            _freeList[i] = i + 1;
            _objects[i].Generation = 0;
            _objects[i].SkinningDataIndex = -1;
        }
        _freeList[maxObjects - 1] = -1;  // End of list
        _freeListHead = 0;

        _boneMatricesPool = new Matrix4x4[maxSkinnedObjects][];
        _boneMatricesFreeList = new int[maxSkinnedObjects];
        for (var i = 0; i < maxSkinnedObjects; i++)
        {
            _boneMatricesPool[i] = new Matrix4x4[MaxBonesPerObject];
            _boneMatricesFreeList[i] = i + 1;
        }
        _boneMatricesFreeList[maxSkinnedObjects - 1] = -1;
        _boneMatricesFreeHead = 0;

        _frames = new FrameState[2];
        _frames[0] = new FrameState(maxObjects, maxLights, maxSkinnedObjects);
        _frames[1] = new FrameState(maxObjects, maxLights, maxSkinnedObjects);

        _writeFrameIndex = 0;
        _readFrameIndex = 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderObjectHandle Add(in RenderObjectDesc desc)
    {
        if (_freeListHead < 0)
        {
            return RenderObjectHandle.Invalid;  // Pool exhausted
        }

        var index = _freeListHead;
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid(RenderObjectHandle handle)
    {
        return handle.Index >= 0 &&
               handle.Index < _maxObjects &&
               _objects[handle.Index].Generation == handle.Generation &&
               _objects[handle.Index].Mesh.IsValid;
    }
    
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
        matrices[..copyCount].CopyTo(dest);
        obj.Dirty = 1;
    }
    
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMainView(in RenderView view)
    {
        _frames[_writeFrameIndex].MainView = view;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearLights()
    {
        _frames[_writeFrameIndex].LightCount = 0;
        _frames[_writeFrameIndex].ShadowViewCount = 0;
    }
    
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
    
    public void BeginFrame()
    {
        _frames[_writeFrameIndex].Reset();
    }
    
    public void CommitFrame()
    {
        ref var frame = ref _frames[_writeFrameIndex];

        if (frame.NeedsRebatch)
        {
            BuildBatches(ref frame);
            frame.NeedsRebatch = false;
        }
        for (var i = 0; i < _maxObjects; i++)
        {
            _objects[i].Dirty = 0;
        }
    }
    
    private void BuildBatches(ref FrameState frame)
    {
        var staticCount = 0;
        frame.SkinnedInstanceCount = 0;
        frame.SkinnedBoneMatricesCount = 0;

        for (var i = 0; i < _maxObjects; i++)
        {
            ref var obj = ref _objects[i];
            if (!obj.Mesh.IsValid)
            {
                continue;
            }

            if ((obj.Flags & RenderFlags.Skinned) != 0)
            {
                if (frame.SkinnedInstanceCount < frame.SkinnedInstances.Length)
                {
                    ref var skinned = ref frame.SkinnedInstances[frame.SkinnedInstanceCount];
                    skinned.Mesh = obj.Mesh;
                    skinned.Transform = obj.Transform;
                    skinned.Material = obj.Material;
                    skinned.BoneMatricesOffset = frame.SkinnedBoneMatricesCount;
                    if (obj.SkinningDataIndex >= 0)
                    {
                        var bones = _boneMatricesPool[obj.SkinningDataIndex];
                        var boneCount = Math.Min(bones.Length, MaxBonesPerObject);
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
                frame.SortedIndices[staticCount++] = i;
            }
        }

        frame.SortedCount = staticCount;
        SortByMesh(frame.SortedIndices.AsSpan(0, staticCount));

        frame.StaticBatchCount = 0;
        frame.StaticInstanceCount = 0;

        if (staticCount == 0)
        {
            return;
        }

        var batchStart = 0;
        var currentMesh = _objects[frame.SortedIndices[0]].Mesh;

        for (var i = 0; i <= staticCount; i++)
        {
            var endBatch = i == staticCount;
            MeshId nextMesh = default;

            if (!endBatch)
            {
                nextMesh = _objects[frame.SortedIndices[i]].Mesh;
                endBatch = nextMesh.Index != currentMesh.Index;
            }

            if (endBatch)
            {
                // Emit batch
                var batchCount = i - batchStart;
                frame.StaticBatches[frame.StaticBatchCount++] = new RenderBatch(
                    currentMesh,
                    frame.StaticInstanceCount,
                    batchCount);

                // Emit instances for this batch
                for (var j = batchStart; j < i; j++)
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
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SortByMesh(Span<int> indices)
    {
        for (var i = 1; i < indices.Length; i++)
        {
            var key = indices[i];
            var keyMesh = _objects[key].Mesh.Index;
            var j = i - 1;

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
    public ref readonly RenderView MainView
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _frames[_writeFrameIndex].MainView;
    }
    
    public ReadOnlySpan<RenderView> ShadowViews
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].ShadowViews.AsSpan(0, _frames[_writeFrameIndex].ShadowViewCount);
    }
    
    public ReadOnlySpan<RenderLight> Lights
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].Lights.AsSpan(0, _frames[_writeFrameIndex].LightCount);
    }
    
    public ReadOnlySpan<RenderBatch> StaticBatches
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].StaticBatches.AsSpan(0, _frames[_writeFrameIndex].StaticBatchCount);
    }
    
    public ReadOnlySpan<RenderInstance> StaticInstances
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].StaticInstances.AsSpan(0, _frames[_writeFrameIndex].StaticInstanceCount);
    }
    
    public ReadOnlySpan<SkinnedRenderInstance> SkinnedInstances
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].SkinnedInstances.AsSpan(0, _frames[_writeFrameIndex].SkinnedInstanceCount);
    }
    
    public ReadOnlySpan<Matrix4x4> SkinnedBoneMatrices
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _frames[_writeFrameIndex].SkinnedBoneMatrices.AsSpan(0, _frames[_writeFrameIndex].SkinnedBoneMatricesCount);
    }
    
    public int ObjectCount => _objectCount;
    
    public int StaticBatchCount => _frames[_writeFrameIndex].StaticBatchCount;
    
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
