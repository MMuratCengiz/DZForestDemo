using System.Numerics;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;

namespace DZForestDemo.GameObjects;

public class SnakeSegment : IComponent
{
    public GameObject? Owner { get; set; }

    public bool IsHead { get; set; }
    public float MoveSpeed { get; set; } = 8f;

    private Vector3 _targetPosition;
    private Vector3 _previousPosition;
    private float _lerpProgress = 1f;

    public void SetTargetPosition(Vector3 target)
    {
        if (Owner == null)
        {
            return;
        }

        _previousPosition = Owner.LocalPosition;
        _targetPosition = target;
        _lerpProgress = 0f;
    }

    public void SetPositionImmediate(Vector3 position)
    {
        if (Owner == null)
        {
            return;
        }

        _previousPosition = position;
        _targetPosition = position;
        _lerpProgress = 1f;
        Owner.LocalPosition = position;
    }

    public void Update()
    {
        if (Owner == null || _lerpProgress >= 1f)
        {
            return;
        }

        _lerpProgress += Time.DeltaTime * MoveSpeed;
        if (_lerpProgress > 1f)
        {
            _lerpProgress = 1f;
        }

        var t = _lerpProgress * _lerpProgress * (3f - 2f * _lerpProgress);
        Owner.LocalPosition = Vector3.Lerp(_previousPosition, _targetPosition, t);
    }
}
