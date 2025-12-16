// Simple geometry pixel shader - basic diffuse lighting

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float3 lightDir = normalize(float3(0.5, 1.0, 0.5));
    float ndotl = saturate(dot(input.Normal, lightDir));
    float3 color = float3(0.8, 0.6, 0.4) * (0.3 + 0.7 * ndotl);
    return float4(color, 1.0);
}
