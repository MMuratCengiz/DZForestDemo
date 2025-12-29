using System.Runtime.CompilerServices;

namespace DenOfIz.Tasks;

public sealed class WorkStealingDeque
{
    private readonly int[] _buffer;
    private readonly int _mask;
    private long _bottom;
    private long _top;

    public WorkStealingDeque(int capacity = 256)
    {
        var size = 1;
        while (size < capacity)
        {
            size <<= 1;
        }

        _buffer = new int[size];
        _mask = size - 1;
        _bottom = 0;
        _top = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushBottom(int item)
    {
        var b = Volatile.Read(ref _bottom);
        _buffer[b & _mask] = item;
        Volatile.Write(ref _bottom, b + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPopBottom(out int item)
    {
        var b = Volatile.Read(ref _bottom) - 1;
        Volatile.Write(ref _bottom, b);

        var t = Interlocked.Read(ref _top);
        var size = b - t;

        if (size < 0)
        {
            Volatile.Write(ref _bottom, t);
            item = -1;
            return false;
        }

        item = _buffer[b & _mask];

        if (size > 0)
        {
            return true;
        }

        if (Interlocked.CompareExchange(ref _top, t + 1, t) != t)
        {
            Volatile.Write(ref _bottom, t + 1);
            item = -1;
            return false;
        }

        Volatile.Write(ref _bottom, t + 1);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySteal(out int item)
    {
        var t = Interlocked.Read(ref _top);
        var b = Volatile.Read(ref _bottom);
        var size = b - t;

        if (size <= 0)
        {
            item = -1;
            return false;
        }

        item = _buffer[t & _mask];

        if (Interlocked.CompareExchange(ref _top, t + 1, t) != t)
        {
            item = -1;
            return false;
        }

        return true;
    }

    public void Clear()
    {
        _bottom = 0;
        _top = 0;
    }

    public int Count => (int)(Volatile.Read(ref _bottom) - Interlocked.Read(ref _top));
}
