using System.Runtime.CompilerServices;
using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics.Binding;

public class BindGroupData
{
    private const int MaxSlots = 16;

    private readonly BindingEntry[] _entries = new BindingEntry[MaxSlots];

    public ulong Hash { get; private set; }

    public uint SetMask { get; private set; }

    public bool IsEmpty => SetMask == 0;

    public struct BindingEntry
    {
        public ResourceBindingType Type;
        public bool IsSet;
        public Texture? Texture;
        public Sampler? Sampler;
        public Buffer? Buffer;
        public ulong BufferOffset;
        public ulong BufferSize;
        public byte[]? InlineData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTexture(ResourceBindingSlot slot, Texture texture)
    {
        if (slot.Binding >= MaxSlots)
        {
            return;
        }

        ref var entry = ref _entries[slot.Binding];
        entry.Type = slot.Type;
        entry.IsSet = true;
        entry.Texture = texture;

        SetMask |= (1u << (int)slot.Binding);
        UpdateHash(slot.Binding, (ulong)slot.Type, (ulong)RuntimeHelpers.GetHashCode(texture));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(ResourceBindingSlot slot, Sampler sampler)
    {
        if (slot.Binding >= MaxSlots)
        {
            return;
        }

        ref var entry = ref _entries[slot.Binding];
        entry.Type = ResourceBindingType.Sampler;
        entry.IsSet = true;
        entry.Sampler = sampler;

        SetMask |= (1u << (int)slot.Binding);
        UpdateHash(slot.Binding, (ulong)ResourceBindingType.Sampler, (ulong)RuntimeHelpers.GetHashCode(sampler));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBuffer(ResourceBindingSlot slot, Buffer buffer, ulong offset = 0, ulong size = 0)
    {
        if (slot.Binding >= MaxSlots)
        {
            return;
        }

        ref var entry = ref _entries[slot.Binding];
        entry.Type = slot.Type;
        entry.IsSet = true;
        entry.Buffer = buffer;
        entry.BufferOffset = offset;
        entry.BufferSize = size;

        SetMask |= (1u << (int)slot.Binding);
        UpdateHash(slot.Binding, (ulong)slot.Type,
            HashCombine((ulong)RuntimeHelpers.GetHashCode(buffer), HashCombine(offset, size)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBuffer(ResourceBindingSlot slot, GpuBufferView bufferView)
    {
        SetBuffer(slot, bufferView.Buffer, bufferView.Offset, bufferView.NumBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetData(ResourceBindingSlot slot, byte[] data)
    {
        if (slot.Binding >= MaxSlots)
        {
            return;
        }

        ref var entry = ref _entries[slot.Binding];
        entry.Type = ResourceBindingType.ConstantBuffer;
        entry.IsSet = true;
        entry.InlineData = data;

        SetMask |= (1u << (int)slot.Binding);
        UpdateHash(slot.Binding, (ulong)ResourceBindingType.ConstantBuffer, (ulong)RuntimeHelpers.GetHashCode(data));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly BindingEntry GetEntry(uint binding)
    {
        return ref _entries[binding];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateHash(uint binding, ulong type, ulong valueHash)
    {
        var slotHash = HashCombine(binding, HashCombine(type, valueHash));
        Hash = HashCombine(Hash, slotHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashCombine(ulong h1, ulong h2)
    {
        return (h1 ^ h2) * 0x100000001b3UL;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HashEquals(BindGroupData other)
    {
        return Hash == other.Hash && SetMask == other.SetMask;
    }

    public void Reset()
    {
        Hash = 0;
        SetMask = 0;
        Array.Clear(_entries);
    }
}
