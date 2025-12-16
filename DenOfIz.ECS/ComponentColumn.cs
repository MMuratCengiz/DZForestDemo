using System.Runtime.CompilerServices;

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

    public ComponentColumn() : this(16)
    {
    }

    public ComponentId ComponentId { get; } = ComponentRegistry.GetId<T>();

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _data.Length;
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
        Array.Copy(_data, newData, Count);
        _data = newData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SwapRemove(int index)
    {
        if (index < Count - 1)
        {
            _data[index] = _data[Count - 1];
        }

        Count--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Count = 0;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Add(in T component)
    {
        EnsureCapacity(Count + 1);
        _data[Count] = component;
        return Count++;
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
        return _data.AsSpan(0, Count);
    }
}