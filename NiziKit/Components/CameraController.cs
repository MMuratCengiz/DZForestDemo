using System.Numerics;
using DenOfIz;

namespace NiziKit.Components;

public enum CameraMode
{
    FreeFly,
    Orbit
}

[NiziComponent]
public partial class CameraController
{
    private bool _isLooking;
    private int _lastMouseX;
    private int _lastMouseY;
    private bool _moveForward;
    private bool _moveBackward;
    private bool _moveLeft;
    private bool _moveRight;
    private bool _moveUp;
    private bool _moveDown;
    private bool _speedBoost;
    private bool _speedSlow;

    private Vector3 _currentVelocity;
    private Vector3 _targetPosition;
    private float _currentYaw;
    private float _currentPitch;
    private float _targetYaw;
    private float _targetPitch;

    public partial CameraMode Mode { get; set; }

    public partial float MoveSpeed { get; set; }
    public partial float SprintMultiplier { get; set; }
    public partial float SlowMultiplier { get; set; }
    public partial float MoveDamping { get; set; }

    public partial float LookSensitivity { get; set; }
    public partial float LookDamping { get; set; }
    public partial float MinPitch { get; set; }
    public partial float MaxPitch { get; set; }

    public partial Vector3 OrbitTarget { get; set; }
    public partial float OrbitDistance { get; set; }
    public partial float ZoomSensitivity { get; set; }
    public partial float MinOrbitDistance { get; set; }
    public partial float MaxOrbitDistance { get; set; }

    public partial float ScrollZoomSpeed { get; set; }

    public CameraController()
    {
        Mode = CameraMode.FreeFly;
        MoveSpeed = 10f;
        SprintMultiplier = 3f;
        SlowMultiplier = 0.25f;
        MoveDamping = 25f;
        LookSensitivity = 0.003f;
        LookDamping = 50f;
        MinPitch = -MathF.PI / 2f + 0.01f;
        MaxPitch = MathF.PI / 2f - 0.01f;
        OrbitDistance = 10f;
        ZoomSensitivity = 1f;
        MinOrbitDistance = 1f;
        MaxOrbitDistance = 100f;
        ScrollZoomSpeed = 2f;
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

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));

    public void LookAt(Vector3 target)
    {
        if (Owner == null)
        {
            return;
        }

        var direction = Vector3.Normalize(target - Owner.LocalPosition);
        _targetYaw = MathF.Atan2(direction.X, direction.Z);
        _targetPitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));
        _currentYaw = _targetYaw;
        _currentPitch = _targetPitch;
        _targetPosition = Owner.LocalPosition;

        OrbitTarget = target;
        OrbitDistance = Vector3.Distance(Owner.LocalPosition, target);

        UpdateOwnerRotation();
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
            _currentVelocity = Vector3.Zero;
            UpdateOwnerRotation();
        }
    }

    public void Update(float deltaTime)
    {
        if (Owner == null)
        {
            return;
        }

        _currentYaw = SmoothDampAngle(_currentYaw, _targetYaw, LookDamping, deltaTime);
        _currentPitch = SmoothDamp(_currentPitch, _targetPitch, LookDamping, deltaTime);

        if (Mode == CameraMode.FreeFly)
        {
            UpdateFreeFly(deltaTime);
        }
        else
        {
            UpdateOrbit(deltaTime);
        }

        UpdateOwnerRotation();
    }

    private void UpdateFreeFly(float deltaTime)
    {
        var moveDir = Vector3.Zero;

        if (_moveForward)
        {
            moveDir += Forward;
        }

        if (_moveBackward)
        {
            moveDir -= Forward;
        }

        if (_moveRight)
        {
            moveDir -= Right;
        }

        if (_moveLeft)
        {
            moveDir += Right;
        }

        if (_moveUp)
        {
            moveDir += Vector3.UnitY;
        }

        if (_moveDown)
        {
            moveDir -= Vector3.UnitY;
        }

        if (moveDir.LengthSquared() > 0.001f)
        {
            moveDir = Vector3.Normalize(moveDir);
        }

        var speed = MoveSpeed;
        if (_speedBoost)
        {
            speed *= SprintMultiplier;
        }

        if (_speedSlow)
        {
            speed *= SlowMultiplier;
        }

        var targetVelocity = moveDir * speed;

        _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, 1f - MathF.Exp(-MoveDamping * deltaTime));
        _targetPosition += _currentVelocity * deltaTime;
        Owner!.LocalPosition =
            Vector3.Lerp(Owner.LocalPosition, _targetPosition, 1f - MathF.Exp(-MoveDamping * deltaTime));
    }

    private void UpdateOrbit(float deltaTime)
    {
        var cosP = MathF.Cos(_currentPitch);
        var targetOrbitPos = OrbitTarget + new Vector3(
            MathF.Sin(_currentYaw) * cosP,
            MathF.Sin(_currentPitch),
            MathF.Cos(_currentYaw) * cosP
        ) * OrbitDistance;

        _targetPosition = targetOrbitPos;
        Owner!.LocalPosition =
            Vector3.Lerp(Owner.LocalPosition, _targetPosition, 1f - MathF.Exp(-MoveDamping * deltaTime));
    }

    private void UpdateOwnerRotation()
    {
        if (Owner == null)
        {
            return;
        }

        Owner.LocalRotation = Quaternion.CreateFromYawPitchRoll(_currentYaw, _currentPitch, 0);
    }

    public bool HandleEvent(in Event ev)
    {
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
                if (Mode == CameraMode.Orbit)
                {
                    OrbitDistance -= ev.MouseWheel.Y * ZoomSensitivity;
                    OrbitDistance = Math.Clamp(OrbitDistance, MinOrbitDistance, MaxOrbitDistance);
                }
                else
                {
                    _targetPosition += Forward * ev.MouseWheel.Y * ScrollZoomSpeed;
                }

                return true;

            case EventType.KeyDown:
                return HandleKeyDown(ev.Key.KeyCode);

            case EventType.KeyUp:
                return HandleKeyUp(ev.Key.KeyCode);
        }

        return false;
    }

    private bool HandleKeyDown(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W:
                _moveForward = true;
                return true;
            case KeyCode.S:
                _moveBackward = true;
                return true;
            case KeyCode.A:
                _moveLeft = true;
                return true;
            case KeyCode.D:
                _moveRight = true;
                return true;
            case KeyCode.E:
                _moveUp = true;
                return true;
            case KeyCode.Q:
            case KeyCode.Lctrl:
            case KeyCode.Rctrl:
                _moveDown = true;
                return true;
            case KeyCode.Lshift:
            case KeyCode.Rshift:
                _speedBoost = true;
                return true;
            case KeyCode.Lalt:
            case KeyCode.Ralt:
                _speedSlow = true;
                return true;
            case KeyCode.F:
                Mode = Mode == CameraMode.FreeFly ? CameraMode.Orbit : CameraMode.FreeFly;
                if (Mode == CameraMode.Orbit && Owner != null)
                {
                    OrbitTarget = Owner.LocalPosition + Forward * OrbitDistance;
                }

                return true;
        }

        return false;
    }

    private bool HandleKeyUp(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W:
                _moveForward = false;
                return true;
            case KeyCode.S:
                _moveBackward = false;
                return true;
            case KeyCode.A:
                _moveLeft = false;
                return true;
            case KeyCode.D:
                _moveRight = false;
                return true;
            case KeyCode.E:
                _moveUp = false;
                return true;
            case KeyCode.Q:
            case KeyCode.Lctrl:
            case KeyCode.Rctrl:
                _moveDown = false;
                return true;
            case KeyCode.Lshift:
            case KeyCode.Rshift:
                _speedBoost = false;
                return true;
            case KeyCode.Lalt:
            case KeyCode.Ralt:
                _speedSlow = false;
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

        Mode = CameraMode.Orbit;
    }

    public void ResetVelocity()
    {
        _currentVelocity = Vector3.Zero;
    }
}