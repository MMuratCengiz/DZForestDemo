#include "NiziKit/Bindings/View.hlsl"
#include "NiziKit/Bindings/Material.hlsl"
#include "NiziKit/Bindings/Draw.hlsl"
#include "NiziKit/lygia/math/const.hlsl"
#include "NiziKit/lygia/generative/snoise.hlsl"
#include "NiziKit/lygia/color/hueShift.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    nointerpolation uint InstanceID : TEXCOORD2;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 baseColor = AlbedoTexture.Sample(TextureSampler, input.TexCoord);

    float3 N = normalize(input.WorldNormal);
    float3 L = normalize(float3(0.5, 1.0, 0.3));
    float NdotL = saturate(dot(N, L));

    float noise = snoise(input.WorldPos * 2.0 + Time * 3.0);
    float pulse = 0.5 + 0.5 * sin(Time * 4.0 + noise * 2.0);

    float3 hueShifted = hueShift(baseColor.rgb, Time * 0.5);
    float3 glowColor = lerp(baseColor.rgb, hueShifted, 0.7);

    float3 diffuse = glowColor * NdotL;
    float3 ambient = glowColor * 0.3;
    float3 glow = glowColor * pulse * 0.6;

    float fresnel = pow(1.0 - saturate(dot(N, normalize(CameraPosition - input.WorldPos))), 2.0);
    float3 rim = glowColor * fresnel * pulse * 0.8;

    return float4(ambient + diffuse + glow + rim, baseColor.a);
}
