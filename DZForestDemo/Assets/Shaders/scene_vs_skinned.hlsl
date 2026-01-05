#include "common/vertex_input.hlsl"
#include "common/constants.hlsl"

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
    uint UseAlbedoTexture;
    uint BoneOffset;
    uint _Pad0;
    uint _Pad1;
    uint _Pad2;
};

StructuredBuffer<InstanceData> Instances : register(t0, space3);
StructuredBuffer<float4x4> BoneMatrices : register(t1, space3);

float4x4 ComputeSkinMatrix(float4 weights, uint4 indices, uint boneOffset)
{
    float4x4 skinMatrix =
        BoneMatrices[boneOffset + indices.x] * weights.x +
        BoneMatrices[boneOffset + indices.y] * weights.y +
        BoneMatrices[boneOffset + indices.z] * weights.z +
        BoneMatrices[boneOffset + indices.w] * weights.w;
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

    if (totalWeight > 0.001f)
    {
        float4x4 skinMatrix = ComputeSkinMatrix(input.BoneWeights, input.BoneIndices, inst.BoneOffset);
        skinnedPos = mul(float4(input.Position, 1.0f), skinMatrix).xyz;
        skinnedNormal = mul(input.Normal, (float3x3)skinMatrix);
    }
    else
    {
        skinnedPos = input.Position;
        skinnedNormal = input.Normal;
    }

    float4 worldPos = mul(float4(skinnedPos, 1.0f), inst.Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = mul(skinnedNormal, (float3x3)inst.Model);
    output.TexCoord = input.TexCoord;
    output.InstanceID = instanceID;

    return output;
}
