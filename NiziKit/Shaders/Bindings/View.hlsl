#ifndef VIEW_HLSL
#define VIEW_HLSL

#define LIGHT_TYPE_DIRECTIONAL 0
#define LIGHT_TYPE_POINT 1
#define LIGHT_TYPE_SPOT 2
#define MAX_LIGHTS 8

#define MAX_SHADOW_CASCADES 4
#define NUM_CASCADES        MAX_SHADOW_CASCADES

struct Light
{
    float3 PositionOrDirection;
    uint   Type;
    float3 Color;
    float  Intensity;
    float3 SpotDirection;
    float  Radius;
    float  InnerConeAngle;
    float  OuterConeAngle;
    int    ShadowIndex; // Index of the first cascade in Shadows[]. -1 = no shadow.
    float  _Pad0;
};

// Match GpuShadowData.cs
struct ShadowData
{
    float4x4 LightViewProjection;
    float    SplitDistance; // Linear view-space depth at this cascade's far edge.
    float    Bias;
    float    NormalBias;
    float    _Pad;
};

cbuffer ViewConstants : register(b0, space1)
{
    float4x4 View;
    float4x4 Projection;
    float4x4 ViewProjection;
    float4x4 InverseViewProjection;
    float3   CameraPosition;
    float    Time;
    float3   CameraForward;
    float    DeltaTime;
    float2   ScreenSize;
    float    NearPlane;
    float    FarPlane;
};

cbuffer LightConstants : register(b1, space1)
{
    Light      Lights[MAX_LIGHTS];
    ShadowData Shadows[MAX_SHADOW_CASCADES];
    float3     AmbientSkyColor;
    uint       NumLights;
    float3     AmbientGroundColor;
    float      AmbientIntensity;
    uint       NumShadows; // Total cascade count that are active.
    uint       _Pad0;
    uint       _Pad1;
    uint       _Pad2;
};

#include "Shadow.hlsl"

#endif
