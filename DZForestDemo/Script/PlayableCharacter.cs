using NiziKit.Animation;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Inputs;
using NiziKit.Physics;

namespace DZForestDemo.Script;

public class PlayableCharacter : NiziComponent
{
    private Animator? _animator;

    public override void Begin()
    {
        _animator = GetComponent<Animator>();
    }

    public override void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var camera = World.CurrentScene!.GetActiveCamera()!;
            if (Physics.Raycast(camera.WorldPosition, camera.Forward, 100.0f, out var hit))
            {
                Console.WriteLine($"Hit! {hit}");
            }
        }
    }
}
