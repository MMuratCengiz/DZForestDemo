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

    float3 N = normalize(input.WorldNormal);
    if (HasNormalTexture > 0.5)
    {
        float3 T = normalize(input.WorldTangent);
        float3 B = normalize(input.WorldBitangent);
        float3x3 TBN = float3x3(T, B, N);
        float3 tangentNormal = NormalTexture.Sample(TextureSampler, uv).xyz * 2.0 - 1.0;
        N = normalize(mul(tangentNormal, TBN));
    }
    float3 normal = N;

    float roughness = HasRoughnessTexture > 0.5
                        ? RoughnessTexture.Sample(TextureSampler, uv).r * RoughnessValue
                        : RoughnessValue;

    float metallic = HasMetallicTexture > 0.5
                        ? MetallicTexture.Sample(TextureSampler, uv).r * MetallicValue
                        : MetallicValue;

    float3 emissive = HasEmissiveTexture > 0.5
                        ? EmissiveTexture.Sample(TextureSampler, uv).rgb * EmissiveColor * EmissiveIntensity
                        : EmissiveColor * EmissiveIntensity;

    float3 color = AmbientGroundColor * albedo.rgb;
    for (int i = 0; i < (int)NumLights; i++)
    {
        Light light = Lights[i];
        if (light.Type == LIGHT_TYPE_DIRECTIONAL)
        {
            float3 L     = normalize(-light.PositionOrDirection);
            float  NdotL = saturate(dot(normal, L));
            float  shadow = SampleShadow(light.ShadowIndex, input.WorldPos, normal, L, input.Position.xy);
            color += light.Color * light.Intensity * NdotL * shadow * albedo.rgb;
        }
        else if (light.Type == LIGHT_TYPE_POINT)
        {
            float3 toLight = light.PositionOrDirection - input.WorldPos;
            float  dist    = length(toLight);
            float3 L       = toLight / dist;
            float  atten   = saturate(1.0 - dist / light.Radius);
            atten *= atten;
            float  NdotL   = saturate(dot(normal, L));
            color += light.Color * light.Intensity * NdotL * atten * albedo.rgb;
        }
    }

    color += emissive;
    return float4(color, albedo.a);
}
