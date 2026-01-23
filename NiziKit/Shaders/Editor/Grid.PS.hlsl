#include "Gizmo.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float Depth : TEXCOORD0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float dist = length(input.Position.xy - ScreenSize * 0.5);
    float fade = saturate(1.0 - dist / (ScreenSize.x * 0.6));
    return float4(input.Color.rgb, input.Color.a * Opacity * fade);
}
