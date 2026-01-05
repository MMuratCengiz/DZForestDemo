// Common constant buffer definitions
// Register space layout:
// - Space 1: Camera/View data (GpuCameraLayout)
// - Space 2: Material data (GpuMaterialLayout)
// - Space 3: Draw/Instance data (GpuDrawLayout)

#ifndef CONSTANTS_HLSL
#define CONSTANTS_HLSL

#include "lighting.hlsl"

// Camera constants - Space 1, binding 0
cbuffer FrameConstants : register(b0, space1)
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

// Light constants - Space 1, binding 1
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

// Shadow atlas texture - Space 1, binding 2
Texture2D ShadowAtlas : register(t2, space1);
SamplerComparisonState ShadowSampler : register(s3, space1);

// Material textures - Space 2
Texture2D AlbedoTexture : register(t0, space2);
Texture2D NormalTexture : register(t1, space2);
Texture2D RoughnessTexture : register(t2, space2);
Texture2D MetallicTexture : register(t3, space2);
SamplerState TextureSampler : register(s0, space2);

// Material constants - Space 2, binding 4
cbuffer MaterialConstants : register(b4, space2)
{
    float4 MaterialBaseColor;
    float MaterialMetallic;
    float MaterialRoughness;
    float MaterialAO;
    float _MatPadding;
};

#endif // CONSTANTS_HLSL
