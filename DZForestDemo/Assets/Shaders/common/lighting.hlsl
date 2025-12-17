// Lighting structures and constants

#ifndef LIGHTING_HLSL
#define LIGHTING_HLSL

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

#endif // LIGHTING_HLSL
