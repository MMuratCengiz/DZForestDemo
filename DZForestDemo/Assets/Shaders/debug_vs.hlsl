// Debug vertex shader - simple pass-through for colored vertices

#include "common/vertex_input.hlsl"

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

PSInput VSMain(VSInputColored input)
{
    PSInput output;
    output.Position = float4(input.Position, 1.0);
    output.Color = input.Color;
    return output;
}
