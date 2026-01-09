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

StructuredBuffer<InstanceData> Instances : register(t0, space3);