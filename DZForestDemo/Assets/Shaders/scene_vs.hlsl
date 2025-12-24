// Scene vertex shader - transforms geometry to world/clip space with GPU instancing

#include "common/vertex_input.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    nointerpolation uint InstanceID : TEXCOORD2;
};

// Per-instance data stored in a structured buffer
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

PSInput VSMain(VSInput input, uint instanceID : SV_InstanceID)
{
    PSInput output;
    InstanceData inst = Instances[instanceID];
    float4 worldPos = mul(float4(input.Position, 1.0), inst.Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = mul(input.Normal, (float3x3)inst.Model);
    output.TexCoord = input.TexCoord;
    output.InstanceID = instanceID;
    return output;
}
