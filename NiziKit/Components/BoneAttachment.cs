using System.Numerics;
using Microsoft.Extensions.Logging;
using NiziKit.Animation;
using NiziKit.Core;

namespace NiziKit.Components;

public partial class BoneAttachment : NiziComponent
{
    private static readonly ILogger Logger = Log.Get<BoneAttachment>();

    [SceneObjectSelector]
    [JsonProperty("targetName")]
    public partial string? TargetName { get; set; }

    [BoneSelector("TargetName")]
    [JsonProperty("boneName")]
    public partial string? BoneName { get; set; }

    [JsonProperty("positionOffset")]
    public partial Vector3 PositionOffset { get; set; }

    [JsonProperty("rotationOffset")]
    public partial Vector3 RotationOffset { get; set; }

    [JsonProperty("scaleOffset")]
    public partial Vector3 ScaleOffset { get; set; }

    public BoneAttachment()
    {
        ScaleOffset = Vector3.One;
    }

    private Animator? _animator;
    private GameObject? _target;
    private int _boneIndex = -1;

    public override void Begin()
    {
        ResolveTarget();
    }

    private void ResolveTarget()
    {
        _animator = null;
        _boneIndex = -1;
        _target = null;

        if (string.IsNullOrEmpty(TargetName) || string.IsNullOrEmpty(BoneName))
        {
            return;
        }

        _target = FindObjectByName(TargetName);
        if (_target == null)
        {
            return;
        }

        _animator = _target.GetComponent<Animator>();
        if (_animator != null)
        {
            _boneIndex = _animator.GetJointIndex(BoneName);
        }
    }

    private static GameObject? FindObjectByName(string name)
    {
        var scene = World.CurrentScene;
        foreach (var root in scene.RootObjects)
        {
            if (root.Name == name)
            {
                return root;
            }

            var found = root.FindChild(name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    public override void PostUpdate()
    {
        if (_animator == null || _boneIndex < 0 || _target == null)
        {
            ResolveTarget();
            return;
        }

        if (!_animator.IsInitialized || _boneIndex >= _animator.BoneCount)
        {
            return;
        }

        var boneModelMatrix = _animator.ModelSpaceTransforms[_boneIndex];
        var boneWorldMatrix = boneModelMatrix * _target.WorldMatrix;

        Matrix4x4.Decompose(boneWorldMatrix, out _, out var boneRotation, out var bonePosition);

        var offsetRotation = Quaternion.CreateFromYawPitchRoll(
            RotationOffset.Y * (MathF.PI / 180f),
            RotationOffset.X * (MathF.PI / 180f),
            RotationOffset.Z * (MathF.PI / 180f));

        Position = bonePosition + Vector3.Transform(PositionOffset, boneRotation);
        Rotation = boneRotation * offsetRotation;
        LocalScale = ScaleOffset;
    }
}
