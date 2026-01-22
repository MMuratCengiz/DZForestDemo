using System.Numerics;
using DenOfIz;

namespace NiziKit.Components;

[NiziComponent]
public partial class OrbitController
{
    private bool _isLooking;
    private int _lastMouseX;
    private int _lastMouseY;

    private Vector3 _targetPosition;
    private float _currentYaw;
    private float _currentPitch;
    private float _targetYaw;
    private float _targetPitch;

    public partial Vector3 OrbitTarget { get; set; }
    public partial float OrbitDistance { get; set; }
    public partial float ZoomSensitivity { get; set; }
    public partial float MinOrbitDistance { get; set; }
    public partial float MaxOrbitDistance { get; set; }
    public partial float LookSensitivity { get; set; }
    public partial float LookDamping { get; set; }
    public partial float MoveDamping { get; set; }
    public partial float MinPitch { get; set; }
    public partial float MaxPitch { get; set; }
    public partial bool IsEnabled { get; set; }

    public OrbitController()
    {
        OrbitTarget = Vector3.Zero;
        OrbitDistance = 10f;
        ZoomSensitivity = 1f;
        MinOrbitDistance = 1f;
        MaxOrbitDistance = 100f;
        LookSensitivity = 0.003f;
        LookDamping = 50f;
        MoveDamping = 25f;
        MinPitch = -MathF.PI / 2f + 0.01f;
        MaxPitch = MathF.PI / 2f - 0.01f;
        IsEnabled = true;
    }

    public Vector3 Forward
    {
        get
        {
            var cosP = MathF.Cos(_currentPitch);
            return new Vector3(
                MathF.Sin(_currentYaw) * cosP,
                MathF.Sin(_currentPitch),
                MathF.Cos(_currentYaw) * cosP
            );
        }
    }

    public void FocusOn(Vector3 target, float distance = -1f)
    {
        OrbitTarget = target;
        if (distance > 0)
        {
            OrbitDistance = distance;
        }
        else if (Owner != null)
        {
            OrbitDistance = Vector3.Distance(Owner.LocalPosition, target);
        }

        if (Owner != null)
        {
            var direction = Vector3.Normalize(target - Owner.LocalPosition);
            _targetYaw = MathF.Atan2(direction.X, direction.Z);
            _targetPitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));
            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;
        }
    }

    public void SetPositionAndLookAt(Vector3 position, Vector3 target, bool immediate = false)
    {
        if (Owner == null)
        {
            return;
        }

        _targetPosition = position;
        OrbitTarget = target;
        OrbitDistance = Vector3.Distance(position, target);

        var direction = Vector3.Normalize(target - position);
        _targetYaw = MathF.Atan2(direction.X, direction.Z);
        _targetPitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));

        if (immediate)
        {
            Owner.LocalPosition = position;
            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;
            UpdateOwnerRotation();
        }
    }

    public void UpdateCamera(float deltaTime)
    {
        if (Owner == null || !IsEnabled)
        {
            return;
        }

        _currentYaw = SmoothDampAngle(_currentYaw, _targetYaw, LookDamping, deltaTime);
        _currentPitch = SmoothDamp(_currentPitch, _targetPitch, LookDamping, deltaTime);

        var cosP = MathF.Cos(_currentPitch);
        var targetOrbitPos = OrbitTarget - new Vector3(
            MathF.Sin(_currentYaw) * cosP,
            MathF.Sin(_currentPitch),
            MathF.Cos(_currentYaw) * cosP
        ) * OrbitDistance;

        _targetPosition = targetOrbitPos;
        Owner.LocalPosition = Vector3.Lerp(Owner.LocalPosition, _targetPosition, 1f - MathF.Exp(-MoveDamping * deltaTime));

        UpdateOwnerRotation();
    }

    private void UpdateOwnerRotation()
    {
        if (Owner == null)
        {
            return;
        }

        Owner.LocalRotation = Quaternion.CreateFromYawPitchRoll(_currentYaw, -_currentPitch, 0);
    }

    public bool HandleEvent(in Event ev)
    {
        if (!IsEnabled)
        {
            return false;
        }

        switch (ev.Type)
        {
            case EventType.MouseButtonDown:
                if (ev.MouseButton.Button == MouseButton.Right)
                {
                    _isLooking = true;
                    _lastMouseX = (int)ev.MouseButton.X;
                    _lastMouseY = (int)ev.MouseButton.Y;
                    return true;
                }
                break;

            case EventType.MouseButtonUp:
                if (ev.MouseButton.Button == MouseButton.Right)
                {
                    _isLooking = false;
                    return true;
                }
                break;

            case EventType.MouseMotion:
                if (_isLooking)
                {
                    var deltaX = ev.MouseMotion.X - _lastMouseX;
                    var deltaY = ev.MouseMotion.Y - _lastMouseY;
                    _lastMouseX = (int)ev.MouseMotion.X;
                    _lastMouseY = (int)ev.MouseMotion.Y;

                    _targetYaw += deltaX * LookSensitivity;
                    _targetPitch -= deltaY * LookSensitivity;
                    _targetPitch = Math.Clamp(_targetPitch, MinPitch, MaxPitch);

                    return true;
                }
                break;

            case EventType.MouseWheel:
                OrbitDistance -= ev.MouseWheel.Y * ZoomSensitivity;
                OrbitDistance = Math.Clamp(OrbitDistance, MinOrbitDistance, MaxOrbitDistance);
                return true;
        }

        return false;
    }

    private static float SmoothDamp(float current, float target, float damping, float deltaTime)
    {
        return current + (target - current) * (1f - MathF.Exp(-damping * deltaTime));
    }

    private static float SmoothDampAngle(float current, float target, float damping, float deltaTime)
    {
        var delta = WrapAngle(target - current);
        return current + delta * (1f - MathF.Exp(-damping * deltaTime));
    }

    private static float WrapAngle(float angle)
    {
        while (angle > MathF.PI)
        {
            angle -= MathF.PI * 2f;
        }

        while (angle < -MathF.PI)
        {
            angle += MathF.PI * 2f;
        }

        return angle;
    }
}
