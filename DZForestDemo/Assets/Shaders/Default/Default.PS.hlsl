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
    float3 T = normalize(input.WorldTangent);
    float3 B = normalize(input.WorldBitangent);
    float3x3 TBN = float3x3(T, B, N);

    float3 normalSample = NormalTexture.Sample(TextureSampler, uv).xyz;
    float normalLen = dot(normalSample, normalSample);
    if (normalLen > 0.01 && normalLen < 2.9)
    {
        float3 tangentNormal = normalSample * 2.0 - 1.0;
        N = normalize(mul(tangentNormal, TBN));
    }

    float roughness = RoughnessTexture.Sample(TextureSampler, uv).r;
    roughness = roughness > 0.001 ? roughness * RoughnessValue : RoughnessValue;
    roughness = clamp(roughness, 0.04, 1.0);

    float metallic = MetallicTexture.Sample(TextureSampler, uv).r;
    metallic = metallic > 0.001 ? metallic * MetallicValue : MetallicValue;
    metallic = saturate(metallic);

    float3 V = normalize(CameraPosition - input.WorldPos);

    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo.rgb, metallic);
    float3 diffuseColor = albedo.rgb * (1.0 - metallic);

    float3 Lo = float3(0, 0, 0);

    for (uint i = 0; i < NumLights; i++)
    {
        Light light = Lights[i];

        if (light.Type == LIGHT_TYPE_AMBIENT)
        {
            float hemisphereBlend = N.y * 0.5 + 0.5;
            float3 ambientSky = light.Color;
            float3 ambientGround = light.PositionOrDirection;
            Lo += lerp(ambientGround, ambientSky, hemisphereBlend) * light.Intensity * diffuseColor;
            continue;
        }

        float3 L;
        float attenuation = 1.0;

        if (light.Type == LIGHT_TYPE_DIRECTIONAL)
        {
            L = normalize(-light.PositionOrDirection);
        }
        else
        {
            float3 toLight = light.PositionOrDirection - input.WorldPos;
            float dist = length(toLight);
            L = toLight / max(dist, 0.0001);

            float rangeAtt = saturate(1.0 - (dist * dist) / (light.Radius * light.Radius));
            attenuation = rangeAtt * rangeAtt;

            if (light.Type == LIGHT_TYPE_SPOT)
            {
                float cosAngle = dot(-L, normalize(light.SpotDirection));
                float cosOuter = cos(light.OuterConeAngle);
                float cosInner = cos(light.InnerConeAngle);
                float spotAtt = saturate((cosAngle - cosOuter) / max(cosInner - cosOuter, 0.0001));
                attenuation *= spotAtt * spotAtt;
            }
        }

        float NdotL = saturate(dot(N, L));
        float3 H = normalize(V + L);
        float NdotH = saturate(dot(N, H));
        float VdotH = saturate(dot(V, H));

        float3 F = F0 + (1.0 - F0) * pow(1.0 - VdotH, 5.0);

        float a = roughness * roughness;
        float a2 = a * a;
        float denom = NdotH * NdotH * (a2 - 1.0) + 1.0;
        float D = a2 / (3.14159265 * denom * denom + 0.0001);

        float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
        float NdotV = saturate(dot(N, V));
        float G1V = NdotV / (NdotV * (1.0 - k) + k);
        float G1L = NdotL / (NdotL * (1.0 - k) + k);
        float G = G1V * G1L;

        float3 specular = (D * F * G) / (4.0 * NdotV * NdotL + 0.001);
        float3 kD = (1.0 - F) * (1.0 - metallic);
        float3 radiance = light.Color * light.Intensity * attenuation;

        float shadow = SampleShadow(light.ShadowIndex, input.WorldPos, N);

        Lo += (kD * diffuseColor / 3.14159265 + specular) * radiance * NdotL * shadow;
    }

    float3 emissive = EmissiveColor * EmissiveIntensity;
    float3 color = Lo + emissive;
    color = pow(max(color, 0.0), 1.0 / 2.2);

    return float4(color, albedo.a);
}
