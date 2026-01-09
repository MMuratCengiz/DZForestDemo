Texture2D AlbedoTexture : register(t0, space2);
Texture2D NormalTexture : register(t1, space2);
Texture2D RoughnessTexture : register(t2, space2);
Texture2D MetallicTexture : register(t3, space2);
SamplerState TextureSampler : register(s0, space2);

cbuffer MaterialConstants : register(b4, space2)
{
    float4 MaterialBaseColor;
    float MaterialMetallic;
    float MaterialRoughness;
    float MaterialAO;
    float _MatPadding;
};
