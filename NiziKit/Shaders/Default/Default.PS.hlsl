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

    float3 normal = NormalTexture.Sample(TextureSampler, uv).xyz;
    if (length(normal) == 0.0)
    {
        // normal = input.WorldNormal;
    }

    float roughness = RoughnessTexture.Sample(TextureSampler, uv).r;
    float metallic = MetallicTexture.Sample(TextureSampler, uv).r;

    float3 color = AmbientGroundColor * albedo.rgb;
    for (int i = 0; i < NumLights; i++)
    {
        Light light = Lights[i];
        if (light.Type == LIGHT_TYPE_DIRECTIONAL)
        {
            // Normal to Light
            float nl = dot(normal, light.SpotDirection);
            color += light.Color * albedo.rgb * nl;
        }
    }

    return float4(color, albedo.a);
}
