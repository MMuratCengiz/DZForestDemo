namespace DZForestDemo;

public static class Shaders
{
    public const string GeometryVertexShader = @"
struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = float4(input.Position, 1.0);
    output.Normal = input.Normal;
    output.TexCoord = input.TexCoord;
    return output;
}
";

    public const string GeometryPixelShader = @"
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
";

    public const string Geometry3DVertexShader = @"
struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
};

cbuffer FrameConstants : register(b0, space0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float Time;
};

cbuffer DrawConstants : register(b0, space2)
{
    float4x4 Model;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    float4 worldPos = mul(float4(input.Position, 1.0), Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = mul(input.Normal, (float3x3)Model);
    output.TexCoord = input.TexCoord;
    return output;
}
";

    public const string Geometry3DPixelShader = @"
struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
};

#define LIGHT_TYPE_DIRECTIONAL 0
#define LIGHT_TYPE_POINT 1
#define LIGHT_TYPE_SPOT 2
#define MAX_LIGHTS 8

struct Light
{
    float3 PositionOrDirection;
    uint Type;
    float3 Color;
    float Intensity;
    float Radius;
    float InnerConeAngle;
    float OuterConeAngle;
    float _Padding;
};

cbuffer FrameConstants : register(b0, space0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float Time;
};

cbuffer LightConstants : register(b0, space1)
{
    Light Lights[MAX_LIGHTS];
    float3 AmbientSkyColor;
    uint NumLights;
    float3 AmbientGroundColor;
    float AmbientIntensity;
};

cbuffer MaterialConstants : register(b0, space3)
{
    float4 BaseColor;
    float Metallic;
    float Roughness;
    float AmbientOcclusion;
    float _MatPadding;
};

float3 CalculateDirectionalLight(Light light, float3 normal, float3 viewDir, float3 albedo)
{
    float3 lightDir = normalize(-light.PositionOrDirection);
    float3 halfDir = normalize(lightDir + viewDir);

    float ndotl = saturate(dot(normal, lightDir));
    float ndoth = saturate(dot(normal, halfDir));

    float specPower = lerp(8.0, 128.0, 1.0 - Roughness);
    float spec = pow(ndoth, specPower) * (1.0 - Roughness) * 0.5;

    float3 diffuse = albedo * ndotl;
    float3 specular = lerp(float3(0.04, 0.04, 0.04), albedo, Metallic) * spec;

    return (diffuse + specular) * light.Color * light.Intensity;
}

float3 CalculatePointLight(Light light, float3 worldPos, float3 normal, float3 viewDir, float3 albedo)
{
    float3 lightVec = light.PositionOrDirection - worldPos;
    float distance = length(lightVec);

    if (distance > light.Radius)
        return float3(0, 0, 0);

    float3 lightDir = lightVec / distance;
    float3 halfDir = normalize(lightDir + viewDir);

    float attenuation = saturate(1.0 - distance / light.Radius);
    attenuation *= attenuation;

    float ndotl = saturate(dot(normal, lightDir));
    float ndoth = saturate(dot(normal, halfDir));

    float specPower = lerp(8.0, 128.0, 1.0 - Roughness);
    float spec = pow(ndoth, specPower) * (1.0 - Roughness) * 0.5;

    float3 diffuse = albedo * ndotl;
    float3 specular = lerp(float3(0.04, 0.04, 0.04), albedo, Metallic) * spec;

    return (diffuse + specular) * light.Color * light.Intensity * attenuation;
}

float3 CalculateSpotLight(Light light, float3 worldPos, float3 normal, float3 viewDir, float3 albedo)
{
    float3 lightVec = light.PositionOrDirection - worldPos;
    float distance = length(lightVec);

    if (distance > light.Radius)
        return float3(0, 0, 0);

    float3 lightDir = lightVec / distance;

    float3 spotDir = float3(0, -1, 0);
    float theta = dot(lightDir, -spotDir);
    float epsilon = light.InnerConeAngle - light.OuterConeAngle;
    float spotIntensity = saturate((theta - light.OuterConeAngle) / max(epsilon, 0.001));

    float3 halfDir = normalize(lightDir + viewDir);

    float attenuation = saturate(1.0 - distance / light.Radius);
    attenuation *= attenuation;

    float ndotl = saturate(dot(normal, lightDir));
    float ndoth = saturate(dot(normal, halfDir));

    float specPower = lerp(8.0, 128.0, 1.0 - Roughness);
    float spec = pow(ndoth, specPower) * (1.0 - Roughness) * 0.5;

    float3 diffuse = albedo * ndotl;
    float3 specular = lerp(float3(0.04, 0.04, 0.04), albedo, Metallic) * spec;

    return (diffuse + specular) * light.Color * light.Intensity * attenuation * spotIntensity;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float3 normal = normalize(input.WorldNormal);
    float3 viewDir = normalize(CameraPosition - input.WorldPos);
    float3 albedo = BaseColor.rgb;

    float3 ambient = lerp(AmbientGroundColor, AmbientSkyColor, normal.y * 0.5 + 0.5);
    ambient *= AmbientIntensity * AmbientOcclusion;

    float3 totalLight = ambient * albedo;

    for (uint i = 0; i < NumLights && i < MAX_LIGHTS; i++)
    {
        Light light = Lights[i];

        if (light.Type == LIGHT_TYPE_DIRECTIONAL)
        {
            totalLight += CalculateDirectionalLight(light, normal, viewDir, albedo);
        }
        else if (light.Type == LIGHT_TYPE_POINT)
        {
            totalLight += CalculatePointLight(light, input.WorldPos, normal, viewDir, albedo);
        }
        else if (light.Type == LIGHT_TYPE_SPOT)
        {
            totalLight += CalculateSpotLight(light, input.WorldPos, normal, viewDir, albedo);
        }
    }

    float ndotv = saturate(dot(normal, viewDir));
    float fresnel = pow(1.0 - ndotv, 4.0) * Metallic * 0.3;
    totalLight += fresnel * albedo;

    totalLight = totalLight / (totalLight + 1.0);
    totalLight = pow(totalLight, 1.0 / 2.2);

    return float4(totalLight, BaseColor.a);
}
";

    public const string VertexShaderSource = @"
struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = float4(input.Position, 1.0);
    output.Color = input.Color;
    return output;
}
";

    public const string PixelShaderSource = @"
struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}
";

    public const string FullscreenVertexShader = @"
struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;
    output.TexCoord = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(output.TexCoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
    return output;
}
";

    public const string CompositePixelShader = @"
Texture2D<float4> SceneTexture : register(t0);
Texture2D<float4> UITexture : register(t1);
Texture2D<float4> DebugTexture : register(t2);
SamplerState LinearSampler : register(s0);

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 scene = SceneTexture.Sample(LinearSampler, input.TexCoord);
    float4 ui = UITexture.Sample(LinearSampler, input.TexCoord);
    float4 debug = DebugTexture.Sample(LinearSampler, input.TexCoord);
    float4 result = lerp(scene, ui, ui.a);
    return lerp(result, debug, debug.a);
}
";
}