using System.Numerics;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Physics;

namespace DZForestDemo.GameObjects;

public class Food() : GameObject("Food")
{
    private float _baseY;
    private float _time;

    public void SetMeshAndMaterial(Mesh mesh, Material material)
    {
        AddComponent(new MeshComponent { Mesh = mesh });
        AddComponent(new MaterialComponent { Material = material });
        AddComponent(RigidbodyComponent.Kinematic(PhysicsShape.Sphere(0.8f)));
        _baseY = LocalPosition.Y;
    }

    public override void Update()
    {
        _time += Time.DeltaTime;
        var newPos = LocalPosition;
        newPos.Y = _baseY + MathF.Sin(_time * 3f) * 0.2f;
        LocalPosition = newPos;
        LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _time * 2f);
    }
}
