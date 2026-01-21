#ifndef GIZMO_HLSL
#define GIZMO_HLSL

cbuffer GizmoConstants : register(b0, space4)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float DepthBias;
    float4 SelectionColor;
    float Opacity;
    float Time;
    float2 ScreenSize;
};

static const float4 AxisColorX = float4(1.0, 0.2, 0.2, 1.0);
static const float4 AxisColorY = float4(0.2, 1.0, 0.2, 1.0);
static const float4 AxisColorZ = float4(0.2, 0.4, 1.0, 1.0);

#endif
