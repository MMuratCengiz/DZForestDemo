using System.Numerics;
using NiziKit.Animation;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.GLTF;

namespace DZForestDemo.GameObjects;

public class Fox : GameObject
{
    class FoxMaterial : Material
    {
        public FoxMaterial()
        {
            Name = "FoxMaterial";
            Albedo = Assets.LoadTexture("Texture.png");
            Variants = ShaderVariants.ToDefines("SKINNED");
            GpuShader = Assets.GetShader("Builtin/Shaders/Default", Variants);
        }
    }

    private readonly Animator? _animator;
    private readonly bool _useLayerBlending;
    private float _speed = 1.0f;

    public Fox(Vector3? position = null, bool useLayerBlending = false) : base("Fox")
    {
        _useLayerBlending = useLayerBlending;
        LocalPosition = position ?? new Vector3(0f, 0f, 0f);
        LocalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI / 2);

        var material = Assets.RegisterMaterial(new FoxMaterial());
        var gltf = GltfModel.Load("Fox.glb");
        var materialComponent = AddComponent<MaterialComponent>();
        materialComponent.Material = material;
        AddComponent<MeshComponent>().Mesh = gltf.Meshes[0];

        var skeleton = gltf.GetSkeleton();

        _animator = AddComponent<Animator>();
        _animator.Skeleton = skeleton;
        _animator.DefaultAnimation = "Run";
        _animator.Initialize();

        if (_useLayerBlending)
        {
            // Add a second layer for overlay animations
            _animator.AddLayer(0f, BlendMode.Override);
            _animator.PlayOnLayer(1, "Survey");
        }
    }

    public void TriggerSurvey()
    {
        _animator?.CrossFade("Survey", 0.3f, LoopMode.Once);
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
        if (_animator != null)
        {
            _animator.Speed = speed;
        }
    }

    public void SetRunning(bool isRunning)
    {
        if (_animator == null) return;

        if (isRunning && _animator.CurrentAnimation != "Run")
        {
            _animator.CrossFade("Run", 0.3f);
        }
    }

    public void SetOverlayWeight(float weight)
    {
        if (_animator != null && _useLayerBlending)
        {
            _animator.SetLayerWeight(1, weight);
        }
    }

    public void CrossFadeToSurvey()
    {
        _animator?.CrossFade("Survey", 0.5f, LoopMode.Once);
    }

    public void CrossFadeToRun()
    {
        _animator?.CrossFade("Run", 0.3f);
    }

    public bool IsInTransition => _animator?.IsBlending ?? false;
    public string? CurrentStateName => _animator?.CurrentAnimation;
    public float NormalizedTime => _animator?.NormalizedTime ?? 0;
}
