namespace DZForestDemo;

public static class Shaders
{
    public const string VertexShaderSource = @"
struct VSInput
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

PSInput VSMain(VSInput input)
{
    PSInput output;
    output.Position = float4(input.Position, 1.0);
    output.Color = input.Color;
    return output;
}
";

    public const string PixelShaderSource = @"
struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}
";

    public const string FullscreenVertexShader = @"
struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;
    output.TexCoord = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(output.TexCoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
    return output;
}
";

    public const string CompositePixelShader = @"
Texture2D<float4> SceneTexture : register(t0);
Texture2D<float4> UITexture : register(t1);
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
    return lerp(scene, ui, ui.a);
}
";
}