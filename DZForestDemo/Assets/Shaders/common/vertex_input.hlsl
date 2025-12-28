// Common vertex input structures
// All meshes (including geometry primitives) use the full 80-byte vertex layout

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
    float4 Tangent : TANGENT;
    float4 BoneWeights : BLENDWEIGHT;
    uint4 BoneIndices : BLENDINDICES;
};

struct VSInputColored
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};
