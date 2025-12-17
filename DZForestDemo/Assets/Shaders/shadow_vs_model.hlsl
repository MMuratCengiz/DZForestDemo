// Shadow map vertex shader for model meshes (Static/Skinned)
// Uses 80-byte vertex layout

#include "common/vertex_input_model.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
};

// Per-instance data (only need model matrix for shadows)
struct ShadowInstanceData
{
    float4x4 Model;
};

cbuffer LightMatrixConstants : register(b0, space0)
{
    float4x4 LightViewProjection;
};

StructuredBuffer<ShadowInstanceData> Instances : register(t0, space1);

PSInput VSMain(VSInput input, uint instanceID : SV_InstanceID)
{
    PSInput output;
    ShadowInstanceData inst = Instances[instanceID];
    float4 worldPos = mul(float4(input.Position, 1.0), inst.Model);
    output.Position = mul(worldPos, LightViewProjection);
    return output;
}
