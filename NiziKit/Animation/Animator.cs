using System.Numerics;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Animation;

public class AnimatorEventArgs : EventArgs
{
    public AnimatorState State { get; init; } = null!;
    public int LayerIndex { get; init; }
}

[NiziComponent(TypeName = "animator")]
public partial class Animator : IDisposable
{
    private const int MaxBones = 256;
    private const int MaxTransitionsPerFrame = 8;

    [AssetRef(AssetRefType.Skeleton, "skeleton")]
    public partial Skeleton? Skeleton { get; set; }

    [HideInInspector]
    public string? SkeletonRef { get; set; }

    [AnimationSelector("Skeleton")]
    [JsonProperty("defaultAnimation")]
    public partial string? DefaultAnimation { get; set; }

    [DontSerialize]
    public AnimatorController? Controller { get; set; }

    public event EventHandler<AnimatorEventArgs>? StateCompleted;
    public event EventHandler<AnimatorEventArgs>? StateStarted;

    private Dictionary<string, AnimatorParameter> _parameters = [];
    private readonly AnimatorLayerState[] _layerStates = new AnimatorLayerState[8];
    private readonly Matrix4x4[] _boneMatrices = new Matrix4x4[MaxBones];
    private readonly Matrix4x4[] _layerTransforms = new Matrix4x4[MaxBones];
    private Matrix4x4[]? _inverseBindMatrices;
    private Matrix4x4 _nodeTransform = Matrix4x4.Identity;
    private Float4x4Array.Pinned? _ozzTransforms;
    private Float4x4Array.Pinned? _ozzBlendTransforms;
    private bool _initialized;

    public int BoneCount { get; private set; }
    public ReadOnlySpan<Matrix4x4> BoneMatrices => _boneMatrices.AsSpan(0, BoneCount);

    public Animator()
    {
        for (var i = 0; i < _layerStates.Length; i++)
        {
            _layerStates[i] = new AnimatorLayerState();
        }

        for (var i = 0; i < MaxBones; i++)
        {
            _boneMatrices[i] = Matrix4x4.Identity;
            _layerTransforms[i] = Matrix4x4.Identity;
        }
    }

    public void Initialize()
    {
        if (Skeleton == null)
        {
            return;
        }

        if (Controller == null)
        {
            var controller = new AnimatorController { Name = "Auto" };

            for (var i = 0; i < Skeleton.AnimationCount; i++)
            {
                var animName = Skeleton.AnimationNames[i];

                Assets.Animation? clip;
                try
                {
                    clip = Skeleton.GetAnimation((uint)i);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Animator] Skipping animation '{animName}': {ex.Message}");
                    continue;
                }

                var state = controller.AddState(animName);
                state.Clip = clip;
                state.LoopMode = AnimationLoopMode.Loop;

                if (controller.BaseLayer.DefaultState == null ||
                    (DefaultAnimation != null && animName.Equals(DefaultAnimation, StringComparison.OrdinalIgnoreCase)))
                {
                    controller.BaseLayer.DefaultState = state;
                }
            }

            Controller = controller;
        }

        _parameters = Controller.CloneParameters();
        BoneCount = Math.Min(Skeleton.JointCount, MaxBones);
        _ozzTransforms = Float4x4Array.Create(new Matrix4x4[BoneCount]);
        _ozzBlendTransforms = Float4x4Array.Create(new Matrix4x4[BoneCount]);
        _initialized = true;

        var meshComponent = Owner?.GetComponent<MeshComponent>();
        _inverseBindMatrices = meshComponent?.Mesh?.InverseBindMatrices;
        _nodeTransform = meshComponent?.Mesh?.NodeTransform ?? Matrix4x4.Identity;

        for (var i = 0; i < Controller.Layers.Count && i < _layerStates.Length; i++)
        {
            var layer = Controller.Layers[i];
            _layerStates[i].CurrentState = layer.DefaultState;
            _layerStates[i].StateTime = 0;
            _layerStates[i].NormalizedTime = 0;
            _layerStates[i].PlaybackDirection = 1;
        }
    }

    public void Update(float deltaTime)
    {
        if (Controller == null || Skeleton == null)
        {
            return;
        }

        for (var i = 0; i < Controller.Layers.Count && i < _layerStates.Length; i++)
        {
            UpdateLayer(i, deltaTime);
        }

        ComputeFinalBoneMatrices();
    }

    private void UpdateLayer(int layerIndex, float deltaTime)
    {
        var layer = Controller!.Layers[layerIndex];
        var state = _layerStates[layerIndex];
        state.TransitionsThisFrame = 0;

        if (state.CurrentState == null)
        {
            return;
        }

        CheckAnyStateTransitions(layer, state, layerIndex);

        if (state.IsTransitioning)
        {
            if (state.CurrentTransition?.CanInterruptSource == true)
            {
                CheckTransitions(state, layerIndex);
            }
            UpdateTransition(state, deltaTime, layerIndex);
        }
        else
        {
            UpdateState(state, deltaTime, layerIndex);
            CheckTransitions(state, layerIndex);
        }

        SampleLayer(layerIndex);
    }

    private void UpdateState(AnimatorLayerState state, float deltaTime, int layerIndex)
    {
        if (state.CurrentState?.Clip == null)
        {
            return;
        }

        var speed = state.CurrentState.GetSpeed(_parameters);
        var effectiveSpeed = speed * state.PlaybackDirection;
        state.StateTime += deltaTime * effectiveSpeed;

        var duration = state.CurrentState.Clip.Duration;
        if (duration <= 0)
        {
            return;
        }

        var wasComplete = false;

        switch (state.CurrentState.LoopMode)
        {
            case AnimationLoopMode.Loop:
                if (state.StateTime >= duration)
                {
                    state.StateTime = state.StateTime % duration;
                    wasComplete = true;
                }
                else if (state.StateTime < 0)
                {
                    state.StateTime = duration - ((-state.StateTime) % duration);
                    if (state.StateTime >= duration)
                    {
                        state.StateTime = 0;
                    }

                    wasComplete = true;
                }
                break;

            case AnimationLoopMode.PingPong:
                if (state.StateTime >= duration)
                {
                    state.StateTime = duration - (state.StateTime - duration);
                    state.PlaybackDirection = -1;
                    if (state.StateTime < 0)
                    {
                        state.StateTime = 0;
                    }
                }
                else if (state.StateTime < 0)
                {
                    state.StateTime = -state.StateTime;
                    state.PlaybackDirection = 1;
                    if (state.StateTime > duration)
                    {
                        state.StateTime = duration;
                    }

                    wasComplete = true;
                }
                break;

            case AnimationLoopMode.Once:
            default:
                if (state.StateTime >= duration)
                {
                    state.StateTime = duration;
                    wasComplete = !state.HasCompleted;
                    state.HasCompleted = true;
                }
                else if (state.StateTime < 0)
                {
                    state.StateTime = 0;
                    wasComplete = !state.HasCompleted;
                    state.HasCompleted = true;
                }
                break;
        }

        state.NormalizedTime = state.StateTime / duration;

        if (wasComplete && state.CurrentState != null)
        {
            StateCompleted?.Invoke(this, new AnimatorEventArgs
            {
                State = state.CurrentState,
                LayerIndex = layerIndex
            });
        }
    }

    private void CheckTransitions(AnimatorLayerState state, int layerIndex)
    {
        if (state.CurrentState == null || state.TransitionsThisFrame >= MaxTransitionsPerFrame)
        {
            return;
        }

        foreach (var transition in state.CurrentState.GetOrderedTransitions())
        {
            if (transition.CanTransition(_parameters, state.NormalizedTime))
            {
                StartTransition(state, transition, layerIndex);
                break;
            }
        }
    }

    private void CheckAnyStateTransitions(AnimatorLayer layer, AnimatorLayerState state, int layerIndex)
    {
        if (layer.AnyState == null || state.TransitionsThisFrame >= MaxTransitionsPerFrame)
        {
            return;
        }

        foreach (var transition in layer.AnyState.GetOrderedTransitions())
        {
            if (transition.DestinationState == state.CurrentState)
            {
                continue;
            }

            if (transition.CanTransition(_parameters, state.NormalizedTime))
            {
                StartTransition(state, transition, layerIndex);
                break;
            }
        }
    }

    private void StartTransition(AnimatorLayerState state, AnimatorTransition transition, int layerIndex)
    {
        state.TransitionsThisFrame++;
        state.PreviousState = state.CurrentState;
        state.PreviousStateTime = state.StateTime;
        state.PreviousPlaybackDirection = state.PlaybackDirection;
        state.CurrentState = transition.DestinationState;
        state.CurrentTransition = transition;
        state.StateTime = transition.Offset * (state.CurrentState?.Clip?.Duration ?? 0);
        state.NormalizedTime = transition.Offset;
        state.TransitionDuration = transition.Duration;
        state.TransitionTime = 0;
        state.IsTransitioning = true;
        state.PlaybackDirection = 1;
        state.HasCompleted = false;

        ResetTriggersInConditions(transition);

        if (state.CurrentState != null)
        {
            StateStarted?.Invoke(this, new AnimatorEventArgs
            {
                State = state.CurrentState,
                LayerIndex = layerIndex
            });
        }
    }

    private void ResetTriggersInConditions(AnimatorTransition transition)
    {
        foreach (var condition in transition.Conditions)
        {
            if (_parameters.TryGetValue(condition.ParameterName, out var param) &&
                param.Type == AnimatorParameterType.Trigger)
            {
                param.ResetTrigger();
            }
        }
    }

    private void UpdateTransition(AnimatorLayerState state, float deltaTime, int layerIndex)
    {
        state.TransitionTime += deltaTime;

        if (state.PreviousState?.Clip != null)
        {
            var speed = state.PreviousState.GetSpeed(_parameters);
            state.PreviousStateTime += deltaTime * speed * state.PreviousPlaybackDirection;

            var duration = state.PreviousState.Clip.Duration;
            if (duration > 0 && state.PreviousState.LoopMode == AnimationLoopMode.Loop)
            {
                if (state.PreviousStateTime >= duration)
                {
                    state.PreviousStateTime = state.PreviousStateTime % duration;
                }
                else if (state.PreviousStateTime < 0)
                {
                    state.PreviousStateTime = duration - ((-state.PreviousStateTime) % duration);
                }
            }
        }

        UpdateState(state, deltaTime, layerIndex);

        if (state.TransitionTime >= state.TransitionDuration)
        {
            state.IsTransitioning = false;
            state.PreviousState = null;
            state.CurrentTransition = null;
        }
    }

    private void SampleLayer(int layerIndex)
    {
        var layer = Controller!.Layers[layerIndex];
        var state = _layerStates[layerIndex];

        if (Skeleton == null || !_initialized)
        {
            return;
        }

        if (state.IsTransitioning && state.TransitionDuration > 0 && state.CurrentTransition != null)
        {
            var linearWeight = Math.Clamp(state.TransitionTime / state.TransitionDuration, 0, 1);
            var blendWeight = state.CurrentTransition.ApplyCurve(linearWeight);
            SampleBlended(state, blendWeight);
        }
        else
        {
            SampleSingle(state.CurrentState, state.NormalizedTime, _ozzTransforms);
        }

        ApplyLayerBlending(layerIndex, layer);
    }

    private void SampleSingle(AnimatorState? animState, float normalizedTime, Float4x4Array outTransforms)
    {
        if (animState?.Clip == null || Skeleton == null)
        {
            return;
        }

        var samplingDesc = new SamplingJobDesc
        {
            Context = animState.Clip.OzzContext,
            Ratio = normalizedTime,
            OutTransforms = outTransforms
        };

        Skeleton.OzzSkeleton.RunSamplingJob(in samplingDesc);
    }

    private void SampleBlended(AnimatorLayerState state, float blendWeight)
    {
        if (Skeleton == null || !_initialized)
        {
            return;
        }

        var prevNormalized = 0f;
        if (state.PreviousState?.Clip != null && state.PreviousState.Clip.Duration > 0)
        {
            prevNormalized = Math.Clamp(state.PreviousStateTime / state.PreviousState.Clip.Duration, 0, 1);
        }

        SampleSingle(state.PreviousState, prevNormalized, _ozzBlendTransforms);
        SampleSingle(state.CurrentState, state.NormalizedTime, _ozzTransforms);

        var prevSpan = _ozzBlendTransforms!.Value.AsSpan();
        var currSpan = _ozzTransforms!.Value.AsSpan();

        for (var i = 0; i < BoneCount; i++)
        {
            currSpan[i] = Matrix4x4.Lerp(prevSpan[i], currSpan[i], blendWeight);
        }
    }

    private void ApplyLayerBlending(int layerIndex, AnimatorLayer layer)
    {
        if (!_initialized)
        {
            return;
        }

        var transforms = _ozzTransforms!.Value.AsSpan();

        if (layerIndex == 0)
        {
            for (var i = 0; i < BoneCount; i++)
            {
                _layerTransforms[i] = transforms[i];
            }
            return;
        }

        var weight = layer.Weight;
        if (weight <= 0)
        {
            return;
        }

        switch (layer.BlendMode)
        {
            case AnimatorLayerBlendMode.Override:
                for (var i = 0; i < BoneCount; i++)
                {
                    _layerTransforms[i] = Matrix4x4.Lerp(_layerTransforms[i], transforms[i], weight);
                }
                break;

            case AnimatorLayerBlendMode.Additive:
                for (var i = 0; i < BoneCount; i++)
                {
                    var additive = transforms[i] - Matrix4x4.Identity;
                    _layerTransforms[i] += additive * weight;
                }
                break;
        }
    }

    private void ComputeFinalBoneMatrices()
    {
        if (Skeleton == null)
        {
            return;
        }

        for (var i = 0; i < BoneCount; i++)
        {
            var inverseBindMatrix = _inverseBindMatrices != null && i < _inverseBindMatrices.Length
                ? _inverseBindMatrices[i]
                : Matrix4x4.Identity;

            _boneMatrices[i] = inverseBindMatrix * _layerTransforms[i] * _nodeTransform;
        }
    }

    public void SetFloat(string name, float value)
    {
        if (_parameters.TryGetValue(name, out var param))
        {
            param.FloatValue = value;
        }
    }

    public void SetInteger(string name, int value)
    {
        if (_parameters.TryGetValue(name, out var param))
        {
            param.IntValue = value;
        }
    }

    public void SetBool(string name, bool value)
    {
        if (_parameters.TryGetValue(name, out var param))
        {
            param.BoolValue = value;
        }
    }

    public void SetTrigger(string name)
    {
        if (_parameters.TryGetValue(name, out var param))
        {
            param.SetTrigger();
        }
    }

    public void ResetTrigger(string name)
    {
        if (_parameters.TryGetValue(name, out var param))
        {
            param.ResetTrigger();
        }
    }

    public float GetFloat(string name) =>
        _parameters.TryGetValue(name, out var param) ? param.FloatValue : 0;

    public int GetInteger(string name) =>
        _parameters.TryGetValue(name, out var param) ? param.IntValue : 0;

    public bool GetBool(string name) =>
        _parameters.TryGetValue(name, out var param) && param.BoolValue;

    public bool HasParameter(string name) => _parameters.ContainsKey(name);

    public void Play(string stateName, int layerIndex = 0)
    {
        if (Controller == null || layerIndex >= Controller.Layers.Count)
        {
            return;
        }

        var layer = Controller.Layers[layerIndex];
        var state = layer.GetState(stateName);
        if (state == null)
        {
            return;
        }

        var layerState = _layerStates[layerIndex];
        layerState.CurrentState = state;
        layerState.StateTime = 0;
        layerState.NormalizedTime = 0;
        layerState.IsTransitioning = false;
        layerState.PlaybackDirection = 1;
        layerState.HasCompleted = false;

        StateStarted?.Invoke(this, new AnimatorEventArgs
        {
            State = state,
            LayerIndex = layerIndex
        });
    }

    public void CrossFade(string stateName, float duration, int layerIndex = 0, TransitionCurve curve = TransitionCurve.Linear)
    {
        if (Controller == null || layerIndex >= Controller.Layers.Count)
        {
            return;
        }

        var layer = Controller.Layers[layerIndex];
        var destState = layer.GetState(stateName);
        if (destState == null)
        {
            return;
        }

        var layerState = _layerStates[layerIndex];
        var transition = new AnimatorTransition
        {
            DestinationState = destState,
            Duration = duration,
            Curve = curve
        };
        StartTransition(layerState, transition, layerIndex);
    }

    public void SetLayerWeight(int layerIndex, float weight)
    {
        if (Controller == null || layerIndex >= Controller.Layers.Count)
        {
            return;
        }

        Controller.Layers[layerIndex].Weight = Math.Clamp(weight, 0f, 1f);
    }

    public float GetLayerWeight(int layerIndex)
    {
        if (Controller == null || layerIndex >= Controller.Layers.Count)
        {
            return 0;
        }

        return Controller.Layers[layerIndex].Weight;
    }

    public AnimatorState? GetCurrentState(int layerIndex = 0)
    {
        if (layerIndex >= _layerStates.Length)
        {
            return null;
        }

        return _layerStates[layerIndex].CurrentState;
    }

    public float GetCurrentStateTime(int layerIndex = 0)
    {
        if (layerIndex >= _layerStates.Length)
        {
            return 0;
        }

        return _layerStates[layerIndex].StateTime;
    }

    public float GetCurrentStateNormalizedTime(int layerIndex = 0)
    {
        if (layerIndex >= _layerStates.Length)
        {
            return 0;
        }

        return _layerStates[layerIndex].NormalizedTime;
    }

    public bool IsInTransition(int layerIndex = 0)
    {
        if (layerIndex >= _layerStates.Length)
        {
            return false;
        }

        return _layerStates[layerIndex].IsTransitioning;
    }

    public bool HasStateCompleted(int layerIndex = 0)
    {
        if (layerIndex >= _layerStates.Length)
        {
            return false;
        }

        return _layerStates[layerIndex].HasCompleted;
    }

    public void Dispose()
    {
        _initialized = false;
        _ozzTransforms?.Dispose();
        _ozzBlendTransforms?.Dispose();
    }

    private class AnimatorLayerState
    {
        public AnimatorState? CurrentState;
        public AnimatorState? PreviousState;
        public AnimatorTransition? CurrentTransition;
        public float StateTime;
        public float PreviousStateTime;
        public float NormalizedTime;
        public bool IsTransitioning;
        public float TransitionTime;
        public float TransitionDuration;
        public int PlaybackDirection = 1;
        public int PreviousPlaybackDirection = 1;
        public int TransitionsThisFrame;
        public bool HasCompleted;
    }
}
