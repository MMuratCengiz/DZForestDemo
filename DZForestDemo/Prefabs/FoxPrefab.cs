using System.Numerics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Graphics.Batching;
using NiziKit.SceneManagement;

namespace DZForestDemo.Prefabs;

public class FoxPrefab : IPrefab<GameObject>
{
    public Mesh Mesh { get; set; } = null!;
    public Texture? Texture { get; set; }
    public Skeleton? Skeleton { get; set; }
    public Animation? Animation { get; set; }
    public AnimationManager? AnimationManager { get; set; }

    public Vector3 Position { get; set; } = new(-4f, -1.5f, 0f);

    public GameObject Instantiate()
    {
        var fox = new GameObject("Fox");
        fox.LocalPosition = Position;

        var material = new RenderMaterial
        {
            BaseColor = new Vector4(1f, 1f, 1f, 1f),
            Metallic = 0f,
            Roughness = 0.8f,
            AmbientOcclusion = 1f,
            AlbedoTexture = Texture?.Id ?? TextureId.Invalid
        };

        var meshComponent = fox.AddComponent<MeshComponent>();
        meshComponent.Mesh = Mesh.Id;
        meshComponent.Material = material;
        meshComponent.Flags = RenderFlags.CastsShadow | RenderFlags.Skinned;

        if (Skeleton != null && AnimationManager != null)
        {
            var animator = fox.AddComponent<AnimatorComponent>();
            animator.Skeleton = Skeleton;
            animator.CurrentAnimation = Animation;
            animator.IsPlaying = true;
            animator.Loop = true;
        }

        return fox;
    }
}
