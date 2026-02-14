using System.Numerics;

namespace NiziKit.Components;

[NiziComponent]
public partial class CharacterController : IComponent
{
    [JsonProperty("moveSpeed")]
    public partial float MoveSpeed { get; set; }

    [JsonProperty("jumpForce")]
    public partial float JumpForce { get; set; }

    [JsonProperty("groundCheckDistance")]
    public partial float GroundCheckDistance { get; set; }

    private Rigidbody? _rigidbody;

    public bool IsGrounded { get; private set; }

    public CharacterController()
    {
        MoveSpeed = 5f;
        JumpForce = 5f;
        GroundCheckDistance = 0.3f;
    }

    public void Begin()
    {
        _rigidbody = Owner?.GetComponent<Rigidbody>();
    }

    private Rigidbody? GetRigidbody()
    {
        _rigidbody ??= Owner?.GetComponent<Rigidbody>();
        return _rigidbody;
    }

    public void Move(Vector3 direction)
    {
        if (GetRigidbody() == null)
        {
            return;
        }

        var vel = _rigidbody.Velocity;
        vel.X = direction.X * MoveSpeed;
        vel.Z = direction.Z * MoveSpeed;
        _rigidbody.Velocity = vel;
    }

    public void Jump()
    {
        if (GetRigidbody() == null || !IsGrounded)
        {
            return;
        }

        var vel = _rigidbody.Velocity;
        vel.Y = JumpForce;
        _rigidbody.Velocity = vel;
    }

    public void Update()
    {
        CheckGrounded();
        if (GetRigidbody() != null)
        {
            _rigidbody!.AngularVelocity = Vector3.Zero;
        }
    }

    private void CheckGrounded()
    {
        if (Owner == null)
        {
            return;
        }

        var origin = Owner.LocalPosition + Vector3.UnitY * 0.1f;
        IsGrounded = Physics.Physics.Raycast(origin, -Vector3.UnitY, GroundCheckDistance + 0.1f, out _);
    }
}
