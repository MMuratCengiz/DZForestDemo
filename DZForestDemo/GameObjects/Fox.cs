using System.Numerics;
using NiziKit.Animation;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics;
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
            Variants = ShaderVariants.Skinned();
            GpuShader = Assets.GetShader("Builtin/Shaders/Default", Variants);
        }
    }

    private readonly Animator? _animator;
    private readonly bool _useLayerBlending;

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
        var runAnimation = skeleton.GetAnimation("Run");
        var surveyAnimation = skeleton.GetAnimation("Survey");

        var controller = useLayerBlending
            ? CreateLayerBlendController(runAnimation, surveyAnimation)
            : CreateFoxController(runAnimation, surveyAnimation);

        _animator = AddComponent<Animator>();
        _animator.Skeleton = skeleton;
        _animator.Controller = controller;
        _animator.Initialize();

        _animator.StateCompleted += (_, e) =>
        {
            Console.WriteLine($"Fox: State '{e.State.Name}' completed on layer {e.LayerIndex}");
        };
    }

    private static AnimatorController CreateFoxController(Animation runAnimation, Animation surveyAnimation)
    {
        var controller = new AnimatorController { Name = "FoxController" };

        controller.AddBool("IsRunning", true);
        controller.AddTrigger("Survey");
        controller.AddFloat("Speed", 1.0f);

        var runState = controller.AddState("Run");
        runState.Clip = runAnimation;
        runState.Loop = true;
        runState.SpeedParameterName = "Speed";

        var surveyState = controller.AddState("Survey");
        surveyState.Clip = surveyAnimation;
        surveyState.Loop = false;

        var toSurvey = runState.AddTransition(surveyState);
        toSurvey.AddCondition("Survey", AnimatorConditionMode.If);
        toSurvey.Duration = 0.3f;
        toSurvey.Curve = TransitionCurve.EaseInOut;

        var toRun = surveyState.AddTransition(runState);
        toRun.AddCondition("IsRunning", AnimatorConditionMode.If);
        toRun.HasExitTime = true;
        toRun.ExitTime = 0.9f;
        toRun.Duration = 0.4f;
        toRun.Curve = TransitionCurve.EaseOut;

        return controller;
    }

    private static AnimatorController CreateLayerBlendController(Animation runAnimation, Animation surveyAnimation)
    {
        var controller = new AnimatorController { Name = "FoxLayerController" };

        controller.AddFloat("OverlayWeight", 0.0f);

        var runState = controller.AddState("Run", 0);
        runState.Clip = runAnimation;
        runState.Loop = true;

        var overlayLayer = controller.AddLayer("SurveyOverlay");
        overlayLayer.Weight = 0.0f;
        overlayLayer.BlendMode = AnimatorLayerBlendMode.Override;

        var surveyOverlay = controller.AddState("SurveyOverlay", 1);
        surveyOverlay.Clip = surveyAnimation;
        surveyOverlay.Loop = true;

        return controller;
    }

    public void TriggerSurvey() => _animator?.SetTrigger("Survey");
    public void SetSpeed(float speed) => _animator?.SetFloat("Speed", speed);
    public void SetRunning(bool isRunning) => _animator?.SetBool("IsRunning", isRunning);

    public void SetOverlayWeight(float weight)
    {
        if (_animator != null && _useLayerBlending)
        {
            _animator.SetLayerWeight(1, weight);
        }
    }

    public void CrossFadeToSurvey(TransitionCurve curve = TransitionCurve.EaseInOut)
    {
        _animator?.CrossFade("Survey", 0.5f, 0, curve);
    }

    public void CrossFadeToRun(TransitionCurve curve = TransitionCurve.EaseOut)
    {
        _animator?.CrossFade("Run", 0.3f, 0, curve);
    }

    public bool IsInTransition => _animator?.IsInTransition() ?? false;
    public string? CurrentStateName => _animator?.GetCurrentState()?.Name;
    public float NormalizedTime => _animator?.GetCurrentStateNormalizedTime() ?? 0;
}
