#include "NiziKit/Bindings/View.hlsl"
#include "NiziKit/Bindings/Material.hlsl"
#include "NiziKit/Bindings/Draw.hlsl"
#include "NiziKit/lygia/generative/snoise.hlsl"

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

    float wave = snoise(input.WorldPos * 0.5 + float3(0, 0, Time * 2.0));
    float pattern = 0.5 + 0.5 * snoise(input.WorldPos * 3.0 + wave);

    float3 highlight = float3(0.4, 1.0, 0.4);
    float3 shadow = float3(0.1, 0.4, 0.1);
    float3 snakeColor = lerp(shadow, highlight, pattern) * baseColor.rgb;

    float pulse = 0.9 + 0.1 * sin(Time * 2.0 + input.WorldPos.x + input.WorldPos.z);
    snakeColor *= pulse;

    float3 diffuse = snakeColor * NdotL;
    float3 ambient = snakeColor * 0.25;

    float3 V = normalize(CameraPosition - input.WorldPos);
    float3 H = normalize(L + V);
    float spec = pow(saturate(dot(N, H)), 32.0) * 0.3;

    return float4(ambient + diffuse + spec, baseColor.a);
}
