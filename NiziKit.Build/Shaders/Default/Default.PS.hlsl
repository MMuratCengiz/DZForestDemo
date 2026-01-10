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
};

float4 PSMain(PSInput input) : SV_TARGET
{
    InstanceData inst = Instances[input.InstanceID];

    float4 albedo = inst.UseAlbedoTexture ?
        AlbedoTexture.Sample(TextureSampler, input.TexCoord) * inst.BaseColor :
        inst.BaseColor;

    float3 N = normalize(input.WorldNormal);
    float3 V = normalize(CameraPosition - input.WorldPos);
    float3 L = normalize(float3(0.5, 1.0, 0.3));

    float NdotL = saturate(dot(N, L));
    float3 diffuse = albedo.rgb * NdotL;
    float3 ambient = albedo.rgb * 0.2;

    return float4(ambient + diffuse, albedo.a);
}
