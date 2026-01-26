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

[NiziComponent(TypeName = "animator")]
public partial class Animator : IDisposable
{
    private static readonly ILogger Logger = Log.Get<Animator>();
    private const int MaxBones = 256;
    private const int MaxLayers = 8;

    [AssetRef(AssetRefType.Skeleton, "skeleton")]
    public partial Skeleton? Skeleton { get; set; }

    [HideInInspector] public string? SkeletonRef { get; set; }

    [AnimationSelector("Skeleton")]
    [JsonProperty("defaultAnimation")]
    public partial string? DefaultAnimation { get; set; }

    [SerializeField]
    [JsonProperty("animations")]
    public List<AnimationEntry> Animations { get; set; } = [];

    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public string? CurrentAnimation => _layers[0].CurrentAnimation?.Name;
    public LoopMode CurrentLoopMode => _layers[0].LoopMode;
    public float Time => _layers[0].Time;
    public float NormalizedTime => _layers[0].NormalizedTime;
    public float Duration => _layers[0].CurrentAnimation?.Duration ?? 0f;
    public float Speed { get; set; } = 1f;
    public int LoopCount => _layers[0].LoopCount;

    public bool IsBlending => _layers[0].IsBlending;
    public float BlendProgress => _layers[0].BlendProgress;

    public int BoneCount { get; private set; }
    public ReadOnlySpan<Matrix4x4> BoneMatrices => _boneMatrices.AsSpan(0, BoneCount);
    public bool IsInitialized => _initialized;

    private string[]? _cachedAnimationNames;
    private int _cachedAnimationCount = -1;

    public IReadOnlyList<string> AnimationNames
    {
        get
        {
            if (Animations.Count > 0)
            {
                if (_cachedAnimationNames == null || _cachedAnimationCount != Animations.Count)
                {
                    _cachedAnimationNames = new string[Animations.Count];
                    for (var i = 0; i < Animations.Count; i++)
                    {
                        _cachedAnimationNames[i] = Animations[i].Name;
                    }
                    _cachedAnimationCount = Animations.Count;
                }
                return _cachedAnimationNames;
            }
            return Skeleton?.AnimationNames ?? Array.Empty<string>();
        }
    }

    public IReadOnlyList<string> JointNames => _jointNames;

    public int LayerCount => _layerCount;

    private readonly Layer[] _layers = new Layer[MaxLayers];
    private int _layerCount = 1;
    private readonly Matrix4x4[] _boneMatrices = new Matrix4x4[MaxBones];
    private readonly Matrix4x4[] _layerTransforms = new Matrix4x4[MaxBones];
    private Matrix4x4[]? _inverseBindMatrices;
    private Matrix4x4 _nodeTransform = Matrix4x4.Identity;
    private Float4x4Array.Pinned? _ozzTransforms;
    private Float4x4Array.Pinned? _ozzBlendTransforms;
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
        _cachedAnimationNames = null;
        _cachedAnimationCount = -1;

        foreach (var animName in Skeleton.AnimationNames)
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
        _cachedAnimationNames = null;
        _cachedAnimationCount = -1;
    }

    public bool RemoveAnimation(string name)
    {
        for (var i = 0; i < Animations.Count; i++)
        {
            if (Animations[i].Name == name)
            {
                Animations.RemoveAt(i);
                _cachedAnimationNames = null;
                _cachedAnimationCount = -1;
                return true;
            }
        }
        return false;
    }

    public void Initialize()
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
        _initialized = true;

        _jointNames = new string[Skeleton.Joints.Count];
        for (var i = 0; i < Skeleton.Joints.Count; i++)
        {
            _jointNames[i] = Skeleton.Joints[i].Name;
        }

        var meshComponent = Owner?.GetComponent<MeshComponent>();
        _inverseBindMatrices = meshComponent?.Mesh?.InverseBindMatrices;
        _nodeTransform = meshComponent?.Mesh?.NodeTransform ?? Matrix4x4.Identity;

        LoadExternalAnimations();

        if (!string.IsNullOrEmpty(DefaultAnimation) && HasAnimation(DefaultAnimation))
        {
            Play(DefaultAnimation);
        }
    }

    private void LoadExternalAnimations()
    {
        if (Skeleton == null)
        {
            return;
        }

        for (var i = 0; i < Animations.Count; i++)
        {
            var entry = Animations[i];
            if (!entry.IsExternal)
            {
                continue;
            }

            try
            {
                var (modelPath, animationName) = ParseAnimationSourceRef(entry.SourceRef!);
                if (!string.IsNullOrEmpty(modelPath) && !string.IsNullOrEmpty(animationName))
                {
                    Skeleton.LoadAnimationFromFile(modelPath, animationName);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Failed to load external animation '{Name}' from '{Source}': {Message}",
                    entry.Name, entry.SourceRef, ex.Message);
            }
        }
    }

    private static (string modelPath, string animationName) ParseAnimationSourceRef(string sourceRef)
    {
        if (string.IsNullOrEmpty(sourceRef))
        {
            return (string.Empty, string.Empty);
        }

        var extensions = new[] { ".glb", ".gltf", ".fbx", ".obj" };
        foreach (var ext in extensions)
        {
            var extIndex = sourceRef.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
            if (extIndex > 0)
            {
                var afterExt = extIndex + ext.Length;
                if (afterExt < sourceRef.Length && sourceRef[afterExt] == '/')
                {
                    return (sourceRef[..afterExt], sourceRef[(afterExt + 1)..]);
                }
                if (afterExt == sourceRef.Length)
                {
                    return (sourceRef, string.Empty);
                }
            }
        }

        return (sourceRef, string.Empty);
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

        if (entry == null)
        {
            try
            {
                return Skeleton.GetAnimation(name);
            }
            catch
            {
                return null;
            }
        }

        if (entry.IsExternal)
        {
            var (modelPath, animationName) = ParseAnimationSourceRef(entry.SourceRef!);
            if (!string.IsNullOrEmpty(modelPath))
            {
                try
                {
                    return Skeleton.LoadAnimationFromFile(modelPath, animationName);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        try
        {
            return Skeleton.GetAnimation(name);
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

        var effectiveDelta = deltaTime * Speed;
        var anyPlaying = false;

        for (var i = 0; i < _layerCount; i++)
        {
            _layers[i].Update(effectiveDelta);
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

        if (layer.IsBlending && layer.BlendDuration > 0)
        {
            SampleBlended(layer);
        }
        else if (layer.CurrentAnimation != null)
        {
            SampleSingle(layer.CurrentAnimation, layer.NormalizedTime, _ozzTransforms!);
        }

        ApplyLayerBlending(layerIndex);
    }

    private void SampleSingle(Assets.Animation anim, float normalizedTime, Float4x4Array outTransforms)
    {
        if (Skeleton == null)
        {
            return;
        }

        var samplingDesc = new SamplingJobDesc
        {
            Context = anim.OzzContext,
            Ratio = Math.Clamp(normalizedTime, 0f, 1f),
            OutTransforms = outTransforms
        };

        Skeleton.OzzSkeleton.RunSamplingJob(in samplingDesc);
    }

    private void SampleBlended(Layer layer)
    {
        if (Skeleton == null || !_initialized || layer.PreviousAnimation == null || layer.CurrentAnimation == null)
        {
            return;
        }

        SampleSingle(layer.PreviousAnimation, layer.PreviousNormalizedTime, _ozzBlendTransforms!);
        SampleSingle(layer.CurrentAnimation, layer.NormalizedTime, _ozzTransforms!);

        var prevSpan = _ozzBlendTransforms!.Value.AsSpan();
        var currSpan = _ozzTransforms!.Value.AsSpan();
        var weight = layer.BlendProgress;

        for (var i = 0; i < BoneCount; i++)
        {
            currSpan[i] = Matrix4x4.Lerp(prevSpan[i], currSpan[i], weight);
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

    public void Dispose()
    {
        _initialized = false;
        _ozzTransforms?.Dispose();
        _ozzBlendTransforms?.Dispose();
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

        public void Play(Assets.Animation anim, LoopMode loop)
        {
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
