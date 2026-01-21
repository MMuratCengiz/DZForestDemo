#include "Gizmo.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float Depth : TEXCOORD0;
};

Texture2D<float> SceneDepth : register(t0, space4);

float4 PSMain(PSInput input) : SV_TARGET
{
    float sceneDepth = SceneDepth.Load(int3(input.Position.xy, 0));
    float fragDepth = input.Depth;

    float occluded = step(sceneDepth, fragDepth);
    float alpha = lerp(1.0, 0.25, occluded) * Opacity;

    float pulse = 0.9 + 0.1 * sin(Time * 4.0);

    return float4(input.Color.rgb * pulse, input.Color.a * alpha);
}
