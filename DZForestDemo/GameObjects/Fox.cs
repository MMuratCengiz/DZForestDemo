using System.Numerics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics;

namespace DZForestDemo.GameObjects;

public class Fox : GameObject
{
    class FoxMaterial : Material
    {
        public FoxMaterial()
        {
            Name = "FoxMaterial";
            Albedo = Assets.LoadTexture("Texture.png");
            GpuShader = Assets.GetShader("Builtin/Shaders/Default");
        }
    }

    public Fox(Vector3? position = null) : base("Fox")
    {
        LocalPosition = position ?? new Vector3(0f, 0f, 0f);

        var material = Assets.RegisterMaterial(new FoxMaterial());
        var model = Assets.LoadModel("Fox.glb");
        var materialComponent = AddComponent<MaterialComponent>();
        materialComponent.Material = material;
        AddComponent<MeshComponent>().Mesh = model.Meshes[0];

        var skeleton = Assets.LoadSkeleton("Fox_skeleton.ozz");
        var animation = Assets.LoadAnimation("Fox_Run.ozz", skeleton);
        var animator = AddComponent<AnimatorComponent>();
        animator.Skeleton = skeleton;
        animator.CurrentAnimation = animation;
        animator.IsPlaying = true;
        animator.Loop = true;
    }
}
