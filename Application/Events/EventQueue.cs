using System.Runtime.CompilerServices;
using DenOfIz;

namespace Application.Events;

/// <summary>
/// Polls and queues input events from the system.
/// Provides iteration without allocation via ref struct enumerator.
/// </summary>
public sealed class EventQueue
{
    // Pre-allocated buffer to avoid per-frame allocation
    private Event[] _events = new Event[64];

    /// <summary>
    /// Polls all pending events from the input system into the internal buffer.
    /// Call once per frame before processing events.
    /// </summary>
    public void Poll()
    {
        Count = 0;

        while (InputSystem.PollEvent(out var ev))
        {
            if (Count >= _events.Length)
            {
                // Grow buffer if needed (rare case)
                Array.Resize(ref _events, _events.Length * 2);
            }

            _events[Count++] = ev;
        }
    }

    /// <summary>
    /// Gets the number of events in the queue.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    /// <summary>
    /// Gets a reference to an event at the specified index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Event GetEvent(int index) => ref _events[index];

    /// <summary>
    /// Gets a span of all polled events. Zero allocation.
    /// </summary>
    public ReadOnlySpan<Event> Events
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new ReadOnlySpan<Event>(_events, 0, Count);
    }

    /// <summary>
    /// Returns an enumerator for foreach support without allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EventEnumerator GetEnumerator() => new(this);

    /// <summary>
    /// Zero-allocation enumerator for event iteration.
    /// </summary>
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
