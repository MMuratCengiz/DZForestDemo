using System.Numerics;
using NiziKit.Animation;
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
            Variants = ShaderVariants.Skinned();
            GpuShader = Assets.GetShader("Builtin/Shaders/Default", Variants);
        }
    }

    private Animator? _animator;

    public Fox(Vector3? position = null) : base("Fox")
    {
        LocalPosition = position ?? new Vector3(0f, 0f, 0f);

        var material = Assets.RegisterMaterial(new FoxMaterial());
        var model = Assets.LoadModel("Fox.glb");
        var materialComponent = AddComponent<MaterialComponent>();
        materialComponent.Material = material;
        AddComponent<MeshComponent>().Mesh = model.Meshes[0];

        var skeleton = Assets.LoadSkeleton("Fox_skeleton.ozz");
        var runAnimation = Assets.LoadAnimation("Fox_Run.ozz", skeleton);
        var surveyAnimation = Assets.LoadAnimation("Fox_Survey.ozz", skeleton);

        var controller = CreateFoxController(runAnimation, surveyAnimation);

        _animator = AddComponent<Animator>();
        _animator.Skeleton = skeleton;
        _animator.Controller = controller;
        _animator.Initialize();
    }

    private static AnimatorController CreateFoxController(
        NiziKit.Assets.Animation runAnimation,
        NiziKit.Assets.Animation surveyAnimation)
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

        runState.AddTransition(surveyState)
            .AddCondition("Survey", AnimatorConditionMode.If);

        surveyState.AddTransition(runState)
            .AddCondition("IsRunning", AnimatorConditionMode.If);
        surveyState.Transitions[0].HasExitTime = true;
        surveyState.Transitions[0].ExitTime = 0.9f;

        return controller;
    }

    public void TriggerSurvey() => _animator?.SetTrigger("Survey");
    public void SetSpeed(float speed) => _animator?.SetFloat("Speed", speed);
    public void SetRunning(bool isRunning) => _animator?.SetBool("IsRunning", isRunning);
}
