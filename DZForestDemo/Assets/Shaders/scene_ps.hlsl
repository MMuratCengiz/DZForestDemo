#include "common/lighting.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    nointerpolation uint InstanceID : TEXCOORD2;
};

struct InstanceData
{
    float4x4 Model;
    float4 BaseColor;
    float Metallic;
    float Roughness;
    float AmbientOcclusion;
    uint UseAlbedoTexture;
};

cbuffer FrameConstants : register(b0, space0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float Time;
};

cbuffer LightConstants : register(b1, space0)
{
    Light Lights[MAX_LIGHTS];
    ShadowData Shadows[MAX_SHADOW_LIGHTS];
    float3 AmbientSkyColor;
    uint NumLights;
    float3 AmbientGroundColor;
    float AmbientIntensity;
    uint NumShadows;
    uint _Pad0;
    uint _Pad1;
    uint _Pad2;
};

// space1 (PerCamera): All textures (SRVs only)
Texture2D<float> ShadowAtlas : register(t0, space1);
Texture2D<float4> AlbedoTexture : register(t1, space1);

// space5 (Samplers): All samplers
SamplerComparisonState ShadowSampler : register(s0, space5);
SamplerState AlbedoSampler : register(s1, space5);

// space3 (PerDraw): Instance data
StructuredBuffer<InstanceData> Instances : register(t0, space3);

float SampleShadow(int shadowIndex, float3 worldPos, float3 normal)
{
    if (shadowIndex < 0 || shadowIndex >= (int)NumShadows)
    {
        return 1.0;
    }

    ShadowData data = Shadows[shadowIndex];

    float3 biasedPos = worldPos + normal * data.NormalBias;
    float4 lightSpace = mul(float4(biasedPos, 1.0), data.LightViewProjection);
    float3 shadowCoord = lightSpace.xyz / lightSpace.w;

    if (shadowCoord.z < 0.0 || shadowCoord.z > 1.0 ||
        shadowCoord.x < 0.0 || shadowCoord.x > 1.0 ||
        shadowCoord.y < 0.0 || shadowCoord.y > 1.0)
    {
        return 1.0;
    }

    float2 uv = shadowCoord.xy * data.AtlasScaleOffset.xy + data.AtlasScaleOffset.zw;
    float currentDepth = shadowCoord.z - data.Bias;

    return ShadowAtlas.SampleCmpLevelZero(ShadowSampler, uv, currentDepth);
}

float3 CalculateDirectionalLight(Light light, float3 worldPos, float3 normal, float3 viewDir, float3 albedo, float metallic, float roughness)
{
    float3 lightDir = normalize(-light.PositionOrDirection);
    float3 halfDir = normalize(lightDir + viewDir);

    float ndotl = saturate(dot(normal, lightDir));
    float ndoth = saturate(dot(normal, halfDir));

    float specPower = lerp(8.0, 128.0, 1.0 - roughness);
    float spec = pow(ndoth, specPower) * (1.0 - roughness) * 0.5;

    float3 diffuse = albedo * ndotl;
    float3 specular = lerp(float3(0.04, 0.04, 0.04), albedo, metallic) * spec;

    float shadow = SampleShadow(light.ShadowIndex, worldPos, normal);

    return (diffuse + specular) * light.Color * light.Intensity * shadow;
}

float3 CalculatePointLight(Light light, float3 worldPos, float3 normal, float3 viewDir, float3 albedo, float metallic, float roughness)
{
    float3 lightVec = light.PositionOrDirection - worldPos;
    float dist = length(lightVec);

    if (dist > light.Radius)
    {
        return float3(0, 0, 0);
    }

    float3 lightDir = lightVec / dist;
    float3 halfDir = normalize(lightDir + viewDir);

    float atten = saturate(1.0 - dist / light.Radius);
    atten *= atten;

    float ndotl = saturate(dot(normal, lightDir));
    float ndoth = saturate(dot(normal, halfDir));

    float specPower = lerp(8.0, 128.0, 1.0 - roughness);
    float spec = pow(ndoth, specPower) * (1.0 - roughness) * 0.5;

    float3 diffuse = albedo * ndotl;
    float3 specular = lerp(float3(0.04, 0.04, 0.04), albedo, metallic) * spec;

    float shadow = SampleShadow(light.ShadowIndex, worldPos, normal);

    return (diffuse + specular) * light.Color * light.Intensity * atten * shadow;
}

float3 CalculateSpotLight(Light light, float3 worldPos, float3 normal, float3 viewDir, float3 albedo, float metallic, float roughness)
{
    float3 lightVec = light.PositionOrDirection - worldPos;
    float dist = length(lightVec);

    if (dist > light.Radius)
    {
        return float3(0, 0, 0);
    }

    float3 lightDir = lightVec / dist;
    float3 spotDir = normalize(light.SpotDirection);
    float theta = dot(-lightDir, spotDir);
    float epsilon = light.InnerConeAngle - light.OuterConeAngle;
    float spotFactor = saturate((theta - light.OuterConeAngle) / max(epsilon, 0.001));

    float3 halfDir = normalize(lightDir + viewDir);

    float atten = saturate(1.0 - dist / light.Radius);
    atten *= atten;

    float ndotl = saturate(dot(normal, lightDir));
    float ndoth = saturate(dot(normal, halfDir));

    float specPower = lerp(8.0, 128.0, 1.0 - roughness);
    float spec = pow(ndoth, specPower) * (1.0 - roughness) * 0.5;

    float3 diffuse = albedo * ndotl;
    float3 specular = lerp(float3(0.04, 0.04, 0.04), albedo, metallic) * spec;

    float shadow = SampleShadow(light.ShadowIndex, worldPos, normal);

    return (diffuse + specular) * light.Color * light.Intensity * atten * spotFactor * shadow;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    // Load material properties from instance data
    InstanceData inst = Instances[input.InstanceID];

    // Sample albedo from texture or use base color
    float3 albedo;
    float alpha;
    if (inst.UseAlbedoTexture != 0)
    {
        float4 texColor = AlbedoTexture.Sample(AlbedoSampler, input.TexCoord);
        albedo = texColor.rgb * inst.BaseColor.rgb; // Multiply texture with tint color
        alpha = texColor.a * inst.BaseColor.a;
    }
    else
    {
        albedo = inst.BaseColor.rgb;
        alpha = inst.BaseColor.a;
    }

    float metallic = inst.Metallic;
    float roughness = inst.Roughness;
    float ao = inst.AmbientOcclusion;

    float3 normal = normalize(input.WorldNormal);
    float3 viewDir = normalize(CameraPosition - input.WorldPos);

    float3 ambient = lerp(AmbientGroundColor, AmbientSkyColor, normal.y * 0.5 + 0.5);
    ambient *= AmbientIntensity * ao;

    float3 totalLight = ambient * albedo;

    for (uint i = 0; i < NumLights && i < MAX_LIGHTS; i++)
    {
        Light light = Lights[i];

        if (light.Type == LIGHT_TYPE_DIRECTIONAL)
        {
            totalLight += CalculateDirectionalLight(light, input.WorldPos, normal, viewDir, albedo, metallic, roughness);
        }
        else if (light.Type == LIGHT_TYPE_POINT)
        {
            totalLight += CalculatePointLight(light, input.WorldPos, normal, viewDir, albedo, metallic, roughness);
        }
        else if (light.Type == LIGHT_TYPE_SPOT)
        {
            totalLight += CalculateSpotLight(light, input.WorldPos, normal, viewDir, albedo, metallic, roughness);
        }
    }

    float ndotv = saturate(dot(normal, viewDir));
    float fresnel = pow(1.0 - ndotv, 4.0) * metallic * 0.3;
    totalLight += fresnel * albedo;

    totalLight = totalLight / (totalLight + 1.0);
    totalLight = pow(totalLight, 1.0 / 2.2);

    return float4(totalLight, alpha);
}
