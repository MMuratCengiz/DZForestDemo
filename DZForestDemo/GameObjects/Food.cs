using System.Numerics;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;

namespace DZForestDemo.GameObjects;

public class Food : IComponent
{
    public GameObject? Owner { get; set; }

    private float _baseY;
    private float _time;
    private bool _initialized;

    public void Begin()
    {
        if (Owner != null)
        {
            _baseY = Owner.LocalPosition.Y;
            _initialized = true;
        }
    }

    public void Update()
    {
        if (Owner == null || !_initialized)
        {
            return;
        }

        _time += Time.DeltaTime;
        var newPos = Owner.LocalPosition;
        newPos.Y = _baseY + MathF.Sin(_time * 3f) * 0.2f;
        Owner.LocalPosition = newPos;
        Owner.LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, _time * 2f);
    }
}
