// Vertex input for model meshes (Static and Skinned)
// This matches the Vertex struct in MeshData.cs (80 bytes)

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
    float4 Tangent : TANGENT;
    float4 BoneWeights : BLENDWEIGHT;
    uint4 BoneIndices : BLENDINDICES;
};
