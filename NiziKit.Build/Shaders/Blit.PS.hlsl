Texture2D<float4> Tex1 : register(t0);
Texture2D<float4> Tex2 : register(t1);
SamplerState LinearSampler : register(s0);

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 tex1 = Tex1.Sample(LinearSampler, input.TexCoord);
    float4 tex2 = Tex2.Sample(LinearSampler, input.TexCoord);
    return lerp(tex1, tex2, tex2.a);
}
