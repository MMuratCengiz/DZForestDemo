// Screen-space bilateral blur for shadow edge smoothing.
// Guided by the depth buffer so geometry edges remain sharp while shadow
// boundaries (same surface, different shadow) get smoothed out.

Texture2D<float4> SceneColor  : register(t0);
Texture2D<float>  SceneDepth  : register(t1);
SamplerState      LinearSampler : register(s0);

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 texSize;
    SceneColor.GetDimensions(texSize.x, texSize.y);
    float2 texel = 1.0 / texSize;

    float4 centerColor = SceneColor.Sample(LinearSampler, input.TexCoord);
    float  centerDepth = SceneDepth.SampleLevel(LinearSampler, input.TexCoord, 0);

    // Skip sky pixels (depth at far plane).
    if (centerDepth >= 0.999)
        return centerColor;

    float4 result     = centerColor;
    float  totalWeight = 1.0;

    // Depth sensitivity: higher = more edge-preserving.
    // Tuned so that depth discontinuities > ~0.2% of the depth range are treated
    // as edges and not blurred across.
    float depthSensitivity = 500.0;

    // Diamond-shaped kernel covering a 5x5 area (21 taps including center).
    // This is enough to eliminate single-pixel shadow staircases without
    // over-blurring fine detail.
    [unroll]
    for (int y = -2; y <= 2; y++)
    {
        [unroll]
        for (int x = -2; x <= 2; x++)
        {
            if (x == 0 && y == 0) continue;

            // Diamond: skip samples where Manhattan distance > 3.
            if (abs(x) + abs(y) > 3) continue;

            float2 offset = float2(x, y) * texel;
            float4 sampleColor = SceneColor.Sample(LinearSampler, input.TexCoord + offset);
            float  sampleDepth = SceneDepth.SampleLevel(LinearSampler, input.TexCoord + offset, 0);

            // Spatial weight (Gaussian falloff).
            float dist2 = float(x * x + y * y);
            float spatialW = exp(-dist2 / 4.5);

            // Depth weight (bilateral term) – suppresses blur across geometry edges.
            float depthDiff = abs(sampleDepth - centerDepth);
            float depthW = exp(-depthDiff * depthSensitivity);

            float w = spatialW * depthW;
            result += sampleColor * w;
            totalWeight += w;
        }
    }

    return result / totalWeight;
}
