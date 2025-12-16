using System.Runtime.CompilerServices;
using DenOfIz;

namespace Application.Events;

public sealed class EventQueue
{
    private Event[] _events = new Event[64];

    private int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        set;
    }

    public ReadOnlySpan<Event> Events
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_events, 0, Count);
    }

    public void Poll()
    {
        Count = 0;
        while (InputSystem.PollEvent(out var ev))
        {
            if (Count >= _events.Length)
            {
                Array.Resize(ref _events, _events.Length * 2);
            }

            _events[Count++] = ev;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Event GetEvent(int index)
    {
        return ref _events[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventEnumerator GetEnumerator()
    {
        return new EventEnumerator(this);
    }

    public ref struct EventEnumerator
    {
        private readonly EventQueue _queue;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EventEnumerator(EventQueue queue)
        {
            _queue = queue;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return ++_index < _queue.Count;
        }

        public ref Event Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _queue._events[_index];
        }
    }
}