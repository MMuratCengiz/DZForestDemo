// Composite pixel shader - blends scene, UI, and debug layers

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
