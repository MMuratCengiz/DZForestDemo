#include "../Bindings/View.hlsl"
#include "../Bindings/Material.hlsl"
#include "../Bindings/Draw.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    nointerpolation uint InstanceID : TEXCOORD2;
    float3 WorldTangent : TEXCOORD3;
    float3 WorldBitangent : TEXCOORD4;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;

    float4 albedo = HasAlbedoTexture > 0.5
        ? AlbedoTexture.Sample(TextureSampler, uv) * AlbedoColor
        : AlbedoColor;

    float3 normalSample = NormalTexture.Sample(TextureSampler, uv).xyz;
    float roughness = RoughnessTexture.Sample(TextureSampler, uv).r;
    float metallic = MetallicTexture.Sample(TextureSampler, uv).r;

    return albedo;
}
