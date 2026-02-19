

struct Particle
{
    float4 Position;
    float4 Velocity;
    float4 Color;
};

struct DrawIndexIndirectCommand
{
    uint NumIndices;
    uint NumInstances;
    uint FirstIndex;
    int VertexOffset;
    uint FirstInstance;
};

RWStructuredBuffer<Particle> g_Particles : register(u0);
RWStructuredBuffer<DrawIndexIndirectCommand> g_IndirectCommands : register(u1);

[numthreads(64, 1, 1)]
void CSMain(uint3 id: SV_DispatchThreadID)
{

}
