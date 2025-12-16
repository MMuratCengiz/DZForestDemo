// Scene vertex shader - transforms geometry to world/clip space

#include "common/vertex_input.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
};

cbuffer FrameConstants : register(b0, space0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float Time;
};

cbuffer DrawConstants : register(b0, space2)
{
    float4x4 Model;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    float4 worldPos = mul(float4(input.Position, 1.0), Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = mul(input.Normal, (float3x3)Model);
    output.TexCoord = input.TexCoord;
    return output;
}
