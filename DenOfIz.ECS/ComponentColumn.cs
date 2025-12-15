using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ECS;

public interface IComponentColumn
{
    ComponentId ComponentId { get; }
    int Count { get; }
    int Capacity { get; }
    void EnsureCapacity(int capacity);
    void SwapRemove(int index);
    void Clear();
    IComponentColumn Clone();
    void CopyFrom(IComponentColumn source, int sourceIndex);
}

public sealed class ComponentColumn<T>(int initialCapacity) : IComponentColumn
    where T : struct
{
    private T[] _data = new T[initialCapacity];
    private int _count = 0;

    public ComponentId ComponentId { get; } = ComponentRegistry.GetId<T>();

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _count;
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Length;
    }

    public ComponentColumn() : this(16)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EnsureCapacity(int capacity)
    {
        if (_data.Length >= capacity)
        {
            return;
        }

        var newCapacity = Math.Max(_data.Length * 2, capacity);
        var newData = new T[newCapacity];
        Array.Copy(_data, newData, _count);
        _data = newData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(in T component)
    {
        EnsureCapacity(_count + 1);
        _data[_count] = component;
        return _count++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Get(int index)
    {
        return ref _data[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int index, in T component)
    {
        _data[index] = component;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        return _data.AsSpan(0, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SwapRemove(int index)
    {
        if (index < _count - 1)
        {
            _data[index] = _data[_count - 1];
        }
        _count--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _count = 0;
    }

    public IComponentColumn Clone()
    {
        return new ComponentColumn<T>(_data.Length);
    }

    public void CopyFrom(IComponentColumn source, int sourceIndex)
    {
        var typedSource = (ComponentColumn<T>)source;
        Add(in typedSource._data[sourceIndex]);
    }
}
