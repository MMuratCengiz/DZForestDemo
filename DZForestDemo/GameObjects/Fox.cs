using System.Numerics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace DZForestDemo.GameObjects;

public class Fox : GameObject
{
    public Fox(Assets assets, Vector3? position = null) : base("Fox")
    {
        LocalPosition = position ?? new Vector3(-4f, -1.5f, 0f);

        var model = assets.LoadModel("Fox.glb");
        AddComponent<MeshComponent>().Mesh = model.Meshes[0];

        var skeleton = assets.LoadSkeleton("Fox_skeleton.ozz");
        var animation = assets.LoadAnimation("Fox_Run.ozz", skeleton);
        var animator = AddComponent<AnimatorComponent>();
        animator.Skeleton = skeleton;
        animator.CurrentAnimation = animation;
        animator.IsPlaying = true;
        animator.Loop = true;
    }
}
