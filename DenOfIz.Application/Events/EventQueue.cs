using System.Runtime.CompilerServices;
using DenOfIz;

namespace Application.Events;

public sealed class EventQueue
{
    private Event[] _events = new Event[64];

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

    private int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        set;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Event GetEvent(int index) => ref _events[index];

    public ReadOnlySpan<Event> Events
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new ReadOnlySpan<Event>(_events, 0, Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventEnumerator GetEnumerator() => new(this);

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
        public bool MoveNext() => ++_index < _queue.Count;

        public ref Event Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _queue._events[_index];
        }
    }
}
