// Example PBR shader demonstrating lygia integration
// To use lygia, clone it to Assets/Shaders/lygia:
// git clone https://github.com/patriciogonzalezvivo/lygia Assets/Shaders/lygia
//
// Then uncomment the includes below to use lygia's PBR functions.

// #include "lygia/lighting/pbr.hlsl"
// #include "lygia/color/tonemap.hlsl"

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
    float4 Tangent : TANGENT;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 WorldPosition : TEXCOORD0;
    float3 WorldNormal : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
    float3x3 TBN : TEXCOORD3;
};

cbuffer FrameConstants : register(b0, space0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float Time;
};

cbuffer DrawConstants : register(b0, space1)
{
    float4x4 Model;
};

cbuffer MaterialConstants : register(b0, space2)
{
    float4 BaseColor;
    float Metallic;
    float Roughness;
    float2 _Padding;
};

Texture2D<float4> AlbedoTexture : register(t0);
Texture2D<float4> NormalTexture : register(t1);
Texture2D<float4> MetallicRoughnessTexture : register(t2);
SamplerState LinearSampler : register(s0);

PSInput VSMain(VSInput input)
{
    PSInput output;

    float4 worldPos = mul(float4(input.Position, 1.0), Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPosition = worldPos.xyz;

    float3 N = normalize(mul(input.Normal, (float3x3)Model));
    float3 T = normalize(mul(input.Tangent.xyz, (float3x3)Model));
    float3 B = cross(N, T) * input.Tangent.w;

    output.WorldNormal = N;
    output.TexCoord = input.TexCoord;
    output.TBN = float3x3(T, B, N);

    return output;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    // Sample textures
    float4 albedo = AlbedoTexture.Sample(LinearSampler, input.TexCoord) * BaseColor;
    float3 normalMap = NormalTexture.Sample(LinearSampler, input.TexCoord).xyz * 2.0 - 1.0;
    float4 mr = MetallicRoughnessTexture.Sample(LinearSampler, input.TexCoord);

    // Transform normal from tangent space to world space
    float3 N = normalize(mul(normalMap, input.TBN));
    float3 V = normalize(CameraPosition - input.WorldPosition);

    // Material properties
    float metallic = mr.b * Metallic;
    float roughness = mr.g * Roughness;

    // Simple directional light for demonstration
    // Replace this with lygia's PBR functions for proper lighting
    float3 L = normalize(float3(1, 1, 0));
    float3 H = normalize(V + L);

    float NdotL = max(dot(N, L), 0.0);
    float NdotH = max(dot(N, H), 0.0);

    // Basic lighting
    float3 diffuse = albedo.rgb * NdotL;
    float3 specular = pow(NdotH, (1.0 - roughness) * 128.0) * lerp(float3(0.04, 0.04, 0.04), albedo.rgb, metallic);

    float3 color = diffuse + specular + albedo.rgb * 0.1; // Add ambient

    // Simple tonemap (replace with lygia/color/tonemap.hlsl for better results)
    color = color / (color + 1.0);
    color = pow(color, 1.0 / 2.2);

    return float4(color, albedo.a);
}
