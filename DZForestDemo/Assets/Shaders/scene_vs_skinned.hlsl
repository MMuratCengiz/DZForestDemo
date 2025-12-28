// Skinned mesh vertex shader
// Applies skeletal animation transforms to vertices

#include "common/vertex_input.hlsl"

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

StructuredBuffer<InstanceData> Instances : register(t0, space3);

cbuffer BoneMatrices : register(b0, space3)
{
    float4x4 Bones[MAX_BONES];
};

float4x4 ComputeSkinMatrix(float4 weights, uint4 indices)
{
    // DEBUG: Return identity to test if skinning math works
    float4x4 identity = float4x4(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1
    );
    return identity;

    /*
    float4x4 skinMatrix =
        Bones[indices.x] * weights.x +
        Bones[indices.y] * weights.y +
        Bones[indices.z] * weights.z +
        Bones[indices.w] * weights.w;

    return skinMatrix;
    */
}

PSInput VSMain(VSInput input, uint instanceID : SV_InstanceID)
{
    PSInput output;

    InstanceData inst = Instances[instanceID];

    // DEBUG: Bypass ALL skinning - use input directly
    float3 skinnedPos = input.Position;
    float3 skinnedNormal = input.Normal;

    float4 worldPos = mul(float4(skinnedPos, 1.0f), inst.Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = mul(skinnedNormal, (float3x3)inst.Model);
    output.TexCoord = input.TexCoord;
    output.InstanceID = instanceID;

    return output;
}
