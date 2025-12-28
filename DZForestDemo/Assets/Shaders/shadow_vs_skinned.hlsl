// Shadow map vertex shader for skinned meshes
// Applies skeletal animation transforms before shadow projection

#include "common/vertex_input.hlsl"

#define MAX_BONES 128

struct PSInput
{
    float4 Position : SV_POSITION;
};

struct ShadowInstanceData
{
    float4x4 Model;
};

cbuffer LightMatrixConstants : register(b0, space0)
{
    float4x4 LightViewProjection;
};

cbuffer BoneMatrices : register(b1, space0)
{
    float4x4 Bones[MAX_BONES];
};

StructuredBuffer<ShadowInstanceData> Instances : register(t0, space1);

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
    ShadowInstanceData inst = Instances[instanceID];

    float totalWeight = input.BoneWeights.x + input.BoneWeights.y +
                        input.BoneWeights.z + input.BoneWeights.w;

    float3 skinnedPos;
    if (totalWeight > 0.0)
    {
        float4x4 skinMatrix = ComputeSkinMatrix(input.BoneWeights, input.BoneIndices);
        skinnedPos = mul(float4(input.Position, 1.0), skinMatrix).xyz;
    }
    else
    {
        skinnedPos = input.Position;
    }

    float4 worldPos = mul(float4(skinnedPos, 1.0), inst.Model);
    output.Position = mul(worldPos, LightViewProjection);
    return output;
}
