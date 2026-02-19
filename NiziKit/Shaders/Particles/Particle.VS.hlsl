struct ResolvedParticle
{
    float4 PositionAndSize; // xyz=position, w=size
    float4 Color;           // rgba
};

cbuffer ParticleConstants : register(b0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float DeltaTime;
    float3 CameraRight;
    float TotalTime;
    float3 CameraUp;
    uint MaxParticles;
    float3 EmitterPosition;
    float _Pad0;
};

StructuredBuffer<ResolvedParticle> g_Resolved : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
};

VSOutput VSMain(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    VSOutput output;

    // 2 triangles forming a quad: 0,1,2, 2,1,3
    // Vertex layout:
    // 0: (-1, -1)  1: ( 1, -1)
    // 2: (-1,  1)  3: ( 1,  1)
    static const float2 quadOffsets[6] = {
        float2(-1, -1), // tri 0
        float2( 1, -1),
        float2(-1,  1),
        float2(-1,  1), // tri 1
        float2( 1, -1),
        float2( 1,  1)
    };

    static const float2 quadUVs[6] = {
        float2(0, 1),
        float2(1, 1),
        float2(0, 0),
        float2(0, 0),
        float2(1, 1),
        float2(1, 0)
    };

    ResolvedParticle particle = g_Resolved[instanceID];
    float3 particlePos = particle.PositionAndSize.xyz;
    float size = particle.PositionAndSize.w;

    float2 offset = quadOffsets[vertexID];
    float3 worldPos = particlePos
        + CameraRight * offset.x * size
        + CameraUp * offset.y * size;

    output.Position = mul(float4(worldPos, 1.0), ViewProjection);
    output.UV = quadUVs[vertexID];
    output.Color = particle.Color;

    return output;
}
