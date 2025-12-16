// Simple geometry vertex shader - no transforms

#include "common/vertex_input.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = float4(input.Position, 1.0);
    output.Normal = input.Normal;
    output.TexCoord = input.TexCoord;
    return output;
}
