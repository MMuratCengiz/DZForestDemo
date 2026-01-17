#include "../Bindings/View.hlsl"
#include "../Bindings/Material.hlsl"
#include "../Bindings/Draw.hlsl"

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
    float4 Tangent : TANGENT;
#if SKINNED
    float4 BoneWeights : BLENDWEIGHT;
    uint4 BoneIndices : BLENDINDICES;
#endif
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    nointerpolation uint InstanceID : TEXCOORD2;
};

VSOutput VSMain(VSInput input, uint instanceID : SV_InstanceID)
{
    VSOutput output;
    InstanceData inst = Instances[instanceID];

#if SKINNED
    float4x4 skinMatrix =
        BoneTransforms[inst.BoneOffset + input.BoneIndices.x] * input.BoneWeights.x +
        BoneTransforms[inst.BoneOffset + input.BoneIndices.y] * input.BoneWeights.y +
        BoneTransforms[inst.BoneOffset + input.BoneIndices.z] * input.BoneWeights.z +
        BoneTransforms[inst.BoneOffset + input.BoneIndices.w] * input.BoneWeights.w;

    float4 skinnedPos = mul(float4(input.Position, 1.0), skinMatrix);
    float3 skinnedNormal = mul(input.Normal, (float3x3)skinMatrix);

    float4 worldPos = mul(skinnedPos, inst.Model);
    output.WorldNormal = normalize(mul(skinnedNormal, (float3x3)inst.Model));
#else
    float4 worldPos = mul(float4(input.Position, 1.0), inst.Model);
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)inst.Model));
#endif

    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.TexCoord = input.TexCoord;
    output.InstanceID = instanceID;
    return output;
}
