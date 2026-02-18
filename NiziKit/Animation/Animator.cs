using System.Numerics;
using DenOfIz;
using Microsoft.Extensions.Logging;
using NiziKit.Assets;
using NiziKit.Assets.Pack;
using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Animation;

public enum LoopMode
{
    Once,
    Loop,
    PingPong
}

public enum BlendMode
{
    Override,
    Additive
}

public partial class Animator : NiziComponent, IDisposable
{
    private static readonly ILogger Logger = Log.Get<Animator>();
    private const int MaxBones = 256;
    private const int MaxLayers = 8;

    [AssetRef(AssetRefType.Skeleton, "skeleton")]
    public partial Skeleton? Skeleton { get; set; }

    [AssetRef(AssetRefType.Skeleton, "retargetSource")]
    public partial Skeleton? RetargetSource { get; set; }

    [AnimationSelector("Skeleton")]
    [JsonProperty("defaultAnimation")]
    public partial string? DefaultAnimation { get; set; }

    [SerializeField]
    [JsonProperty("animations")]
    public List<AnimationEntry> Animations { get; set; } = [];

    [HideInInspector] public bool IsPlaying { get; private set; }
    [HideInInspector] public bool IsPaused { get; private set; }
    [HideInInspector] public string? CurrentAnimation => _layers[0].CurrentAnimation?.Name;
    [HideInInspector] public LoopMode CurrentLoopMode => _layers[0].LoopMode;
    [HideInInspector] public float Time => _layers[0].Time;
    [HideInInspector] public float NormalizedTime => _layers[0].NormalizedTime;
    [HideInInspector] public float Duration => _layers[0].CurrentAnimation?.Duration ?? 0f;
    [HideInInspector] public float Speed { get; set; } = 1f;
    [HideInInspector] public int LoopCount => _layers[0].LoopCount;

    [HideInInspector] public bool IsBlending => _layers[0].IsBlending;
    [HideInInspector] public float BlendProgress => _layers[0].BlendProgress;

    [HideInInspector] public int BoneCount { get; private set; }
    [HideInInspector] public ReadOnlySpan<Matrix4x4> BoneMatrices => _boneMatrices.AsSpan(0, BoneCount);
    [HideInInspector] public ReadOnlySpan<Matrix4x4> ModelSpaceTransforms => _layerTransforms.AsSpan(0, BoneCount);
    [HideInInspector] public bool IsInitialized => _initialized;

    [HideInInspector]
    public IReadOnlyList<string> AnimationNames
    {
        get
        {
            if (Animations.Count > 0)
            {
                var names = new string[Animations.Count];
                for (var i = 0; i < Animations.Count; i++)
                {
                    names[i] = Animations[i].Name;
                }

                return names;
            }

            if (RetargetSource != null)
            {
                return RetargetSource.AnimationNames;
            }

            return Skeleton?.AnimationNames ?? Array.Empty<string>();
        }
    }

    [HideInInspector] public IReadOnlyList<string> JointNames => _jointNames;

    [HideInInspector] public int LayerCount => _layerCount;
    [HideInInspector] public bool IsRetargeting => _retargeter?.IsValid == true;

    private readonly Layer[] _layers = new Layer[MaxLayers];
    private int _layerCount = 1;
    private readonly Matrix4x4[] _boneMatrices = new Matrix4x4[MaxBones];
    private readonly Matrix4x4[] _layerTransforms = new Matrix4x4[MaxBones];
    private Matrix4x4[]? _inverseBindMatrices;
    private Matrix4x4 _nodeTransform = Matrix4x4.Identity;
    private int[]? _jointRemapTable;
    private Float4x4Array.Pinned? _ozzTransforms;
    private Float4x4Array.Pinned? _ozzBlendTransforms;
    private OzzJointTransformArray.Pinned? _ozzLocalTransforms;
    private OzzJointTransformArray.Pinned? _ozzBlendLocalTransforms;
    private AnimationRetargeter? _retargeter;
    private bool _initialized;
    private string[] _jointNames = [];

    public Animator()
    {
        for (var i = 0; i < MaxLayers; i++)
        {
            _layers[i] = new Layer();
        }

        for (var i = 0; i < MaxBones; i++)
        {
            _boneMatrices[i] = Matrix4x4.Identity;
            _layerTransforms[i] = Matrix4x4.Identity;
        }
    }

    private List<AnimationEntry>? _externalAnimationsBuffer;

    public void SyncAnimationsFromSkeleton()
    {
        if (Skeleton == null)
        {
            return;
        }

        _externalAnimationsBuffer ??= new List<AnimationEntry>(8);
        _externalAnimationsBuffer.Clear();

        for (var i = 0; i < Animations.Count; i++)
        {
            if (Animations[i].IsExternal)
            {
                _externalAnimationsBuffer.Add(Animations[i]);
            }
        }

        Animations.Clear();

        var animSource = RetargetSource ?? Skeleton;
        foreach (var animName in animSource.AnimationNames)
        {
            Animations.Add(AnimationEntry.FromSkeleton(animName));
        }

        for (var i = 0; i < _externalAnimationsBuffer.Count; i++)
        {
            Animations.Add(_externalAnimationsBuffer[i]);
        }
    }

    public void AddExternalAnimation(string name, string sourceRef)
    {
        for (var i = 0; i < Animations.Count; i++)
        {
            if (Animations[i].Name == name)
            {
                Animations[i].SourceRef = sourceRef;
                return;
            }
        }
        Animations.Add(AnimationEntry.External(name, sourceRef));
    }

    public bool RemoveAnimation(string name)
    {
        for (var i = 0; i < Animations.Count; i++)
        {
            if (Animations[i].Name == name)
            {
                Animations.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public override void Initialize()
    {
        if (Skeleton == null)
        {
            return;
        }

        if (Animations.Count == 0)
        {
            SyncAnimationsFromSkeleton();
        }

        BoneCount = Math.Min(Skeleton.JointCount, MaxBones);
        _ozzTransforms = Float4x4Array.Create(new Matrix4x4[BoneCount]);
        _ozzBlendTransforms = Float4x4Array.Create(new Matrix4x4[BoneCount]);
        _ozzLocalTransforms = OzzJointTransformArray.Create(new OzzJointTransform[BoneCount]);
        _ozzBlendLocalTransforms = OzzJointTransformArray.Create(new OzzJointTransform[BoneCount]);
        _initialized = true;

        _jointNames = new string[Skeleton.Joints.Count];
        for (var i = 0; i < Skeleton.Joints.Count; i++)
        {
            _jointNames[i] = Skeleton.Joints[i].Name;
        }

        var meshComponent = Owner?.GetComponent<MeshComponent>();
        _inverseBindMatrices = meshComponent?.Mesh?.InverseBindMatrices;
        _nodeTransform = meshComponent?.Mesh?.NodeTransform ?? Matrix4x4.Identity;

        BuildJointRemapTable(meshComponent?.Mesh?.JointNames);
        InitializeRetargeting();
        LoadExternalAnimations();

        if (!string.IsNullOrEmpty(DefaultAnimation) && HasAnimation(DefaultAnimation))
        {
            Play(DefaultAnimation);
        }
    }

    private void InitializeRetargeting()
    {
        _retargeter?.Dispose();
        _retargeter = null;

        if (RetargetSource == null || Skeleton == null)
        {
            return;
        }

        var srcTPose = RetargetSource.ComputeRestPose();
        var dstTPose = Skeleton.ComputeRestPose();

        _retargeter = new AnimationRetargeter();
        _retargeter.Setup(RetargetSource, Skeleton, srcTPose, dstTPose);

        if (!_retargeter.IsValid)
        {
            Logger.LogWarning("Retargeting setup failed between '{Src}' and '{Dst}'",
                RetargetSource.Name, Skeleton.Name);
            _retargeter.Dispose();
            _retargeter = null;
        }
    }

    private void LoadExternalAnimations()
    {
        if (Skeleton == null)
        {
            return;
        }

        var targetSkeleton = IsRetargeting ? RetargetSource! : Skeleton;

        for (var i = 0; i < Animations.Count; i++)
        {
            var entry = Animations[i];
            if (!entry.IsExternal)
            {
                continue;
            }

            try
            {
                var animData = AssetPacks.GetAnimationDataByPath(entry.SourceRef!);
                if (animData != null)
                {
                    targetSkeleton.LoadAnimation(animData, entry.Name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to load external animation '{Name}' from '{Source}': {Message}",
                    entry.Name, entry.SourceRef, ex.Message);
            }
        }
    }

    public int GetJointIndex(string name)
    {
        for (var i = 0; i < _jointNames.Length; i++)
        {
            if (_jointNames[i] == name)
            {
                return i;
            }
        }

        return -1;
    }

    public bool HasAnimation(string name)
    {
        for (var i = 0; i < Animations.Count; i++)
        {
            if (Animations[i].Name == name)
            {
                return true;
            }
        }

        if (RetargetSource != null)
        {
            var srcNames = RetargetSource.AnimationNames;
            for (var i = 0; i < srcNames.Count; i++)
            {
                if (srcNames[i] == name)
                {
                    return true;
                }
            }
        }

        var names = Skeleton?.AnimationNames ?? Array.Empty<string>();
        for (var i = 0; i < names.Count; i++)
        {
            if (names[i] == name)
            {
                return true;
            }
        }

        return false;
    }

    public AnimationEntry? GetAnimationEntry(string name)
    {
        for (var i = 0; i < Animations.Count; i++)
        {
            if (Animations[i].Name == name)
            {
                return Animations[i];
            }
        }

        return null;
    }

    private Assets.Animation? ResolveAnimation(string name)
    {
        if (Skeleton == null)
        {
            return null;
        }

        var entry = GetAnimationEntry(name);

        var animSkeleton = IsRetargeting ? RetargetSource! : Skeleton;

        if (entry == null)
        {
            try
            {
                return animSkeleton.GetAnimation(name);
            }
            catch
            {
                if (IsRetargeting)
                {
                    try { return Skeleton.GetAnimation(name); }
                    catch { return null; }
                }

                return null;
            }
        }

        if (entry.IsExternal)
        {
            try
            {
                var animData = AssetPacks.GetAnimationDataByPath(entry.SourceRef!);
                if (animData != null)
                {
                    return animSkeleton.LoadAnimation(animData, entry.Name);
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        try
        {
            return animSkeleton.GetAnimation(name);
        }
        catch
        {
            return null;
        }
    }

    public float GetDuration(string name)
    {
        if (Skeleton == null || !HasAnimation(name))
        {
            return 0f;
        }

        var anim = ResolveAnimation(name);
        return anim?.Duration ?? 0f;
    }

    public void Play(string name, LoopMode loop = LoopMode.Loop)
    {
        PlayOnLayer(0, name, loop);
    }

    public void CrossFade(string name, float duration, LoopMode loop = LoopMode.Loop)
    {
        CrossFadeOnLayer(0, name, duration, loop);
    }

    public void Stop()
    {
        IsPlaying = false;
        IsPaused = false;
        for (var i = 0; i < _layerCount; i++)
        {
            _layers[i].Stop();
        }
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Resume()
    {
        IsPaused = false;
    }

    public int AddLayer(float weight = 1f, BlendMode mode = BlendMode.Override)
    {
        if (_layerCount >= MaxLayers)
        {
            Logger.LogWarning("Maximum layer count ({MaxLayers}) reached", MaxLayers);
            return _layerCount - 1;
        }

        var index = _layerCount;
        _layers[index].Weight = weight;
        _layers[index].Mode = mode;
        _layerCount++;
        return index;
    }

    public void SetLayerWeight(int layer, float weight)
    {
        if (layer >= 0 && layer < _layerCount)
        {
            _layers[layer].Weight = Math.Clamp(weight, 0f, 1f);
        }
    }

    public void SetLayerBlendMode(int layer, BlendMode mode)
    {
        if (layer >= 0 && layer < _layerCount)
        {
            _layers[layer].Mode = mode;
        }
    }

    public void PlayOnLayer(int layer, string name, LoopMode loop = LoopMode.Loop)
    {
        if (Skeleton == null || layer < 0 || layer >= _layerCount)
        {
            return;
        }

        if (!_initialized)
        {
            Initialize();
        }

        var anim = ResolveAnimation(name);
        if (anim == null)
        {
            Logger.LogWarning("Failed to play animation '{Name}': animation not found", name);
            return;
        }

        _layers[layer].Play(anim, loop);
        IsPlaying = true;
        IsPaused = false;
    }

    public void CrossFadeOnLayer(int layer, string name, float duration, LoopMode loop = LoopMode.Loop)
    {
        if (Skeleton == null || layer < 0 || layer >= _layerCount)
        {
            return;
        }

        if (!_initialized)
        {
            Initialize();
        }

        var anim = ResolveAnimation(name);
        if (anim == null)
        {
            Logger.LogWarning("Failed to crossfade to animation '{Name}': animation not found", name);
            return;
        }

        _layers[layer].CrossFade(anim, duration, loop);
        IsPlaying = true;
        IsPaused = false;
    }

    public void Update(float deltaTime)
    {
        if (!_initialized || Skeleton == null || !IsPlaying || IsPaused)
        {
            return;
        }

        if (Skeleton.JointCount != BoneCount)
        {
            Stop();
            _initialized = false;
            Initialize();
            return;
        }

        var effectiveDelta = deltaTime * Speed;
        var anyPlaying = false;

        for (var i = 0; i < _layerCount; i++)
        {
            _layers[i].Update(effectiveDelta);

            if (!_layers[i].IsBlending)
            {
                _layers[i].DestroyBlendSamplingContext();
            }

            if (_layers[i].IsActive)
            {
                anyPlaying = true;
            }

            SampleLayer(i);
        }

        IsPlaying = anyPlaying;
        ComputeFinalBoneMatrices();
    }

    private void SampleLayer(int layerIndex)
    {
        var layer = _layers[layerIndex];

        if (!layer.IsActive || Skeleton == null || !_initialized)
        {
            return;
        }

        if (_retargeter is { IsValid: true })
        {
            SampleLayerRetargeted(layer);
        }
        else
        {
            SampleLayerDirect(layer);
        }

        ApplyLayerBlending(layerIndex);
    }

    private void SampleLayerDirect(Layer layer)
    {
        if (layer is { IsBlending: true, BlendDuration: > 0 })
        {
            SampleBlendedLocal(layer);
        }
        else if (layer.CurrentAnimation != null)
        {
            SampleLocal(layer.SamplingContext, layer.NormalizedTime, _ozzLocalTransforms!.Value);
        }

        if (layer is { IsBlending: true, BlendDuration: > 0 })
        {
            BlendAndConvertToModel(layer.BlendProgress);
        }
        else
        {
            ConvertLocalToModel(_ozzLocalTransforms!.Value, _ozzTransforms!.Value);
        }
    }

    private void SampleLayerRetargeted(Layer layer)
    {
        var destMatrices = _ozzTransforms!.Value.AsSpan().Slice(0, BoneCount);

        if (layer is { IsBlending: true, BlendDuration: > 0 } &&
            layer.PreviousAnimation != null && layer.CurrentAnimation != null)
        {
            _retargeter!.SampleBlendedAndRetarget(
                layer.BlendSamplingContext, layer.PreviousNormalizedTime,
                layer.SamplingContext, layer.NormalizedTime,
                layer.BlendProgress,
                destMatrices, Skeleton!.Joints);
        }
        else if (layer.CurrentAnimation != null)
        {
            _retargeter!.SampleAndRetarget(
                layer.SamplingContext, layer.NormalizedTime,
                destMatrices, Skeleton!.Joints);
        }
    }

    private void SampleLocal(OzzContext context, float normalizedTime, OzzJointTransformArray outLocalTransforms)
    {
        Skeleton?.OzzSkeleton.RunSamplingJobLocal(new SamplingJobLocalDesc
        {
            Context = context,
            Ratio = Math.Clamp(normalizedTime, 0f, 1f),
            OutTransforms = outLocalTransforms
        });
    }

    private void ConvertLocalToModel(OzzJointTransformArray localTransforms, Float4x4Array outModelTransforms)
    {
        Skeleton?.OzzSkeleton.RunLocalToModelFromTRS(new LocalToModelFromTRSDesc
        {
            LocalTransforms = localTransforms,
            OutTransforms = outModelTransforms
        });
    }

    private void SampleBlendedLocal(Layer layer)
    {
        if (Skeleton == null || !_initialized || layer.PreviousAnimation == null || layer.CurrentAnimation == null)
        {
            return;
        }

        SampleLocal(layer.BlendSamplingContext, layer.PreviousNormalizedTime, _ozzBlendLocalTransforms!.Value);
        SampleLocal(layer.SamplingContext, layer.NormalizedTime, _ozzLocalTransforms!.Value);
    }

    private void BlendAndConvertToModel(float blendWeight)
    {
        ConvertLocalToModel(_ozzBlendLocalTransforms!.Value, _ozzBlendTransforms!.Value);
        ConvertLocalToModel(_ozzLocalTransforms!.Value, _ozzTransforms!.Value);

        var prevSpan = _ozzBlendTransforms!.Value.AsSpan();
        var currSpan = _ozzTransforms!.Value.AsSpan();

        for (var i = 0; i < BoneCount; i++)
        {
            currSpan[i] = Matrix4x4.Lerp(prevSpan[i], currSpan[i], blendWeight);
        }
    }

    private void ApplyLayerBlending(int layerIndex)
    {
        if (!_initialized)
        {
            return;
        }

        var layer = _layers[layerIndex];
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
        if (weight <= 0 || !layer.IsActive)
        {
            return;
        }

        switch (layer.Mode)
        {
            case BlendMode.Override:
                for (var i = 0; i < BoneCount; i++)
                {
                    _layerTransforms[i] = Matrix4x4.Lerp(_layerTransforms[i], transforms[i], weight);
                }

                break;

            case BlendMode.Additive:
                for (var i = 0; i < BoneCount; i++)
                {
                    var additive = transforms[i] - Matrix4x4.Identity;
                    _layerTransforms[i] += additive * weight;
                }

                break;
        }
    }

    private void BuildJointRemapTable(string[]? meshJointNames)
    {
        if (Skeleton == null || meshJointNames == null || meshJointNames.Length == 0)
        {
            _jointRemapTable = null;
            return;
        }

        var skeletonJointLookup = new Dictionary<string, int>(Skeleton.Joints.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < Skeleton.Joints.Count; i++)
        {
            skeletonJointLookup[Skeleton.Joints[i].Name] = i;
        }

        var needsRemap = false;
        var remap = new int[meshJointNames.Length];
        for (var i = 0; i < meshJointNames.Length; i++)
        {
            if (skeletonJointLookup.TryGetValue(meshJointNames[i], out var skeletonIndex))
            {
                remap[i] = skeletonIndex;
                if (skeletonIndex != i)
                {
                    needsRemap = true;
                }
            }
            else
            {
                remap[i] = -1;
                needsRemap = true;
            }
        }

        _jointRemapTable = needsRemap ? remap : null;
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

            var skeletonIndex = _jointRemapTable != null && i < _jointRemapTable.Length
                ? _jointRemapTable[i]
                : i;

            if (skeletonIndex < 0)
            {
                _boneMatrices[i] = Matrix4x4.Identity;
                continue;
            }

            _boneMatrices[i] = inverseBindMatrix * _layerTransforms[skeletonIndex] * _nodeTransform;
        }
    }

    public void Dispose()
    {
        _initialized = false;
        for (var i = 0; i < MaxLayers; i++)
        {
            _layers[i].DestroyAllContexts();
        }

        _ozzTransforms?.Dispose();
        _ozzBlendTransforms?.Dispose();
        _ozzLocalTransforms?.Dispose();
        _ozzBlendLocalTransforms?.Dispose();
        _retargeter?.Dispose();
        _retargeter = null;
    }

    private class Layer
    {
        public Assets.Animation? CurrentAnimation;
        public Assets.Animation? PreviousAnimation;
        public LoopMode LoopMode;
        public float Time;
        public float NormalizedTime;
        public float PreviousTime;
        public float PreviousNormalizedTime;
        public int LoopCount;
        public int PlaybackDirection = 1;
        public float Weight = 1f;
        public BlendMode Mode = BlendMode.Override;

        public bool IsBlending;
        public float BlendTime;
        public float BlendDuration;
        public float BlendProgress => BlendDuration > 0 ? Math.Clamp(BlendTime / BlendDuration, 0f, 1f) : 1f;

        public bool IsActive => CurrentAnimation != null;

        public OzzContext SamplingContext;
        public OzzContext BlendSamplingContext;
        private Assets.Animation? _samplingContextOwner;
        private Assets.Animation? _blendSamplingContextOwner;

        public void EnsureSamplingContext(Assets.Animation anim)
        {
            if (anim == _samplingContextOwner)
            {
                return;
            }

            DestroySamplingContext();
            SamplingContext = anim.CreateSamplingContext();
            _samplingContextOwner = anim;
        }

        public void EnsureBlendSamplingContext(Assets.Animation anim)
        {
            if (anim == _blendSamplingContextOwner)
            {
                return;
            }

            DestroyBlendSamplingContext();
            BlendSamplingContext = anim.CreateSamplingContext();
            _blendSamplingContextOwner = anim;
        }

        public void DestroySamplingContext()
        {
            if ((ulong)SamplingContext != 0)
            {
                _samplingContextOwner?.DestroySamplingContext(SamplingContext);
                SamplingContext = default;
                _samplingContextOwner = null;
            }
        }

        public void DestroyBlendSamplingContext()
        {
            if ((ulong)BlendSamplingContext != 0)
            {
                _blendSamplingContextOwner?.DestroySamplingContext(BlendSamplingContext);
                BlendSamplingContext = default;
                _blendSamplingContextOwner = null;
            }
        }

        public void DestroyAllContexts()
        {
            DestroySamplingContext();
            DestroyBlendSamplingContext();
        }

        public void Play(Assets.Animation anim, LoopMode loop)
        {
            DestroyBlendSamplingContext();
            EnsureSamplingContext(anim);

            CurrentAnimation = anim;
            PreviousAnimation = null;
            LoopMode = loop;
            Time = 0;
            NormalizedTime = 0;
            LoopCount = 0;
            PlaybackDirection = 1;
            IsBlending = false;
            BlendTime = 0;
            BlendDuration = 0;
        }

        public void CrossFade(Assets.Animation anim, float duration, LoopMode loop)
        {
            if (CurrentAnimation == null)
            {
                Play(anim, loop);
                return;
            }

            // Move current context to blend context
            DestroyBlendSamplingContext();
            BlendSamplingContext = SamplingContext;
            _blendSamplingContextOwner = _samplingContextOwner;
            SamplingContext = default;
            _samplingContextOwner = null;

            // Create new context for the incoming animation
            EnsureSamplingContext(anim);

            PreviousAnimation = CurrentAnimation;
            PreviousTime = Time;
            PreviousNormalizedTime = NormalizedTime;

            CurrentAnimation = anim;
            LoopMode = loop;
            Time = 0;
            NormalizedTime = 0;
            LoopCount = 0;
            PlaybackDirection = 1;

            IsBlending = true;
            BlendTime = 0;
            BlendDuration = duration;
        }

        public void Stop()
        {
            DestroyAllContexts();
            CurrentAnimation = null;
            PreviousAnimation = null;
            Time = 0;
            NormalizedTime = 0;
            LoopCount = 0;
            IsBlending = false;
        }

        public void Update(float deltaTime)
        {
            if (CurrentAnimation == null)
            {
                return;
            }

            if (IsBlending)
            {
                BlendTime += deltaTime;
                if (BlendTime >= BlendDuration)
                {
                    IsBlending = false;
                    PreviousAnimation = null;
                }
                else if (PreviousAnimation != null)
                {
                    PreviousTime += deltaTime;
                    var prevDuration = PreviousAnimation.Duration;
                    if (prevDuration > 0)
                    {
                        PreviousNormalizedTime = PreviousTime / prevDuration;
                        if (PreviousNormalizedTime > 1f)
                        {
                            PreviousNormalizedTime %= 1f;
                            PreviousTime %= prevDuration;
                        }
                    }
                }
            }

            var duration = CurrentAnimation.Duration;
            if (duration <= 0)
            {
                return;
            }

            Time += deltaTime * PlaybackDirection;

            switch (LoopMode)
            {
                case LoopMode.Loop:
                    while (Time >= duration)
                    {
                        Time -= duration;
                        LoopCount++;
                    }

                    while (Time < 0)
                    {
                        Time += duration;
                        LoopCount++;
                    }

                    NormalizedTime = Time / duration;
                    break;

                case LoopMode.PingPong:
                    if (Time >= duration)
                    {
                        Time = duration - (Time - duration);
                        PlaybackDirection = -1;
                        if (Time < 0)
                        {
                            Time = 0;
                        }
                    }
                    else if (Time < 0)
                    {
                        Time = -Time;
                        PlaybackDirection = 1;
                        LoopCount++;
                        if (Time > duration)
                        {
                            Time = duration;
                        }
                    }

                    NormalizedTime = Time / duration;
                    break;

                case LoopMode.Once:
                default:
                    if (Time >= duration)
                    {
                        Time = duration;
                        NormalizedTime = 1f;
                    }
                    else if (Time < 0)
                    {
                        Time = 0;
                        NormalizedTime = 0f;
                    }
                    else
                    {
                        NormalizedTime = Time / duration;
                    }

                    break;
            }
        }
    }
}
