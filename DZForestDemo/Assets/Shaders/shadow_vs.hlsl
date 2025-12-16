// Shadow map vertex shader - renders depth from light's perspective

#include "common/vertex_input.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
};

cbuffer LightMatrixConstants : register(b0, space0)
{
    float4x4 LightViewProjection;
};

cbuffer DrawConstants : register(b0, space1)
{
    float4x4 Model;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    float4 worldPos = mul(float4(input.Position, 1.0), Model);
    output.Position = mul(worldPos, LightViewProjection);
    return output;
}
