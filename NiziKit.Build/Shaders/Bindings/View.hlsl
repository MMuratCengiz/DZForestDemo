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