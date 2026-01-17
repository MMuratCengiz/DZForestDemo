using System.Numerics;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Animation;

public class Animator : IComponent, IDisposable
{
    private const int MaxBones = 256;

    public GameObject? Owner { get; set; }
    public Skeleton? Skeleton { get; set; }
    public AnimatorController? Controller { get; set; }

    private Dictionary<string, AnimatorParameter> _parameters = [];
    private readonly AnimatorLayerState[] _layerStates = new AnimatorLayerState[8];
    private readonly Matrix4x4[] _boneMatrices = new Matrix4x4[MaxBones];
    private readonly Matrix4x4[] _sampledTransforms = new Matrix4x4[MaxBones];
    private readonly Matrix4x4[] _blendTransforms = new Matrix4x4[MaxBones];
    private Matrix4x4[]? _inverseBindMatrices;
    private Matrix4x4 _nodeTransform = Matrix4x4.Identity;
    private Float4x4Array? _ozzTransforms;

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
        }
    }

    public void Initialize()
    {
        if (Controller == null || Skeleton == null)
        {
            return;
        }

        _parameters = Controller.CloneParameters();
        BoneCount = Math.Min(Skeleton.JointCount, MaxBones);
        _ozzTransforms = Float4x4Array.Create(new Matrix4x4[BoneCount]);

        var meshComponent = Owner?.GetComponent<MeshComponent>();
        _inverseBindMatrices = meshComponent?.Mesh?.InverseBindMatrices;
        _nodeTransform = meshComponent?.Mesh?.NodeTransform ?? Matrix4x4.Identity;

        for (var i = 0; i < Controller.Layers.Count && i < _layerStates.Length; i++)
        {
            var layer = Controller.Layers[i];
            _layerStates[i].CurrentState = layer.DefaultState;
            _layerStates[i].StateTime = 0;
            _layerStates[i].NormalizedTime = 0;
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

        if (state.CurrentState == null)
        {
            return;
        }

        CheckAnyStateTransitions(layer, state);

        if (state.IsTransitioning)
        {
            UpdateTransition(state, deltaTime);
        }
        else
        {
            UpdateState(state, deltaTime);
            CheckTransitions(state);
        }

        SampleLayer(layerIndex);
    }

    private void UpdateState(AnimatorLayerState state, float deltaTime)
    {
        if (state.CurrentState?.Clip == null)
        {
            return;
        }

        var speed = state.CurrentState.GetSpeed(_parameters);
        state.StateTime += deltaTime * speed;

        var duration = state.CurrentState.Clip.Duration;
        if (duration > 0)
        {
            if (state.CurrentState.Loop)
            {
                while (state.StateTime >= duration)
                {
                    state.StateTime -= duration;
                }

                while (state.StateTime < 0)
                {
                    state.StateTime += duration;
                }
            }
            else
            {
                state.StateTime = Math.Clamp(state.StateTime, 0, duration);
            }
            state.NormalizedTime = state.StateTime / duration;
        }
    }

    private void CheckTransitions(AnimatorLayerState state)
    {
        if (state.CurrentState == null)
        {
            return;
        }

        foreach (var transition in state.CurrentState.Transitions)
        {
            if (transition.CanTransition(_parameters, state.NormalizedTime))
            {
                StartTransition(state, transition);
                break;
            }
        }
    }

    private void CheckAnyStateTransitions(AnimatorLayer layer, AnimatorLayerState state)
    {
        if (layer.AnyState == null)
        {
            return;
        }

        foreach (var transition in layer.AnyState.Transitions)
        {
            if (transition.DestinationState == state.CurrentState)
            {
                continue;
            }

            if (transition.CanTransition(_parameters, state.NormalizedTime))
            {
                StartTransition(state, transition);
                break;
            }
        }
    }

    private void StartTransition(AnimatorLayerState state, AnimatorTransition transition)
    {
        state.PreviousState = state.CurrentState;
        state.PreviousStateTime = state.StateTime;
        state.CurrentState = transition.DestinationState;
        state.StateTime = transition.Offset * (state.CurrentState?.Clip?.Duration ?? 0);
        state.NormalizedTime = transition.Offset;
        state.TransitionDuration = transition.Duration;
        state.TransitionTime = 0;
        state.IsTransitioning = true;

        ResetTriggersInConditions(transition);
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

    private void UpdateTransition(AnimatorLayerState state, float deltaTime)
    {
        state.TransitionTime += deltaTime;

        if (state.PreviousState?.Clip != null)
        {
            var speed = state.PreviousState.GetSpeed(_parameters);
            state.PreviousStateTime += deltaTime * speed;
        }

        UpdateState(state, deltaTime);

        if (state.TransitionTime >= state.TransitionDuration)
        {
            state.IsTransitioning = false;
            state.PreviousState = null;
        }
    }

    private void SampleLayer(int layerIndex)
    {
        var state = _layerStates[layerIndex];
        if (Skeleton == null)
        {
            return;
        }

        if (state.IsTransitioning && state.TransitionDuration > 0)
        {
            var blendWeight = Math.Clamp(state.TransitionTime / state.TransitionDuration, 0, 1);
            SampleBlended(state, blendWeight);
        }
        else
        {
            SampleSingle(state.CurrentState, state.NormalizedTime);
        }
    }

    private void SampleSingle(AnimatorState? animState, float normalizedTime)
    {
        if (animState?.Clip == null || Skeleton == null || _ozzTransforms == null)
        {
            return;
        }

        var samplingDesc = new SamplingJobDesc
        {
            Context = animState.Clip.OzzContext,
            Ratio = normalizedTime,
            OutTransforms = _ozzTransforms.Value
        };

        if (Skeleton.OzzSkeleton.RunSamplingJob(in samplingDesc))
        {
            var span = _ozzTransforms.Value.AsSpan();
            for (var i = 0; i < BoneCount; i++)
            {
                _sampledTransforms[i] = span[i];
            }
        }
    }

    private void SampleBlended(AnimatorLayerState state, float blendWeight)
    {
        if (Skeleton == null || _ozzTransforms == null)
        {
            return;
        }

        SampleSingle(state.PreviousState, state.PreviousStateTime / (state.PreviousState?.Clip?.Duration ?? 1));
        for (var i = 0; i < BoneCount; i++)
        {
            _blendTransforms[i] = _sampledTransforms[i];
        }

        SampleSingle(state.CurrentState, state.NormalizedTime);

        for (var i = 0; i < BoneCount; i++)
        {
            _sampledTransforms[i] = Matrix4x4.Lerp(_blendTransforms[i], _sampledTransforms[i], blendWeight);
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

            _boneMatrices[i] = inverseBindMatrix * _sampledTransforms[i] * _nodeTransform;
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
    }

    public void CrossFade(string stateName, float duration, int layerIndex = 0)
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
            Duration = duration
        };
        StartTransition(layerState, transition);
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

    public void Dispose()
    {
        _ozzTransforms = null;
    }

    private class AnimatorLayerState
    {
        public AnimatorState? CurrentState;
        public AnimatorState? PreviousState;
        public float StateTime;
        public float PreviousStateTime;
        public float NormalizedTime;
        public bool IsTransitioning;
        public float TransitionTime;
        public float TransitionDuration;
    }
}
