using System.Numerics;
using System.Runtime.CompilerServices;
using NiziKit.Components;

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

            if (!_trackedObjects.TryGetValue(key.IdA, out var goA) ||
                !_trackedObjects.TryGetValue(key.IdB, out var goB))
            {
                continue;
            }

            var rbA = goA.GetComponent<RigidbodyComponent>();
            var rbB = goB.GetComponent<RigidbodyComponent>();

            var velA = rbA != null ? GetVelocity(key.IdA) : Vector3.Zero;
            var velB = rbB != null ? GetVelocity(key.IdB) : Vector3.Zero;
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
                goA.OnCollisionStay(in collisionA);
                goB.OnCollisionStay(in collisionB);
            }
            else
            {
                goA.OnCollisionEnter(in collisionA);
                goB.OnCollisionEnter(in collisionB);
            }
        }

        for (var i = 0; i < _previousContactCount; i++)
        {
            var key = _previousContactsBuffer[i];

            if (ContainsContact(_activeContactsBuffer, _activeContactCount, key))
            {
                continue;
            }

            if (!_trackedObjects.TryGetValue(key.IdA, out var goA) ||
                !_trackedObjects.TryGetValue(key.IdB, out var goB))
            {
                continue;
            }

            var rbA = goA.GetComponent<RigidbodyComponent>();
            var rbB = goB.GetComponent<RigidbodyComponent>();

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

            goA.OnCollisionExit(in collisionA);
            goB.OnCollisionExit(in collisionB);
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
}
