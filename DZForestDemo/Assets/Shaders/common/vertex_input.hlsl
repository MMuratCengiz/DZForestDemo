// Common vertex input structures

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD0;
};

struct VSInputColored
{
    float3 Position : POSITION;
    float4 Color : COLOR;
};
