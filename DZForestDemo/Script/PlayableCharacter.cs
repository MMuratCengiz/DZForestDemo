using System;
using System.Numerics;
using DenOfIz;
using NiziKit.Animation;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Inputs;
using NiziKit.Physics;

namespace DZForestDemo.Script;

/// <summary>
/// State-machine character controller using a kinematic rigidbody.
/// Fully manages its own gravity, ground detection, movement, rotation, and animations.
/// Supports walk, run (Shift), crouch (C), 8-directional strafe, jumping, and click-to-move.
/// </summary>
public class PlayableCharacter : NiziComponent
{
    private enum State
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        Landing,
        CrouchIdle,
        CrouchMoving
    }

    private Animator? _animator;
    private Rigidbody? _rigidbody;
    private CameraComponent? _camera;

    private State _state = State.Idle;
    private string _currentAnim = "";
    private float _currentAnimSpeed = 1f;
    private Vector3? _clickTarget;
    private bool _crouching;
    private bool _sprinting;
    private float _landingTimer;

    private bool _isGrounded;
    private float _groundedBuffer;
    private float _groundY;

    /// <summary>
    /// Grace period after jump initiation during which ground detection is suppressed.
    /// Prevents raycasts from immediately re-grounding the character before it rises.
    /// </summary>
    private float _jumpGraceTimer;

    private float _jumpBufferTimer;
    private bool _jumpHeld;

    /// <summary>
    /// Controller-managed velocity. Kinematic body has no physics gravity,
    /// so we fully own both horizontal and vertical velocity.
    /// </summary>
    private Vector3 _velocity;

    private Quaternion _targetRotation;

    private Vector3 _smoothLookTarget;
    private bool _cameraInitialized;

    private const float CoyoteTime = 0.15f;
    private const float JumpBufferTime = 0.15f;
    private const float JumpGraceTime = 0.2f;
    private const float GroundProbeRadius = 0.35f;
    private const float GroundProbeDepth = 1.2f;
    private const float GroundSnapThreshold = 0.05f;

    public float MoveSpeed { get; set; } = 7f;
    public float RunSpeed { get; set; } = 12f;
    public float RunAnimSpeed { get; set; } = 1.7f;
    public float TurnSpeed { get; set; } = 12f;
    public float StoppingDistance { get; set; } = 0.2f;
    public float CrossFadeDuration { get; set; } = 0.15f;
    public float GroundAccelRate { get; set; } = 12f;
    public float GroundDecelRate { get; set; } = 18f;
    public float AirControlFactor { get; set; } = 0.3f;
    public float AirAccelRate { get; set; } = 4f;

    public float JumpForce { get; set; } = 12f;
    public float Gravity { get; set; } = 20f;
    public float FallGravityMultiplier { get; set; } = 2.5f;
    public float LowJumpGravityMultiplier { get; set; } = 2.0f;
    public float MaxFallSpeed { get; set; } = 25f;

    public float CameraDistance { get; set; } = 15f;
    public float CameraHeight { get; set; } = 10f;
    public float CameraSmoothSpeed { get; set; } = 5f;
    public float CameraLookHeight { get; set; } = 1.5f;
    public float CameraLookSmoothSpeed { get; set; } = 10f;

    public string AnimIdle { get; set; } = "A_Idle_Standing_Femn";
    public string AnimWalk { get; set; } = "A_Walk_FwdStrafeF_Femn";
    public string AnimStrafeL { get; set; } = "A_Walk_FwdStrafeL_Femn";
    public string AnimStrafeR { get; set; } = "A_Walk_FwdStrafeR_Femn";
    public string AnimWalkBack { get; set; } = "A_Walk_BckStrafeB_Femn";
    public string AnimWalkFL { get; set; } = "A_Walk_FwdStrafeFL_Femn";
    public string AnimWalkFR { get; set; } = "A_Walk_FwdStrafeFR_Femn";
    public string AnimWalkBL { get; set; } = "A_Walk_BckStrafeBL_Femn";
    public string AnimWalkBR { get; set; } = "A_Walk_BckStrafeBR_Femn";
    public string AnimJump { get; set; } = "A_Jump_Idle_Femn";
    public string AnimJumpWalk { get; set; } = "A_Jump_Walking_Femn";
    public string AnimJumpRun { get; set; } = "A_Jump_Running_Femn";
    public string AnimFall { get; set; } = "A_InAir_FallLarge_Femn";
    public string AnimLand { get; set; } = "A_Land_IdleHard_Femn";
    public string AnimCrouchIdle { get; set; } = "A_Idle_Crouching_Femn";
    public string AnimCrouchWalk { get; set; } = "A_POLY_BL_Crouch_FwdStrafeF_Femn";
    public string AnimTurnL { get; set; } = "A_Turn_Standing_180L_Femn";
    public string AnimTurnR { get; set; } = "A_Turn_Standing_180R_Femn";

    public override void Begin()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
        _camera = World.CurrentScene?.GetActiveCamera();
        _targetRotation = LocalRotation;
        _groundY = LocalPosition.Y;
        _velocity = Vector3.Zero;
        SetAnimation(AnimIdle);
    }

    public override void Update()
    {
        if (_rigidbody == null || Owner == null)
        {
            return;
        }

        var dt = Time.DeltaTime;

        UpdateGroundDetection(dt);
        UpdateJumpBuffer(dt);

        switch (_state)
        {
            case State.Idle:
            case State.Moving:
            case State.CrouchIdle:
            case State.CrouchMoving:
                HandleGroundState(dt);
                break;
            case State.Jumping:
            case State.Falling:
                UpdateAirborne(dt);
                break;
            case State.Landing:
                UpdateLanding(dt);
                break;
        }

        ApplyGravity(dt);
        ApplyMovement(dt);
    }

    public override void PostUpdate()
    {
        UpdateFollowCamera(Time.DeltaTime);
    }

    /// <summary>
    /// Applies asymmetric gravity to the controller's vertical velocity.
    /// Uses higher gravity when falling or when jump button is released early
    /// to create a snappy, weighted jump arc with variable height.
    /// </summary>
    private void ApplyGravity(float dt)
    {
        if (_isGrounded && _state != State.Jumping)
        {
            if (_velocity.Y < 0)
            {
                _velocity.Y = 0;
            }
            return;
        }

        float gravityScale;
        if (_velocity.Y < 0)
        {
            gravityScale = FallGravityMultiplier;
        }
        else if (_velocity.Y > 0 && !_jumpHeld)
        {
            gravityScale = LowJumpGravityMultiplier;
        }
        else
        {
            gravityScale = 1f;
        }

        _velocity.Y -= Gravity * gravityScale * dt;
        _velocity.Y = MathF.Max(_velocity.Y, -MaxFallSpeed);
    }

    /// <summary>
    /// Moves the kinematic body to the new position derived from the controller's
    /// velocity. Sets LocalPosition directly for immediate visual feedback, and
    /// also informs the physics engine via Move() for collision handling.
    /// Clamps to ground surface when grounded to prevent hovering or sinking.
    /// </summary>
    private void ApplyMovement(float dt)
    {
        if (_rigidbody == null || Owner == null)
        {
            return;
        }

        var newPos = LocalPosition + _velocity * dt;

        if (_isGrounded && _state != State.Jumping)
        {
            newPos.Y = _groundY;
            if (_velocity.Y < 0)
            {
                _velocity.Y = 0;
            }
        }

        Owner.LocalPosition = newPos;
        Owner.LocalRotation = _targetRotation;
        _rigidbody.Move(newPos, _targetRotation);
    }

    /// <summary>
    /// Probes for ground using 4 offset raycasts arranged around the character,
    /// positioned just outside the capsule collider radius to avoid self-intersection.
    /// Suppressed during the jump grace period so the character can cleanly leave the ground.
    /// Uses a coyote time buffer for stable ground state transitions.
    /// </summary>
    private void UpdateGroundDetection(float dt)
    {
        if (_jumpGraceTimer > 0)
        {
            _jumpGraceTimer -= dt;
            _groundedBuffer -= dt;
            _isGrounded = false;
            return;
        }

        var pos = LocalPosition;
        var down = -Vector3.UnitY;
        var originY = 0.15f;
        var bestGroundY = float.MinValue;
        var hitGround = false;

        Vector3[] offsets =
        [
            new(GroundProbeRadius, originY, 0),
            new(-GroundProbeRadius, originY, 0),
            new(0, originY, GroundProbeRadius),
            new(0, originY, -GroundProbeRadius)
        ];

        foreach (var offset in offsets)
        {
            if (Physics.Raycast(pos + offset, down, GroundProbeDepth, out var hit))
            {
                hitGround = true;
                if (hit.Point.Y > bestGroundY)
                {
                    bestGroundY = hit.Point.Y;
                }
            }
        }

        if (hitGround)
        {
            var distToGround = pos.Y - bestGroundY;
            if (distToGround < GroundProbeDepth - originY)
            {
                _groundedBuffer = CoyoteTime;
                _groundY = bestGroundY;
            }
        }
        else
        {
            _groundedBuffer -= dt;
        }

        _isGrounded = _groundedBuffer > 0f;
    }

    /// <summary>
    /// Tracks jump button press for input buffering. Allows the player to press
    /// jump slightly before landing and still have it register.
    /// </summary>
    private void UpdateJumpBuffer(float dt)
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _jumpBufferTimer = JumpBufferTime;
            _jumpHeld = true;
        }

        if (!Input.GetKey(KeyCode.Space))
        {
            _jumpHeld = false;
        }

        _jumpBufferTimer -= dt;
    }

    /// <summary>
    /// Smoothly interpolates horizontal velocity toward a target direction and speed
    /// using frame-rate independent exponential decay: lerp(current, target, 1 - e^(-rate * dt)).
    /// </summary>
    private void SmoothAccelerateXZ(Vector3 targetDir, float targetSpeed, float rate, float dt)
    {
        var t = 1f - MathF.Exp(-rate * dt);
        _velocity.X += (targetDir.X * targetSpeed - _velocity.X) * t;
        _velocity.Z += (targetDir.Z * targetSpeed - _velocity.Z) * t;
    }

    /// <summary>
    /// Smoothly decelerates horizontal velocity to zero.
    /// </summary>
    private void SmoothDecelerateXZ(float rate, float dt)
    {
        SmoothAccelerateXZ(Vector3.Zero, 0f, rate, dt);
    }

    /// <summary>
    /// Smoothly interpolates rotation toward a target using frame-rate independent slerp.
    /// </summary>
    private void SmoothRotateToward(Quaternion target, float dt)
    {
        var t = 1f - MathF.Exp(-TurnSpeed * dt);
        _targetRotation = Quaternion.Slerp(_targetRotation, target, t);
    }

    /// <summary>
    /// Returns the direction the mesh visually faces. The engine's Forward property
    /// uses Transform(+UnitZ, rotation), but this mesh faces -Z in local space,
    /// so the visual facing direction is the negation of Forward.
    /// </summary>
    private Vector3 GetVisualFacing()
    {
        var facing = -Forward;
        facing.Y = 0;
        if (facing.LengthSquared() < 0.001f)
        {
            return -Vector3.UnitZ;
        }
        return Vector3.Normalize(facing);
    }

    /// <summary>
    /// Handles input and state transitions for all grounded states.
    /// Supports walk (WASD), run (Shift+WASD), crouch (C toggle), jump (Space),
    /// strafe (RMB+WASD), and click-to-move (LMB).
    /// </summary>
    private void HandleGroundState(float dt)
    {
        if (!_isGrounded)
        {
            _state = State.Falling;
            SetAnimation(AnimFall, LoopMode.Loop);
            return;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            _crouching = !_crouching;
            if (_crouching)
            {
                _sprinting = false;
            }
        }

        _sprinting = !_crouching && Input.GetKey(KeyCode.Lshift);

        if (_jumpBufferTimer > 0 && !_crouching)
        {
            var wasMoving = _state == State.Moving || _state == State.CrouchMoving;
            _velocity.Y = JumpForce;
            _clickTarget = null;
            _groundedBuffer = 0f;
            _isGrounded = false;
            _jumpBufferTimer = 0f;
            _jumpGraceTimer = JumpGraceTime;
            _state = State.Jumping;

            string jumpAnim;
            if (_sprinting && wasMoving)
            {
                jumpAnim = AnimJumpRun;
            }
            else if (wasMoving)
            {
                jumpAnim = AnimJumpWalk;
            }
            else
            {
                jumpAnim = AnimJump;
            }
            SetAnimation(jumpAnim, LoopMode.Once);
            return;
        }

        var moveInput = Vector2.Zero;
        if (Input.GetKey(KeyCode.W)) { moveInput.Y += 1f; }
        if (Input.GetKey(KeyCode.S)) { moveInput.Y -= 1f; }
        if (Input.GetKey(KeyCode.A)) { moveInput.X -= 1f; }
        if (Input.GetKey(KeyCode.D)) { moveInput.X += 1f; }

        var hasWasd = moveInput.LengthSquared() > 0.001f;
        if (hasWasd)
        {
            _clickTarget = null;
        }

        if (Input.GetMouseButtonDown(MouseButton.Left) && !Input.GetMouseButton(MouseButton.Right))
        {
            if (_camera != null)
            {
                var mousePos = Input.MousePosition;
                var ray = _camera.ScreenPointToRay(
                    mousePos.X, mousePos.Y, GraphicsContext.Width, GraphicsContext.Height);
                if (Physics.Raycast(ray, 100.0f, out var hit))
                {
                    _clickTarget = hit.Point;
                }
            }
        }

        var moveDir = Vector3.Zero;

        if (hasWasd)
        {
            moveDir = GetCameraRelativeDirection(moveInput);
        }
        else if (_clickTarget.HasValue)
        {
            var toTarget = new Vector3(
                _clickTarget.Value.X, LocalPosition.Y, _clickTarget.Value.Z) - LocalPosition;
            if (toTarget.Length() <= StoppingDistance)
            {
                _clickTarget = null;
            }
            else
            {
                moveDir = Vector3.Normalize(toTarget);
            }
        }

        var isMoving = moveDir.LengthSquared() > 0.001f;
        var strafing = Input.GetMouseButton(MouseButton.Right) && hasWasd;

        if (isMoving)
        {
            float speed;
            float animSpeed;
            if (_crouching)
            {
                speed = MoveSpeed * 0.5f;
                animSpeed = 1f;
            }
            else if (_sprinting)
            {
                speed = RunSpeed;
                animSpeed = RunAnimSpeed;
            }
            else
            {
                speed = MoveSpeed;
                animSpeed = 1f;
            }

            SmoothAccelerateXZ(moveDir, speed, GroundAccelRate, dt);

            if (strafing)
            {
                if (_camera != null)
                {
                    var camFwd = _camera.Forward;
                    camFwd.Y = 0;
                    if (camFwd.LengthSquared() > 0.001f)
                    {
                        SmoothRotateToward(QuaternionFromDirection(Vector3.Normalize(camFwd)), dt);
                    }
                }

                var strafeAnim = PickStrafeAnimation(moveDir);
                _state = _crouching ? State.CrouchMoving : State.Moving;
                SetAnimation(_crouching ? AnimCrouchWalk : strafeAnim, LoopMode.Loop, animSpeed);
            }
            else
            {
                SmoothRotateToward(QuaternionFromDirection(moveDir), dt);
                _state = _crouching ? State.CrouchMoving : State.Moving;
                SetAnimation(_crouching ? AnimCrouchWalk : AnimWalk, LoopMode.Loop, animSpeed);
            }
        }
        else
        {
            SmoothDecelerateXZ(GroundDecelRate, dt);
            _state = _crouching ? State.CrouchIdle : State.Idle;
            SetAnimation(_crouching ? AnimCrouchIdle : AnimIdle);
        }
    }

    /// <summary>
    /// Handles air control and state transitions while jumping or falling.
    /// Jump animation plays for the entire rising phase, fall animation plays
    /// for the entire descent until landing.
    /// </summary>
    private void UpdateAirborne(float dt)
    {
        var moveInput = Vector2.Zero;
        if (Input.GetKey(KeyCode.W)) { moveInput.Y += 1f; }
        if (Input.GetKey(KeyCode.S)) { moveInput.Y -= 1f; }
        if (Input.GetKey(KeyCode.A)) { moveInput.X -= 1f; }
        if (Input.GetKey(KeyCode.D)) { moveInput.X += 1f; }

        if (moveInput.LengthSquared() > 0.001f)
        {
            var airDir = GetCameraRelativeDirection(moveInput);
            SmoothAccelerateXZ(airDir, MoveSpeed, AirAccelRate, dt);
        }

        if (_state == State.Jumping && _velocity.Y <= 0)
        {
            _state = State.Falling;
            SetAnimation(AnimFall, LoopMode.Loop);
        }

        if (_isGrounded && _velocity.Y <= 0)
        {
            _state = State.Landing;
            _landingTimer = 0.2f;
            SetAnimation(AnimLand, LoopMode.Once);
        }
    }

    /// <summary>
    /// Brief landing recovery with smooth deceleration, interruptible by movement input.
    /// </summary>
    private void UpdateLanding(float dt)
    {
        SmoothDecelerateXZ(GroundDecelRate, dt);
        _landingTimer -= dt;

        var hasInput = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) ||
                       Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D);

        if (_landingTimer <= 0 || hasInput)
        {
            _state = State.Idle;
            _crouching = false;
            _sprinting = false;
            SetAnimation(AnimIdle);
        }
    }

    /// <summary>
    /// Third-person follow camera with smoothed position and look target
    /// to eliminate visual jitter.
    /// </summary>
    private void UpdateFollowCamera(float dt)
    {
        if (_camera == null)
        {
            return;
        }

        var charPos = LocalPosition;
        var rawLookTarget = charPos + new Vector3(0, CameraLookHeight, 0);
        var desiredCamPos = charPos + new Vector3(0, CameraHeight, -CameraDistance);

        if (!_cameraInitialized)
        {
            _camera.Position = desiredCamPos;
            _smoothLookTarget = rawLookTarget;
            _cameraInitialized = true;
        }

        var posT = 1f - MathF.Exp(-CameraSmoothSpeed * dt);
        _camera.Position = Vector3.Lerp(_camera.Position, desiredCamPos, posT);

        var lookT = 1f - MathF.Exp(-CameraLookSmoothSpeed * dt);
        _smoothLookTarget = Vector3.Lerp(_smoothLookTarget, rawLookTarget, lookT);
        _camera.LookAt(_smoothLookTarget);
    }

    /// <summary>
    /// Transforms 2D input (WASD) into a 3D world direction relative to the camera orientation.
    /// </summary>
    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (_camera == null)
        {
            return new Vector3(input.X, 0, input.Y);
        }

        var camFwd = _camera.Forward;
        camFwd.Y = 0;
        if (camFwd.LengthSquared() < 0.001f)
        {
            camFwd = Vector3.UnitZ;
        }
        else
        {
            camFwd = Vector3.Normalize(camFwd);
        }

        var camRight = Vector3.Cross(Vector3.UnitY, camFwd);
        if (camRight.LengthSquared() < 0.001f)
        {
            camRight = Vector3.UnitX;
        }
        else
        {
            camRight = Vector3.Normalize(camRight);
        }

        var dir = camFwd * input.Y + camRight * input.X;
        if (dir.LengthSquared() > 0.001f)
        {
            dir = Vector3.Normalize(dir);
        }

        return dir;
    }

    /// <summary>
    /// Selects the correct 8-directional strafe animation based on the angle
    /// between the character's visual facing direction and the movement direction.
    /// </summary>
    private string PickStrafeAnimation(Vector3 moveDir)
    {
        var fwd = GetVisualFacing();

        moveDir.Y = 0;
        if (moveDir.LengthSquared() < 0.001f)
        {
            return AnimWalk;
        }
        moveDir = Vector3.Normalize(moveDir);

        var dot = Vector3.Dot(fwd, moveDir);
        var cross = fwd.X * moveDir.Z - fwd.Z * moveDir.X;
        var angle = MathF.Atan2(cross, dot) * (180f / MathF.PI);
        var absAngle = MathF.Abs(angle);

        if (absAngle < 22.5f) { return AnimWalk; }
        if (absAngle < 67.5f) { return angle > 0 ? AnimWalkFL : AnimWalkFR; }
        if (absAngle < 112.5f) { return angle > 0 ? AnimStrafeL : AnimStrafeR; }
        if (absAngle < 157.5f) { return angle > 0 ? AnimWalkBL : AnimWalkBR; }
        return AnimWalkBack;
    }

    /// <summary>
    /// Creates a Y-axis rotation quaternion that makes the mesh (faces -Z locally)
    /// face the given world direction.
    /// </summary>
    private static Quaternion QuaternionFromDirection(Vector3 direction)
    {
        direction.Y = 0;
        if (direction.LengthSquared() < 0.001f)
        {
            return Quaternion.Identity;
        }

        direction = Vector3.Normalize(direction);
        var yaw = MathF.Atan2(-direction.X, -direction.Z);
        return Quaternion.CreateFromYawPitchRoll(yaw, 0, 0);
    }

    /// <summary>
    /// Changes the current animation only if it differs from what's already playing,
    /// preventing CrossFade restarts on repeated calls. Also updates playback speed
    /// without retriggering the animation when only speed changes.
    /// </summary>
    private void SetAnimation(string anim, LoopMode loop = LoopMode.Loop, float speed = 1f)
    {
        if (_animator == null)
        {
            return;
        }

        if (_currentAnim != anim)
        {
            _currentAnim = anim;
            _currentAnimSpeed = speed;
            _animator.Speed = speed;
            _animator.CrossFade(anim, CrossFadeDuration, loop);
        }
        else if (MathF.Abs(_currentAnimSpeed - speed) > 0.01f)
        {
            _currentAnimSpeed = speed;
            _animator.Speed = speed;
        }
    }
}
