struct PSInput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    // Soft circle falloff
    float2 centered = input.UV * 2.0 - 1.0;
    float dist = length(centered);
    float falloff = saturate(1.0 - dist * dist);
    falloff *= falloff; // sharper edges

    // Premultiplied alpha for additive blending
    float alpha = input.Color.a * falloff;
    float3 color = input.Color.rgb * alpha;

    return float4(color, alpha);
}
