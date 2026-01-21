#include "Gizmo.hlsl"

struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float Depth : TEXCOORD0;
};

VSOutput VSMain(VSInput input)
{
    VSOutput output;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    output.Position.z -= DepthBias;
    output.Color = input.Color;
    output.Depth = output.Position.z / output.Position.w;
    return output;
}
