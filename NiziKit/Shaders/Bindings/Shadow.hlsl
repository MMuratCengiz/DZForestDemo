#ifndef SHADOW_HLSL
#define SHADOW_HLSL

// Shadow resources (space1, continuing after ViewConstants b0 and LightConstants b1).
Texture2DArray<float>       ShadowCascades : register(t2, space1);
SamplerComparisonState      ShadowSampler  : register(s3, space1);

// ────────────────────────────────────────────────────────────────────────────────
// Utility functions
// ────────────────────────────────────────────────────────────────────────────────

// Vogel disk sample (golden-angle spiral) – provides mathematically uniform
// coverage with arbitrary sample counts, eliminating the clumping that Poisson
// disks suffer from at small radii.
float2 VogelDiskSample(int sampleIndex, int sampleCount, float rotation)
{
    float goldenAngle = 2.4f; // ~137.5 degrees in radians
    float r = sqrt((float)sampleIndex + 0.5f) / sqrt((float)sampleCount);
    float theta = sampleIndex * goldenAngle + rotation;
    return float2(r * cos(theta), r * sin(theta));
}

// Interleaved gradient noise – frame-stable screen-space noise that produces
// cleaner patterns than world-space hashing. Standard technique in modern
// engines (UE4/5, Unity HDRP).
float InterleavedGradientNoise(float2 screenPos)
{
    return frac(52.9829189f * frac(dot(screenPos, float2(0.06711056f, 0.00583715f))));
}

// ────────────────────────────────────────────────────────────────────────────────
// Constants
// ────────────────────────────────────────────────────────────────────────────────

#define PCF_SAMPLE_COUNT     32
#define BLOCKER_SEARCH_COUNT 16

// Filter radii in texels – must be large enough that the soft band is visible
// on screen. At 4096x4096 with cascade 0 covering ~20 world units, 1 texel ≈ 0.5cm,
// so radius 8 ≈ 4cm of soft edge.
static const float CascadeFilterRadius[NUM_CASCADES] = { 5.0f, 8.0f, 11.0f, 14.0f };

// ────────────────────────────────────────────────────────────────────────────────
// Internal: PCF filtering with Vogel disk
// ────────────────────────────────────────────────────────────────────────────────

float PCF_VogelDisk(float2 uv, float slice, float compareDepth,
                    float filterRadius, float2 texelSize, float rotation)
{
    float shadow = 0.0f;
    [loop]
    for (int k = 0; k < PCF_SAMPLE_COUNT; k++)
    {
        float2 s = VogelDiskSample(k, PCF_SAMPLE_COUNT, rotation);
        shadow += ShadowCascades.SampleCmpLevelZero(
            ShadowSampler,
            float3(uv + s * filterRadius * texelSize, slice),
            compareDepth);
    }
    return shadow / (float)PCF_SAMPLE_COUNT;
}

// ────────────────────────────────────────────────────────────────────────────────
// Internal: project world position into shadow map UV space for a cascade
// ────────────────────────────────────────────────────────────────────────────────

struct ShadowCoord
{
    float2 uv;
    float  depth;
    bool   valid;
};

ShadowCoord ProjectToShadowMap(float3 worldPos, float3 normal, float3 lightDir,
                                ShadowData sd)
{
    ShadowCoord coord;

    // Slope-scale bias: surfaces at grazing angles to the light need more normal
    // offset than surfaces facing the light directly. This virtually eliminates
    // shadow acne on steep surfaces without causing peter panning on flat ones.
    float NdotL = saturate(dot(normal, lightDir));
    float slopeFactor = sqrt(1.0 - NdotL * NdotL) / max(NdotL, 0.001);
    float3 biasedPos = worldPos + normal * sd.NormalBias * max(slopeFactor, 1.0);

    float4 lightClip = mul(float4(biasedPos, 1.0), sd.LightViewProjection);
    float3 ndc       = lightClip.xyz / lightClip.w;

    // D3D NDC -> UV (flip Y because texture V=0 is at top).
    coord.uv    = ndc.xy * 0.5 + 0.5;
    coord.uv.y  = 1.0 - coord.uv.y;
    coord.depth = ndc.z - sd.Bias;
    coord.valid = !(any(coord.uv < 0.0) || any(coord.uv > 1.0));

    return coord;
}

// ────────────────────────────────────────────────────────────────────────────────
// Internal: PCSS blocker search – finds average blocker depth for penumbra
// estimation. Uses Load() to read raw depth (no comparison sampler needed).
// ────────────────────────────────────────────────────────────────────────────────

float PCSS_BlockerSearch(float2 uv, float slice, float receiverDepth,
                         float searchRadius, float2 texelSize, float rotation,
                         float arrayW, float arrayH, out float blockerCount)
{
    float blockerSum = 0.0f;
    blockerCount = 0.0f;

    [unroll]
    for (int i = 0; i < BLOCKER_SEARCH_COUNT; i++)
    {
        float2 s = VogelDiskSample(i, BLOCKER_SEARCH_COUNT, rotation);
        float2 sampleUV = uv + s * searchRadius * texelSize;

        int3 loadCoord = int3(
            clamp((int)(sampleUV.x * arrayW), 0, (int)arrayW - 1),
            clamp((int)(sampleUV.y * arrayH), 0, (int)arrayH - 1),
            (int)slice);
        float blockerDepth = ShadowCascades.Load(int4(loadCoord, 0));

        if (blockerDepth < receiverDepth)
        {
            blockerSum += blockerDepth;
            blockerCount += 1.0f;
        }
    }

    return blockerCount > 0.0f ? blockerSum / blockerCount : 0.0f;
}

// ────────────────────────────────────────────────────────────────────────────────
// Internal: sample a single cascade with PCF or PCSS
// ────────────────────────────────────────────────────────────────────────────────

float SampleCascade(ShadowCoord coord, int cascadeIdx, float slice,
                    float2 texelSize, float rotation, float lightSize,
                    float arrayW, float arrayH)
{
    if (!coord.valid)
        return 1.0f;

    float baseRadius = CascadeFilterRadius[cascadeIdx];
    float filterRadius = baseRadius;

    // PCSS: contact-hardening shadows when LightSize > 0.
    // Shadows are sharp near contact points and soft far from the caster.
    if (lightSize > 0.0f)
    {
        float searchRadius = lightSize * baseRadius;
        float blockerCount;
        float avgBlockerDepth = PCSS_BlockerSearch(
            coord.uv, slice, coord.depth, searchRadius, texelSize, rotation,
            arrayW, arrayH, blockerCount);

        if (blockerCount < 1.0f)
            return 1.0f; // No blockers found -- fully lit.

        // Classic penumbra estimation: wider penumbra when receiver is far
        // from blocker, sharp when close.
        float penumbraRatio = (coord.depth - avgBlockerDepth) / max(avgBlockerDepth, 0.0001) * lightSize;
        filterRadius = clamp(penumbraRatio * baseRadius, 0.5f, baseRadius * 4.0f);
    }

    return PCF_VogelDisk(coord.uv, slice, coord.depth, filterRadius, texelSize, rotation);
}

// ────────────────────────────────────────────────────────────────────────────────
// SampleShadow – main entry point
//
// Selects the appropriate cascade for worldPos and returns [0,1] shadow factor
// (1 = fully lit, 0 = fully in shadow).
//
// shadowIndex: value of Light.ShadowIndex (first cascade slot). -1 = no shadow.
// lightDir:    normalized direction FROM surface TO light.
// screenPos:   SV_POSITION.xy for interleaved gradient noise.
// ────────────────────────────────────────────────────────────────────────────────

float SampleShadow(int shadowIndex, float3 worldPos, float3 normal,
                   float3 lightDir, float2 screenPos)
{
    if (shadowIndex < 0 || (uint)shadowIndex >= NumShadows)
        return 1.0;

    // Linear view-space depth for cascade selection.
    float viewDepth = dot(CameraForward, worldPos - CameraPosition);

    // Walk cascades from farthest to closest; last write wins -> picks the
    // tightest cascade. Avoids relying on break inside [unroll].
    int cascadeIdx = NUM_CASCADES - 1;
    [unroll]
    for (int i = NUM_CASCADES - 2; i >= 0; i--)
    {
        if (viewDepth < Shadows[shadowIndex + i].SplitDistance)
            cascadeIdx = i;
    }

    int totalIdx = shadowIndex + cascadeIdx;
    ShadowData sd = Shadows[totalIdx];

    // Shadow map dimensions (shared across all cascades).
    float arrayW, arrayH, arrayElements;
    ShadowCascades.GetDimensions(arrayW, arrayH, arrayElements);
    float2 texelSize = 1.0f / float2(arrayW, arrayH);

    // Screen-space rotation via interleaved gradient noise.
    float rotation = InterleavedGradientNoise(screenPos) * 6.28318f;

    // Project to shadow map with slope-scale bias.
    ShadowCoord coord = ProjectToShadowMap(worldPos, normal, lightDir, sd);
    if (!coord.valid)
        return 1.0f;

    float slice = (float)totalIdx;
    float shadow = SampleCascade(coord, cascadeIdx, slice, texelSize, rotation,
                                  sd.LightSize, arrayW, arrayH);

    // ── Cascade blending ──────────────────────────────────────────────────────
    // Sample both cascades near split boundaries and blend with smoothstep to
    // eliminate the visible resolution pop when transitioning between cascades.
    if (cascadeIdx < NUM_CASCADES - 1)
    {
        float splitDist = sd.SplitDistance;
        // Blend band = 10% of the gap between current and previous split.
        float prevSplit = (cascadeIdx > 0)
            ? Shadows[shadowIndex + cascadeIdx - 1].SplitDistance
            : 0.0f;
        float blendBand = (splitDist - prevSplit) * 0.1f;
        float blendStart = splitDist - blendBand;

        if (viewDepth > blendStart)
        {
            int nextIdx = totalIdx + 1;
            ShadowData sdNext = Shadows[nextIdx];
            ShadowCoord coordNext = ProjectToShadowMap(worldPos, normal, lightDir, sdNext);
            float sliceNext = (float)nextIdx;
            float shadowNext = SampleCascade(coordNext, cascadeIdx + 1, sliceNext,
                                              texelSize, rotation, sdNext.LightSize,
                                              arrayW, arrayH);

            float blendFactor = smoothstep(blendStart, splitDist, viewDepth);
            shadow = lerp(shadow, shadowNext, blendFactor);
        }
    }

    return shadow;
}

#endif
