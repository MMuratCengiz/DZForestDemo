#ifndef SHADOW_HLSL
#define SHADOW_HLSL

// Shadow resources (space1, continuing after ViewConstants b0 and LightConstants b1).
Texture2DArray<float>       ShadowCascades : register(t2, space1);
SamplerComparisonState      ShadowSampler  : register(s3, space1);

// 16-tap Poisson disk in the unit circle.
// Rotated per world-position to eliminate grid repetition in PCF sampling.
static const float2 PoissonDisk[16] =
{
    float2(-0.94201624f, -0.39906216f),
    float2( 0.94558609f, -0.76890725f),
    float2(-0.09418410f, -0.92938870f),
    float2( 0.34495938f,  0.29387760f),
    float2(-0.91588581f,  0.45771432f),
    float2(-0.81544232f, -0.87912464f),
    float2(-0.38277543f,  0.27676845f),
    float2( 0.97484398f,  0.75648379f),
    float2( 0.44323325f, -0.97511554f),
    float2( 0.53742981f, -0.47373420f),
    float2(-0.26496911f, -0.41893023f),
    float2( 0.79197514f,  0.19090188f),
    float2(-0.24188840f,  0.99706507f),
    float2(-0.81409955f,  0.91437590f),
    float2( 0.19984126f,  0.78641367f),
    float2( 0.14383161f, -0.14100790f)
};

// ────────────────────────────────────────────────────────────────────────────────
// SampleShadow – selects the appropriate cascade for worldPos and returns [0,1]
// shadow factor (1 = fully lit, 0 = fully in shadow).
//
// Requires ViewConstants (CameraForward, CameraPosition) and LightConstants
// (Shadows[], NumShadows) to be declared before this file is included.
//
// shadowIndex: value of Light.ShadowIndex (first cascade slot for this light).
//              Pass -1 to skip shadows (returns 1.0).
// ────────────────────────────────────────────────────────────────────────────────
float SampleShadow(int shadowIndex, float3 worldPos, float3 normal)
{
    if (shadowIndex < 0 || (uint)shadowIndex >= NumShadows)
        return 1.0;

    // Linear view-space depth for cascade selection.
    float viewDepth = dot(CameraForward, worldPos - CameraPosition);

    // Walk cascades from farthest to closest; last write wins → picks the tightest cascade.
    // Avoids relying on break inside [unroll] which some compilers implement imprecisely.
    int cascadeIdx = NUM_CASCADES - 1;
    [unroll]
    for (int i = NUM_CASCADES - 2; i >= 0; i--)
    {
        if (viewDepth < Shadows[shadowIndex + i].SplitDistance)
            cascadeIdx = i;
    }

    int totalIdx = shadowIndex + cascadeIdx;
    ShadowData sd = Shadows[totalIdx];

    // Project with normal-offset bias to reduce shadow acne.
    float3 biasedPos = worldPos + normal * sd.NormalBias;
    float4 lightClip = mul(float4(biasedPos, 1.0), sd.LightViewProjection);
    float3 ndc       = lightClip.xyz / lightClip.w;

    // D3D NDC → UV (flip Y because texture V=0 is at top).
    float2 uv = ndc.xy * 0.5 + 0.5;
    uv.y = 1.0 - uv.y;

    // Reject fragments outside the shadow map.
    if (any(uv < 0.0) || any(uv > 1.0))
        return 1.0;

    float compareDepth = ndc.z - sd.Bias;

    float arrayW, arrayH, arrayElements;
    ShadowCascades.GetDimensions(arrayW, arrayH, arrayElements);
    float2 texelSize = 1.0f / float2(arrayW, arrayH);

    // Rotate the Poisson disk by a per-world-position angle so the sampling pattern
    // is different at every surface point. Using worldPos (not screen pos) keeps the
    // noise stable in world space – it does not swim as the camera moves.
    float angle = frac(sin(dot(worldPos.xz, float2(127.1f, 311.7f))) * 43758.5453f) * 6.28318f;
    float sinA, cosA;
    sincos(angle, sinA, cosA);

    static const float CascadeFilterRadius[NUM_CASCADES] = { 1.0f, 1.5f, 2.0f, 2.5f };
    float filterRadius = CascadeFilterRadius[cascadeIdx];
    float shadow = 0.0f;
    float slice  = (float)totalIdx;
    [unroll]
    for (int k = 0; k < 16; k++)
    {
        float2 s = float2(
            PoissonDisk[k].x * cosA - PoissonDisk[k].y * sinA,
            PoissonDisk[k].x * sinA + PoissonDisk[k].y * cosA);
        shadow += ShadowCascades.SampleCmpLevelZero(
            ShadowSampler, float3(uv + s * filterRadius * texelSize, slice), compareDepth);
    }
    return shadow / 16.0f;
}

#endif
