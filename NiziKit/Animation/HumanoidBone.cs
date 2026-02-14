namespace NiziKit.Animation;

/// <summary>
/// Standard humanoid bone identifiers following the VRM specification.
/// Used for animation retargeting between different skeleton topologies.
/// </summary>
public enum HumanoidBone
{
    // Torso
    Hips,
    Spine,
    Chest,
    UpperChest,
    Neck,

    // Head
    Head,
    LeftEye,
    RightEye,
    Jaw,

    // Left Leg
    LeftUpperLeg,
    LeftLowerLeg,
    LeftFoot,
    LeftToes,

    // Right Leg
    RightUpperLeg,
    RightLowerLeg,
    RightFoot,
    RightToes,

    // Left Arm
    LeftShoulder,
    LeftUpperArm,
    LeftLowerArm,
    LeftHand,

    // Right Arm
    RightShoulder,
    RightUpperArm,
    RightLowerArm,
    RightHand,

    // Left Fingers
    LeftThumbProximal,
    LeftThumbIntermediate,
    LeftThumbDistal,
    LeftIndexProximal,
    LeftIndexIntermediate,
    LeftIndexDistal,
    LeftMiddleProximal,
    LeftMiddleIntermediate,
    LeftMiddleDistal,
    LeftRingProximal,
    LeftRingIntermediate,
    LeftRingDistal,
    LeftLittleProximal,
    LeftLittleIntermediate,
    LeftLittleDistal,

    // Right Fingers
    RightThumbProximal,
    RightThumbIntermediate,
    RightThumbDistal,
    RightIndexProximal,
    RightIndexIntermediate,
    RightIndexDistal,
    RightMiddleProximal,
    RightMiddleIntermediate,
    RightMiddleDistal,
    RightRingProximal,
    RightRingIntermediate,
    RightRingDistal,
    RightLittleProximal,
    RightLittleIntermediate,
    RightLittleDistal,

    Count
}
