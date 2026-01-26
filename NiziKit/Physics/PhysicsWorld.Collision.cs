using System.Numerics;
using System.Runtime.CompilerServices;
using NiziKit.Core;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SwapContactBuffers()
    {
        Array.Copy(_activeContactsBuffer, _previousContactsBuffer, _activeContactCount);
        _previousContactCount = _activeContactCount;
        _activeContactCount = 0;
    }

    private void ProcessCollisionCallbacks()
    {
        int eventCount;
        Span<CollisionEventData> events = stackalloc CollisionEventData[MaxCollisionEvents];

        lock (_collisionLock)
        {
            eventCount = _pendingEventCount;
            for (var i = 0; i < eventCount; i++)
            {
                events[i] = _pendingCollisionEvents[i];
            }
            _pendingEventCount = 0;
        }

        for (var i = 0; i < eventCount; i++)
        {
            ref var evt = ref events[i];
            var idA = evt.IdA;
            var idB = evt.IdB;
            var key = idA < idB ? new CollisionPair(idA, idB) : new CollisionPair(idB, idA);

            if (!ContainsContact(_activeContactsBuffer, _activeContactCount, key))
            {
                if (_activeContactCount < MaxTrackedContacts)
                {
                    _activeContactsBuffer[_activeContactCount++] = key;
                }
            }
        }

        for (var i = 0; i < _activeContactCount; i++)
        {
            var key = _activeContactsBuffer[i];

            if (!_trackedObjects.TryGetValue(key.IdA, out var entryA) ||
                !_trackedObjects.TryGetValue(key.IdB, out var entryB))
            {
                continue;
            }

            var goA = entryA.Go;
            var goB = entryB.Go;
            var rbA = entryA.Rigidbody;
            var rbB = entryB.Rigidbody;

            var velA = GetVelocity(key.IdA);
            var velB = GetVelocity(key.IdB);
            var relativeVelocity = velA - velB;

            var wasActive = ContainsContact(_previousContactsBuffer, _previousContactCount, key);

            var collisionA = new Collision
            {
                Other = goB,
                Rigidbody = rbB,
                RelativeVelocity = relativeVelocity,
                ContactCount = 0
            };

            var collisionB = new Collision
            {
                Other = goA,
                Rigidbody = rbA,
                RelativeVelocity = -relativeVelocity,
                ContactCount = 0
            };

            if (wasActive)
            {
                NotifyCollisionStay(goA, in collisionA);
                NotifyCollisionStay(goB, in collisionB);
            }
            else
            {
                NotifyCollisionEnter(goA, in collisionA);
                NotifyCollisionEnter(goB, in collisionB);
            }
        }

        for (var i = 0; i < _previousContactCount; i++)
        {
            var key = _previousContactsBuffer[i];

            if (ContainsContact(_activeContactsBuffer, _activeContactCount, key))
            {
                continue;
            }

            if (!_trackedObjects.TryGetValue(key.IdA, out var entryA) ||
                !_trackedObjects.TryGetValue(key.IdB, out var entryB))
            {
                continue;
            }

            var goA = entryA.Go;
            var goB = entryB.Go;
            var rbA = entryA.Rigidbody;
            var rbB = entryB.Rigidbody;

            var collisionA = new Collision
            {
                Other = goB,
                Rigidbody = rbB,
                RelativeVelocity = Vector3.Zero,
                ContactCount = 0
            };

            var collisionB = new Collision
            {
                Other = goA,
                Rigidbody = rbA,
                RelativeVelocity = Vector3.Zero,
                ContactCount = 0
            };

            NotifyCollisionExit(goA, in collisionA);
            NotifyCollisionExit(goB, in collisionB);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsContact(CollisionPair[] buffer, int count, CollisionPair key)
    {
        for (var i = 0; i < count; i++)
        {
            if (buffer[i].IdA == key.IdA && buffer[i].IdB == key.IdB)
            {
                return true;
            }
        }
        return false;
    }

    internal void RecordContact(int idA, int idB, Vector3 contactPoint, Vector3 normal, float depth)
    {
        lock (_collisionLock)
        {
            if (_pendingEventCount < MaxCollisionEvents)
            {
                _pendingCollisionEvents[_pendingEventCount++] = new CollisionEventData
                {
                    IdA = idA,
                    IdB = idB,
                    ContactPoint = contactPoint,
                    Normal = normal,
                    Depth = depth
                };
            }
        }
    }

    private static void NotifyCollisionEnter(GameObject go, in Collision collision)
    {
        foreach (var component in go.Components)
        {
            component.OnCollisionEnter(in collision);
        }
    }

    private static void NotifyCollisionStay(GameObject go, in Collision collision)
    {
        foreach (var component in go.Components)
        {
            component.OnCollisionStay(in collision);
        }
    }

    private static void NotifyCollisionExit(GameObject go, in Collision collision)
    {
        foreach (var component in go.Components)
        {
            component.OnCollisionExit(in collision);
        }
    }
}
