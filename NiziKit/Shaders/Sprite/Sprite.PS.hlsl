#include "../Bindings/Material.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord * UVScale + UVOffset;
    float4 texColor = HasAlbedoTexture > 0.5
        ? AlbedoTexture.Sample(TextureSampler, uv)
        : float4(1, 1, 1, 1);
    float4 result = texColor * AlbedoColor;
    clip(result.a - 0.001);
    return result;
}
