using System.Numerics;
using DenOfIz;
using NiziKit.Components;
using NiziKit.Inputs;

namespace DZForestDemo.Script;

public class JumpController : NiziComponent
{
    private CharacterController? _controller;

    public override void Begin()
    {
        _controller = GetComponent<CharacterController>();
    }

    public override void Update()
    {
        _controller ??= GetComponent<CharacterController>();
        if (_controller == null)
        {
            return;
        }

        var move = Vector3.Zero;
        if (Input.GetKey(KeyCode.W))
        {
            move.Z -= 1;
        }

        if (Input.GetKey(KeyCode.S))
        {
            move.Z += 1;
        }

        if (Input.GetKey(KeyCode.A))
        {
            move.X -= 1;
        }

        if (Input.GetKey(KeyCode.D))
        {
            move.X += 1;
        }

        if (move != Vector3.Zero)
        {
            move = Vector3.Normalize(move);
        }

        _controller.Move(move);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _controller.Jump();
        }
    }
}
