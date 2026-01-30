#ifndef VIEW_HLSL
#define VIEW_HLSL

#define LIGHT_TYPE_DIRECTIONAL 0
#define LIGHT_TYPE_POINT 1
#define LIGHT_TYPE_SPOT 2
#define MAX_LIGHTS 8
#define MAX_SHADOW_LIGHTS 4

struct Light
{
    float3 PositionOrDirection;
    uint Type;
    float3 Color;
    float Intensity;
    float3 SpotDirection;
    float Radius;
    float InnerConeAngle;
    float OuterConeAngle;
    int ShadowIndex;
    float _Pad0;
};

struct ShadowData
{
    float4x4 LightViewProjection;
    float4 AtlasScaleOffset;
    float Bias;
    float NormalBias;
    float2 _Padding;
};

cbuffer ViewConstants : register(b0, space1)
{
    float4x4 View;
    float4x4 Projection;
    float4x4 ViewProjection;
    float4x4 InverseViewProjection;
    float3 CameraPosition;
    float Time;
    float3 CameraForward;
    float DeltaTime;
    float2 ScreenSize;
    float NearPlane;
    float FarPlane;
};

cbuffer LightConstants : register(b1, space1)
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

Texture2D<float> ShadowAtlas : register(t2, space1);
SamplerComparisonState ShadowSampler : register(s3, space1);

float SampleShadow(int shadowIndex, float3 worldPos, float3 normal)
{
    if (shadowIndex < 0 || (uint)shadowIndex >= NumShadows)
        return 1.0;

    ShadowData sd = Shadows[shadowIndex];

    float3 biasedPos = worldPos + normal * sd.NormalBias;
    float4 lightClip = mul(float4(biasedPos, 1.0), sd.LightViewProjection);
    float3 ndc = lightClip.xyz / lightClip.w;

    float2 uv = ndc.xy * 0.5 + 0.5;
    uv.y = 1.0 - uv.y;

    uv = uv * sd.AtlasScaleOffset.xy + sd.AtlasScaleOffset.zw;

    float depth = ndc.z - sd.Bias;

    float2 texelSize;
    ShadowAtlas.GetDimensions(texelSize.x, texelSize.y);
    texelSize = 1.0 / texelSize;

    float shadow = 0.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            shadow += ShadowAtlas.SampleCmpLevelZero(ShadowSampler, uv + float2(x, y) * texelSize, depth);
        }
    }
    return shadow / 9.0;
}

#endif
