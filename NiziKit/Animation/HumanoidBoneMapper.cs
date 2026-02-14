using NiziKit.Assets;

namespace NiziKit.Animation;

/// <summary>
/// Auto-detects humanoid bone assignments from skeleton joint names.
/// Supports Mixamo, Synty, Unreal, generic, and VRM naming conventions.
/// </summary>
public static class HumanoidBoneMapper
{
    /// <summary>
    /// Maps skeleton joints to humanoid bones by matching joint names.
    /// Returns an array indexed by <see cref="HumanoidBone"/> with skeleton joint indices (-1 if unmapped).
    /// </summary>
    public static int[] MapSkeleton(IReadOnlyList<Joint> joints)
    {
        var mapping = new int[(int)HumanoidBone.Count];
        Array.Fill(mapping, -1);

        var nameLookup = new Dictionary<string, int>(joints.Count * 2, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < joints.Count; i++)
        {
            var name = joints[i].Name;
            nameLookup.TryAdd(name, i);

            var stripped = StripPrefix(name);
            if (stripped != name)
            {
                nameLookup.TryAdd(stripped, i);
            }
        }

        var claimed = new HashSet<int>();

        for (var bone = 0; bone < (int)HumanoidBone.Count; bone++)
        {
            var candidates = GetCandidateNames((HumanoidBone)bone);
            foreach (var candidate in candidates)
            {
                if (nameLookup.TryGetValue(candidate, out var jointIdx) && !claimed.Contains(jointIdx))
                {
                    mapping[bone] = jointIdx;
                    claimed.Add(jointIdx);
                    break;
                }
            }
        }

        return mapping;
    }

    /// <summary>
    /// Returns true if the mapping has at least the required bones for retargeting
    /// (hips, spine, head, arms, legs).
    /// </summary>
    public static bool IsValidHumanoid(int[] mapping)
    {
        return mapping[(int)HumanoidBone.Hips] >= 0
            && mapping[(int)HumanoidBone.Spine] >= 0
            && mapping[(int)HumanoidBone.Head] >= 0
            && mapping[(int)HumanoidBone.LeftUpperArm] >= 0
            && mapping[(int)HumanoidBone.LeftLowerArm] >= 0
            && mapping[(int)HumanoidBone.LeftHand] >= 0
            && mapping[(int)HumanoidBone.RightUpperArm] >= 0
            && mapping[(int)HumanoidBone.RightLowerArm] >= 0
            && mapping[(int)HumanoidBone.RightHand] >= 0
            && mapping[(int)HumanoidBone.LeftUpperLeg] >= 0
            && mapping[(int)HumanoidBone.LeftLowerLeg] >= 0
            && mapping[(int)HumanoidBone.LeftFoot] >= 0
            && mapping[(int)HumanoidBone.RightUpperLeg] >= 0
            && mapping[(int)HumanoidBone.RightLowerLeg] >= 0
            && mapping[(int)HumanoidBone.RightFoot] >= 0;
    }

    private static string StripPrefix(string name)
    {
        var colonIdx = name.LastIndexOf(':');
        if (colonIdx >= 0)
        {
            return name[(colonIdx + 1)..];
        }

        var pipeIdx = name.LastIndexOf('|');
        if (pipeIdx >= 0)
        {
            return name[(pipeIdx + 1)..];
        }

        return name;
    }

    /// <summary>
    /// Returns candidate joint names for a humanoid bone, tried in priority order.
    /// Order matters for Synty disambiguation: Clavicle_L=shoulder, Shoulder_L=upper arm,
    /// Elbow_L=forearm, Ankle_L=foot. Combined with claimed-joint tracking, putting clavicle
    /// names before shoulder names ensures correct mapping.
    /// </summary>
    private static string[] GetCandidateNames(HumanoidBone bone)
    {
        return bone switch
        {
            // Torso
            HumanoidBone.Hips => ["Hips", "Hip", "Pelvis", "hip", "pelvis"],
            HumanoidBone.Spine => ["Spine", "Spine1", "spine", "spine_01", "Spine_01"],
            HumanoidBone.Chest => ["Chest", "Spine2", "chest", "spine_02", "Spine_02"],
            HumanoidBone.UpperChest => ["UpperChest", "Spine3", "upper_chest", "spine_03", "Spine_03"],
            HumanoidBone.Neck => ["Neck", "neck", "neck_01"],

            // Head
            HumanoidBone.Head => ["Head", "head"],
            HumanoidBone.LeftEye => ["LeftEye", "Eye_L", "eye_l", "EyeLeft"],
            HumanoidBone.RightEye => ["RightEye", "Eye_R", "eye_r", "EyeRight"],
            HumanoidBone.Jaw => ["Jaw", "jaw"],

            // Left Leg
            HumanoidBone.LeftUpperLeg => ["LeftUpLeg", "LeftUpperLeg", "Left_UpperLeg", "UpperLeg_L",
                "Thigh_L", "thigh_l", "l_thigh", "Left leg", "L_Thigh"],
            HumanoidBone.LeftLowerLeg => ["LeftLeg", "LeftLowerLeg", "Left_LowerLeg", "LowerLeg_L",
                "calf_l", "l_calf", "Left knee", "L_Calf"],
            HumanoidBone.LeftFoot => ["LeftFoot", "Left_Foot", "Foot_L", "Ankle_L",
                "foot_l", "l_foot", "Left ankle", "L_Foot"],
            HumanoidBone.LeftToes => ["LeftToeBase", "LeftToes", "Left_Toes", "Toes_L", "Ball_L",
                "ball_l", "l_ball", "Left toe", "L_Toe"],

            // Right Leg
            HumanoidBone.RightUpperLeg => ["RightUpLeg", "RightUpperLeg", "Right_UpperLeg", "UpperLeg_R",
                "Thigh_R", "thigh_r", "r_thigh", "Right leg", "R_Thigh"],
            HumanoidBone.RightLowerLeg => ["RightLeg", "RightLowerLeg", "Right_LowerLeg", "LowerLeg_R",
                "calf_r", "r_calf", "Right knee", "R_Calf"],
            HumanoidBone.RightFoot => ["RightFoot", "Right_Foot", "Foot_R", "Ankle_R",
                "foot_r", "r_foot", "Right ankle", "R_Foot"],
            HumanoidBone.RightToes => ["RightToeBase", "RightToes", "Right_Toes", "Toes_R", "Ball_R",
                "ball_r", "r_ball", "Right toe", "R_Toe"],

            // Left Arm
            HumanoidBone.LeftShoulder => ["LeftShoulder", "Left_Shoulder",
                "Clavicle_L", "clavicle_l", "l_clavicle", "L_Clavicle", "Shoulder_L"],
            HumanoidBone.LeftUpperArm => ["LeftArm", "LeftUpperArm", "Left_UpperArm", "UpperArm_L",
                "Shoulder_L", "upperarm_l", "l_upperarm", "Left arm", "L_UpperArm"],
            HumanoidBone.LeftLowerArm => ["LeftForeArm", "LeftLowerArm", "Left_LowerArm", "LowerArm_L",
                "Elbow_L", "lowerarm_l", "l_lowerarm", "Left elbow", "L_LowerArm"],
            HumanoidBone.LeftHand => ["LeftHand", "Left_Hand", "Hand_L",
                "hand_l", "l_hand", "Left wrist", "L_Hand"],

            // Right Arm
            HumanoidBone.RightShoulder => ["RightShoulder", "Right_Shoulder",
                "Clavicle_R", "clavicle_r", "r_clavicle", "R_Clavicle", "Shoulder_R"],
            HumanoidBone.RightUpperArm => ["RightArm", "RightUpperArm", "Right_UpperArm", "UpperArm_R",
                "Shoulder_R", "upperarm_r", "r_upperarm", "Right arm", "R_UpperArm"],
            HumanoidBone.RightLowerArm => ["RightForeArm", "RightLowerArm", "Right_LowerArm", "LowerArm_R",
                "Elbow_R", "lowerarm_r", "r_lowerarm", "Right elbow", "R_LowerArm"],
            HumanoidBone.RightHand => ["RightHand", "Right_Hand", "Hand_R",
                "hand_r", "r_hand", "Right wrist", "R_Hand"],

            // Left Fingers
            HumanoidBone.LeftThumbProximal => ["LeftHandThumb1", "L_Thumb1", "thumb_01_l", "Thumb_01", "LeftThumbProximal"],
            HumanoidBone.LeftThumbIntermediate => ["LeftHandThumb2", "L_Thumb2", "thumb_02_l", "Thumb_02", "LeftThumbIntermediate"],
            HumanoidBone.LeftThumbDistal => ["LeftHandThumb3", "L_Thumb3", "thumb_03_l", "Thumb_03", "LeftThumbDistal"],
            HumanoidBone.LeftIndexProximal => ["LeftHandIndex1", "L_Index1", "index_01_l", "indexFinger_01_l", "IndexFinger_01", "LeftIndexProximal"],
            HumanoidBone.LeftIndexIntermediate => ["LeftHandIndex2", "L_Index2", "index_02_l", "indexFinger_02_l", "IndexFinger_02", "LeftIndexIntermediate"],
            HumanoidBone.LeftIndexDistal => ["LeftHandIndex3", "L_Index3", "index_03_l", "indexFinger_03_l", "IndexFinger_03", "LeftIndexDistal"],
            HumanoidBone.LeftMiddleProximal => ["LeftHandMiddle1", "L_Middle1", "middle_01_l", "Finger_01", "LeftMiddleProximal"],
            HumanoidBone.LeftMiddleIntermediate => ["LeftHandMiddle2", "L_Middle2", "middle_02_l", "Finger_02", "LeftMiddleIntermediate"],
            HumanoidBone.LeftMiddleDistal => ["LeftHandMiddle3", "L_Middle3", "middle_03_l", "Finger_03", "LeftMiddleDistal"],
            HumanoidBone.LeftRingProximal => ["LeftHandRing1", "L_Ring1", "ring_01_l", "LeftRingProximal"],
            HumanoidBone.LeftRingIntermediate => ["LeftHandRing2", "L_Ring2", "ring_02_l", "LeftRingIntermediate"],
            HumanoidBone.LeftRingDistal => ["LeftHandRing3", "L_Ring3", "ring_03_l", "LeftRingDistal"],
            HumanoidBone.LeftLittleProximal => ["LeftHandPinky1", "L_Little1", "pinky_01_l", "LeftLittleProximal"],
            HumanoidBone.LeftLittleIntermediate => ["LeftHandPinky2", "L_Little2", "pinky_02_l", "LeftLittleIntermediate"],
            HumanoidBone.LeftLittleDistal => ["LeftHandPinky3", "L_Little3", "pinky_03_l", "LeftLittleDistal"],

            // Right Fingers
            HumanoidBone.RightThumbProximal => ["RightHandThumb1", "R_Thumb1", "thumb_01_r", "Thumb_01_1", "RightThumbProximal"],
            HumanoidBone.RightThumbIntermediate => ["RightHandThumb2", "R_Thumb2", "thumb_02_r", "Thumb_02_1", "RightThumbIntermediate"],
            HumanoidBone.RightThumbDistal => ["RightHandThumb3", "R_Thumb3", "thumb_03_r", "Thumb_03_1", "RightThumbDistal"],
            HumanoidBone.RightIndexProximal => ["RightHandIndex1", "R_Index1", "index_01_r", "indexFinger_01_r", "IndexFinger_01_1", "RightIndexProximal"],
            HumanoidBone.RightIndexIntermediate => ["RightHandIndex2", "R_Index2", "index_02_r", "indexFinger_02_r", "IndexFinger_02_1", "RightIndexIntermediate"],
            HumanoidBone.RightIndexDistal => ["RightHandIndex3", "R_Index3", "index_03_r", "indexFinger_03_r", "IndexFinger_03_1", "RightIndexDistal"],
            HumanoidBone.RightMiddleProximal => ["RightHandMiddle1", "R_Middle1", "middle_01_r", "Finger_01_1", "RightMiddleProximal"],
            HumanoidBone.RightMiddleIntermediate => ["RightHandMiddle2", "R_Middle2", "middle_02_r", "Finger_02_1", "RightMiddleIntermediate"],
            HumanoidBone.RightMiddleDistal => ["RightHandMiddle3", "R_Middle3", "middle_03_r", "Finger_03_1", "RightMiddleDistal"],
            HumanoidBone.RightRingProximal => ["RightHandRing1", "R_Ring1", "ring_01_r", "RightRingProximal"],
            HumanoidBone.RightRingIntermediate => ["RightHandRing2", "R_Ring2", "ring_02_r", "RightRingIntermediate"],
            HumanoidBone.RightRingDistal => ["RightHandRing3", "R_Ring3", "ring_03_r", "RightRingDistal"],
            HumanoidBone.RightLittleProximal => ["RightHandPinky1", "R_Little1", "pinky_01_r", "RightLittleProximal"],
            HumanoidBone.RightLittleIntermediate => ["RightHandPinky2", "R_Little2", "pinky_02_r", "RightLittleIntermediate"],
            HumanoidBone.RightLittleDistal => ["RightHandPinky3", "R_Little3", "pinky_03_r", "RightLittleDistal"],

            _ => []
        };
    }
}
