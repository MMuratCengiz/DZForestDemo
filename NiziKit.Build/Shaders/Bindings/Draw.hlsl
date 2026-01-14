#ifndef DRAW_HLSL
#define DRAW_HLSL

struct InstanceData
{
    float4x4 Model;
    float4 BaseColor;
    float Metallic;
    float Roughness;
    float AmbientOcclusion;
    uint UseAlbedoTexture;
    uint BoneOffset;
    uint _Pad0;
    uint _Pad1;
    uint _Pad2;
};


#ifndef MAX_INStANCES
#define MAX_INSTANCES 500
#endif

#ifndef MAX_BONES
#define MAX_BONES 256
#endif

cbuffer InstanceConstants : register(b0, space3)
{
    InstanceData Instances[MAX_INSTANCES];
};

cbuffer BoneTransforms : register(b1, space3)
{
    float4x4 BoneTransforms[MAX_BONES];
};

#endif
