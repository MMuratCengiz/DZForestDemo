using System.Runtime.CompilerServices;
using DenOfIz;

namespace Application.Events;

/// <summary>
/// Dispatches events to registered receivers in priority order.
/// Designed for zero allocation in the hot path.
/// </summary>
public sealed class EventDispatcher
{
    private readonly List<ReceiverEntry> _receivers = new();
    private ReceiverEntry[] _sortedReceivers = [];
    private bool _dirty = true;

    /// <summary>
    /// Registers an event receiver with the specified priority.
    /// Higher priority receivers are called first.
    /// </summary>
    /// <param name="receiver">The receiver to register.</param>
    /// <param name="priority">Priority level (higher = called first). Default is 0.</param>
    public void Register(IEventReceiver receiver, int priority = 0)
    {
        _receivers.Add(new ReceiverEntry(receiver, priority));
        _dirty = true;
    }

    /// <summary>
    /// Unregisters an event receiver.
    /// </summary>
    /// <param name="receiver">The receiver to unregister.</param>
    public void Unregister(IEventReceiver receiver)
    {
        for (int i = _receivers.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_receivers[i].Receiver, receiver))
            {
                _receivers.RemoveAt(i);
                _dirty = true;
                return;
            }
        }
    }

    /// <summary>
    /// Dispatches an event to all registered receivers until one consumes it.
    /// </summary>
    /// <param name="ev">The event to dispatch.</param>
    /// <returns>True if the event was consumed by a receiver.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Dispatch(ref Event ev)
    {
        if (_dirty)
        {
            RebuildSortedArray();
        }

        ReadOnlySpan<ReceiverEntry> receivers = _sortedReceivers;
        for (int i = 0; i < receivers.Length; i++)
        {
            if (receivers[i].Receiver.OnEvent(ref ev))
            {
                return true;
            }
        }

        return false;
    }

    private void RebuildSortedArray()
    {
        _sortedReceivers = _receivers.ToArray();
        Array.Sort(_sortedReceivers, static (a, b) => b.Priority.CompareTo(a.Priority));
        _dirty = false;
    }

    private readonly struct ReceiverEntry(IEventReceiver receiver, int priority)
    {
        public readonly IEventReceiver Receiver = receiver;
        public readonly int Priority = priority;
    }
}
