using System.Numerics;
using NiziKit.Animation;
using NiziKit.Assets;

namespace NiziKit.Components;

[NiziComponent]
public partial class AnimatorComponent
{
    [AssetRef(AssetRefType.Skeleton, "skeleton")]
    public partial Skeleton? Skeleton { get; set; }

    public string? SkeletonRef { get; set; }

    [JsonProperty("defaultAnimation")]
    public partial string? DefaultAnimation { get; set; }

    public Animator Animator { get; } = new();

    public ReadOnlySpan<Matrix4x4> BoneMatrices => Animator.BoneMatrices;

    public void Initialize()
    {
        if (Skeleton == null)
        {
            return;
        }

        var controller = new AnimatorController { Name = "Auto" };

        for (var i = 0; i < Skeleton.AnimationCount; i++)
        {
            var animName = Skeleton.AnimationNames[i];
            var state = controller.AddState(animName);
            state.Clip = Skeleton.GetAnimation((uint)i);
            state.LoopMode = AnimationLoopMode.Loop;

            if (controller.BaseLayer.DefaultState == null ||
                (DefaultAnimation != null && animName.Equals(DefaultAnimation, StringComparison.OrdinalIgnoreCase)))
            {
                controller.BaseLayer.DefaultState = state;
            }
        }

        Animator.Skeleton = Skeleton;
        Animator.Controller = controller;
        Animator.Initialize();
    }

    public void Update(float deltaTime)
    {
        Animator.Update(deltaTime);
    }

    public void Play(string animationName, int layerIndex = 0)
    {
        Animator.Play(animationName, layerIndex);
    }

    public void CrossFade(string animationName, float duration, int layerIndex = 0)
    {
        Animator.CrossFade(animationName, duration, layerIndex);
    }

    public void SetFloat(string name, float value) => Animator.SetFloat(name, value);

    public void SetBool(string name, bool value) => Animator.SetBool(name, value);

    public void SetInteger(string name, int value) => Animator.SetInteger(name, value);

    public void SetTrigger(string name) => Animator.SetTrigger(name);
}
