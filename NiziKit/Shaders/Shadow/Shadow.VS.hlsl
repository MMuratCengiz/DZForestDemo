// Shadow-caster vertex shader for Cascaded Shadow Maps.
//
// Technique: vertex amplification via instancing.
//   Draw call instance count = objectCount * NUM_CASCADES
//   cascadeIdx = instanceID % NUM_CASCADES  → routes triangle to the correct array layer
//   objectIdx  = instanceID / NUM_CASCADES  → selects the per-object transform
//
// Output SV_RenderTargetArrayIndex routes the primitive to the correct depth-array layer,
// eliminating the need for a geometry shader.
//
// The cascade LVP matrices are read from LightConstants.Shadows[] (cbuffer b1, space1),
// which is populated with all NUM_CASCADES cascade data before this pass runs.

#include "../Bindings/View.hlsl"
#include "../Bindings/Draw.hlsl"

// Match the same input layout as Default.VS.hlsl so mesh vertex buffers are compatible.
struct ShadowVSInput
{
    float3 Position  : POSITION;
    float3 Normal    : NORMAL;
    float2 TexCoord  : TEXCOORD0;
    float4 Tangent   : TANGENT;
#if SKINNED
    float4 BoneWeights : BLENDWEIGHT;
    uint4  BoneIndices : BLENDINDICES;
#endif
};

struct ShadowVSOutput
{
    float4 Position               : SV_POSITION;
    uint   RenderTargetArrayIndex : SV_RenderTargetArrayIndex;
};

ShadowVSOutput VSMain(ShadowVSInput input, uint instanceID : SV_InstanceID)
{
    uint cascadeIdx = instanceID % NUM_CASCADES;
    uint objectIdx  = instanceID / NUM_CASCADES;

    InstanceData inst = Instances[objectIdx];

#if SKINNED
    float4x4 skinMatrix =
        BoneTransforms[inst.BoneOffset + input.BoneIndices.x] * input.BoneWeights.x +
        BoneTransforms[inst.BoneOffset + input.BoneIndices.y] * input.BoneWeights.y +
        BoneTransforms[inst.BoneOffset + input.BoneIndices.z] * input.BoneWeights.z +
        BoneTransforms[inst.BoneOffset + input.BoneIndices.w] * input.BoneWeights.w;

    float4 worldPos = mul(mul(float4(input.Position, 1.0), skinMatrix), inst.Model);
#else
    float4 worldPos = mul(float4(input.Position, 1.0), inst.Model);
#endif

    ShadowVSOutput output;
    output.Position               = mul(worldPos, Shadows[cascadeIdx].LightViewProjection);
    output.RenderTargetArrayIndex = cascadeIdx;
    return output;
}
