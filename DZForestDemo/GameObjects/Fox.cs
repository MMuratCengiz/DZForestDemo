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
        public FoxMaterial(Assets assets, GraphicsContext context) : base(context)
        {
            Name = "FoxMaterial";
            Albedo = assets.LoadTexture("Texture.png");
            GpuShader = assets.GetShader("Builtin/Shaders/Default");
        }
    }
    
    public Fox(Assets assets, Vector3? position = null) : base("Fox")
    {
        LocalPosition = position ?? new Vector3(0f, 0f, 0f);

        var material = assets.RegisterMaterial(new FoxMaterial(assets, assets.GraphicsContext));        
        var model = assets.LoadModel("Fox.glb");
        var materialComponent = AddComponent<MaterialComponent>();
        materialComponent.Material = material;
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
