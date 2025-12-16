// Common constant buffer definitions

#ifndef CONSTANTS_HLSL
#define CONSTANTS_HLSL

#include "lighting.hlsl"

cbuffer FrameConstants : register(b0, space0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float Time;
};

cbuffer LightConstants : register(b0, space1)
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

cbuffer DrawConstants : register(b0, space2)
{
    float4x4 Model;
};

cbuffer MaterialConstants : register(b0, space3)
{
    float4 BaseColor;
    float Metallic;
    float Roughness;
    float AmbientOcclusion;
    float _MatPadding;
};

#endif // CONSTANTS_HLSL
