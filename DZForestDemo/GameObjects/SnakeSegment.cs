using System.Numerics;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Physics;

namespace DZForestDemo.GameObjects;

public class SnakeSegment(string name, bool isHead = false) : GameObject(name)
{
    public bool IsHead { get; } = isHead;

    private Vector3 _targetPosition;
    private Vector3 _previousPosition;
    private float _lerpProgress = 1f;

    public float MoveSpeed { get; set; } = 8f;

    public void SetMeshAndMaterial(Mesh mesh, Material material)
    {
        AddComponent(new MeshComponent { Mesh = mesh });
        AddComponent(new MaterialComponent { Material = material });
        AddComponent(RigidbodyComponent.Kinematic(PhysicsShape.Cube(1f)));
    }

    public void SetTargetPosition(Vector3 target)
    {
        _previousPosition = LocalPosition;
        _targetPosition = target;
        _lerpProgress = 0f;
    }

    public void SetPositionImmediate(Vector3 position)
    {
        _previousPosition = position;
        _targetPosition = position;
        _lerpProgress = 1f;
        LocalPosition = position;
    }

    public override void Update()
    {
        if (_lerpProgress < 1f)
        {
            _lerpProgress += Time.DeltaTime * MoveSpeed;
            if (_lerpProgress > 1f)
            {
                _lerpProgress = 1f;
            }

            var t = _lerpProgress * _lerpProgress * (3f - 2f * _lerpProgress); // SmoothStep
            LocalPosition = Vector3.Lerp(_previousPosition, _targetPosition, t);
        }
    }
}
