// Skinned mesh vertex shader
// Applies skeletal animation transforms to vertices

#include "common/vertex_input_model.hlsl"

#define MAX_BONES 128

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    nointerpolation uint InstanceID : TEXCOORD2;
};

struct InstanceData
{
    float4x4 Model;
    float4 BaseColor;
    float Metallic;
    float Roughness;
    float AmbientOcclusion;
    float Padding;
};

cbuffer FrameConstants : register(b0, space0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float Time;
};

cbuffer BoneMatrices : register(b0, space5)
{
    float4x4 Bones[MAX_BONES];
};

StructuredBuffer<InstanceData> Instances : register(t0, space2);

float4x4 ComputeSkinMatrix(float4 weights, uint4 indices)
{
    float4x4 skinMatrix =
        Bones[indices.x] * weights.x +
        Bones[indices.y] * weights.y +
        Bones[indices.z] * weights.z +
        Bones[indices.w] * weights.w;
    return skinMatrix;
}

PSInput VSMain(VSInput input, uint instanceID : SV_InstanceID)
{
    PSInput output;

    InstanceData inst = Instances[instanceID];

    float totalWeight = input.BoneWeights.x + input.BoneWeights.y +
                        input.BoneWeights.z + input.BoneWeights.w;

    float3 skinnedPos;
    float3 skinnedNormal;

    if (totalWeight > 0.0)
    {
        float4x4 skinMatrix = ComputeSkinMatrix(input.BoneWeights, input.BoneIndices);

        skinnedPos = mul(float4(input.Position, 1.0), skinMatrix).xyz;
        skinnedNormal = mul(input.Normal, (float3x3)skinMatrix);
    }
    else
    {
        skinnedPos = input.Position;
        skinnedNormal = input.Normal;
    }

    float4 worldPos = mul(float4(skinnedPos, 1.0), inst.Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = mul(skinnedNormal, (float3x3)inst.Model);
    output.TexCoord = input.TexCoord;
    output.InstanceID = instanceID;

    return output;
}
