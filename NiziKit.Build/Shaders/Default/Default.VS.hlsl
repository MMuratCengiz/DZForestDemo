#include "../Bindings/View.hlsl"
#include "../Bindings/Material.hlsl"
#include "../Bindings/Draw.hlsl"

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float3 WorldNormal : NORMAL;
    float3 WorldPos : TEXCOORD1;
    float2 TexCoord : TEXCOORD0;
    nointerpolation uint InstanceID : TEXCOORD2;
};

VSOutput VSMain(VSInput input, uint instanceID : SV_InstanceID)
{
    VSOutput output;
    InstanceData inst = Instances[instanceID];
    float4 worldPos = mul(float4(input.Position, 1.0), inst.Model);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.WorldNormal = normalize(mul(input.Normal, (float3x3)inst.Model));
    output.TexCoord = input.TexCoord;
    output.InstanceID = instanceID;
    return output;
}
