using System.Numerics;
using DenOfIz;

namespace DZForestDemo;

public enum CameraMode
{
    FreeFly,
    Orbit
}

public class Camera
{
    // Input state
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

    // Smoothing state
    private Vector3 _currentVelocity;
    private Vector3 _targetPosition;
    private float _currentYaw;
    private float _currentPitch;
    private float _targetYaw;
    private float _targetPitch;

    // Camera properties
    public Vector3 Position { get; set; }
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public CameraMode Mode { get; set; } = CameraMode.FreeFly;

    // Projection settings
    public float FieldOfView { get; set; } = MathF.PI / 4f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000f;
    public float AspectRatio { get; set; } = 16f / 9f;

    // Movement settings
    public float MoveSpeed { get; set; } = 10f;
    public float SprintMultiplier { get; set; } = 3f;
    public float SlowMultiplier { get; set; } = 0.25f;
    public float MoveSmoothTime { get; set; } = 0.05f;
    public float MoveDamping { get; set; } = 25f;

    // Look settings
    public float LookSensitivity { get; set; } = 0.003f;
    public float LookSmoothTime { get; set; } = 0.02f;
    public float LookDamping { get; set; } = 50f;
    public float MinPitch { get; set; } = -MathF.PI / 2f + 0.01f;
    public float MaxPitch { get; set; } = MathF.PI / 2f - 0.01f;

    // Orbit mode settings
    public Vector3 OrbitTarget { get; set; }
    public float OrbitDistance { get; set; } = 10f;
    public float ZoomSensitivity { get; set; } = 1f;
    public float MinOrbitDistance { get; set; } = 1f;
    public float MaxOrbitDistance { get; set; } = 100f;

    // Scroll zoom (works in both modes)
    public float ScrollZoomSpeed { get; set; } = 2f;

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

    public Vector3 Right => Vector3.Normalize(Vector3.Cross(Forward, Up));

    public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAtLeftHanded(Position, Position + Forward, Up);

    public Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
        FieldOfView, AspectRatio, NearPlane, FarPlane);

    public Matrix4x4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

    public Camera(Vector3 position, Vector3 lookAt)
    {
        Position = position;
        _targetPosition = position;
        OrbitTarget = lookAt;

        var direction = Vector3.Normalize(lookAt - position);
        _currentYaw = MathF.Atan2(direction.X, direction.Z);
        _currentPitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));
        _targetYaw = _currentYaw;
        _targetPitch = _currentPitch;

        OrbitDistance = Vector3.Distance(position, lookAt);
    }

    public void SetAspectRatio(uint width, uint height)
    {
        if (height > 0)
        {
            AspectRatio = (float)width / height;
        }
    }

    public void LookAt(Vector3 target)
    {
        var direction = Vector3.Normalize(target - Position);
        _targetYaw = MathF.Atan2(direction.X, direction.Z);
        _targetPitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));
    }

    public void SetPositionAndLookAt(Vector3 position, Vector3 target, bool immediate = false)
    {
        _targetPosition = position;
        OrbitTarget = target;
        OrbitDistance = Vector3.Distance(position, target);

        var direction = Vector3.Normalize(target - position);
        _targetYaw = MathF.Atan2(direction.X, direction.Z);
        _targetPitch = MathF.Asin(Math.Clamp(direction.Y, -1f, 1f));

        if (immediate)
        {
            Position = position;
            _currentYaw = _targetYaw;
            _currentPitch = _targetPitch;
            _currentVelocity = Vector3.Zero;
        }
    }

    public void Update(float deltaTime)
    {
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
            moveDir += Up;
        }

        if (_moveDown)
        {
            moveDir -= Up;
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
        Position = Vector3.Lerp(Position, _targetPosition, 1f - MathF.Exp(-MoveDamping * deltaTime));
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

        Position = Vector3.Lerp(Position, _targetPosition, 1f - MathF.Exp(-MoveDamping * deltaTime));
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
                    // In free-fly mode, scroll moves camera forward/backward
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
            case KeyCode.Space:
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
                if (Mode == CameraMode.Orbit)
                {
                    OrbitTarget = Position + Forward * OrbitDistance;
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
            case KeyCode.Space:
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
        else
        {
            OrbitDistance = Vector3.Distance(Position, target);
        }
        Mode = CameraMode.Orbit;
    }

    public void ResetVelocity()
    {
        _currentVelocity = Vector3.Zero;
    }
}
