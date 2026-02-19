struct Particle
{
    float4 Position; // xyz=position, w=lifetime remaining
    float4 Velocity; // xyz=velocity, w=max lifetime
    float4 Color;    // rgba base color
};

struct ResolvedParticle
{
    float4 PositionAndSize; // xyz=position, w=size
    float4 Color;           // rgba
};

struct SystemParams
{
    float3 EmitterPosition;
    float _pad;
    uint EmitStartIndex;
    uint EmitCount;
    float StartLifetimeMin;
    float StartLifetimeMax;
    float StartSpeedMin;
    float StartSpeedMax;
    float StartSizeMin;
    float StartSizeMax;
    float GravityModifier;
    float Drag;
    float EmitterRadius;
    float EmitterAngle;
    float4 StartColor;
    float4 EndColor;
};

cbuffer ParticleConstants : register(b0)
{
    float4x4 ViewProjection;
    float3 CameraPosition;
    float DeltaTime;
    float3 CameraRight;
    float TotalTime;
    float3 CameraUp;
    uint ParticlesPerSystem;
    uint NumActiveSystems;
    uint _pad0;
    uint _pad1;
    uint _pad2;
    SystemParams Systems[8];
};

RWStructuredBuffer<Particle> g_Particles : register(u0);
RWStructuredBuffer<ResolvedParticle> g_Resolved : register(u1);

// PCG hash for random number generation
uint pcg_hash(uint input)
{
    uint state = input * 747796405u + 2891336453u;
    uint word = ((state >> ((state >> 28u) + 4u)) ^ state) * 277803737u;
    return (word >> 22u) ^ word;
}

float rand_float(uint seed)
{
    return float(pcg_hash(seed)) / 4294967295.0;
}

float rand_range(uint seed, float minVal, float maxVal)
{
    return minVal + rand_float(seed) * (maxVal - minVal);
}

[numthreads(64, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint idx = id.x;
    uint systemIdx = idx / ParticlesPerSystem;
    uint localIdx = idx % ParticlesPerSystem;

    if (systemIdx >= NumActiveSystems)
        return;

    SystemParams sys = Systems[systemIdx];

    Particle p = g_Particles[idx];
    float life = p.Position.w;
    float maxLife = p.Velocity.w;

    // Check if this particle is in the emission range (ring-buffer index matching)
    bool inEmitRange = false;
    if (sys.EmitCount > 0)
    {
        uint end = (sys.EmitStartIndex + sys.EmitCount) % ParticlesPerSystem;
        if (sys.EmitStartIndex < end)
            inEmitRange = (localIdx >= sys.EmitStartIndex && localIdx < end);
        else
            inEmitRange = (localIdx >= sys.EmitStartIndex || localIdx < end);
    }

    // Dead particle in emit range - respawn
    if (life <= 0.0 && inEmitRange)
    {
        uint timeBits = asuint(TotalTime);
        uint seed0 = pcg_hash(idx * 6791u + timeBits);
        uint seed1 = pcg_hash(seed0);
        uint seed2 = pcg_hash(seed1);
        uint seed3 = pcg_hash(seed2);
        uint seed4 = pcg_hash(seed3);
        uint seed5 = pcg_hash(seed4);

        // Random position offset within emitter radius
        float angle = rand_range(seed0, 0.0, 6.28318);
        float radius = rand_range(seed1, 0.0, sys.EmitterRadius);
        float3 offset = float3(cos(angle) * radius, 0.0, sin(angle) * radius);

        p.Position.xyz = sys.EmitterPosition + offset;

        // Random velocity with cone spread based on EmitterAngle
        float speed = rand_range(seed2, sys.StartSpeedMin, sys.StartSpeedMax);
        float spreadX = rand_range(seed3, -sys.EmitterAngle, sys.EmitterAngle);
        float spreadZ = rand_range(seed4, -sys.EmitterAngle, sys.EmitterAngle);
        p.Velocity.xyz = float3(spreadX * speed, speed, spreadZ * speed);

        // Lifetime from properties
        float newLife = rand_range(seed5, sys.StartLifetimeMin, sys.StartLifetimeMax);
        p.Position.w = newLife;
        p.Velocity.w = newLife;

        p.Color = sys.StartColor;

        g_Particles[idx] = p;
        life = newLife;
        maxLife = newLife;
    }

    // Update alive particle
    if (life > 0.0)
    {
        life -= DeltaTime;

        float3 vel = p.Velocity.xyz;

        // Apply gravity modifier (negative = upward/buoyancy, positive = downward)
        vel.y -= sys.GravityModifier * DeltaTime;

        // Apply drag
        vel *= (1.0 - sys.Drag * DeltaTime);

        // Slight turbulence
        uint turbSeed = pcg_hash(idx * 3571u + asuint(TotalTime * 10.0));
        float turbX = (rand_float(turbSeed) - 0.5) * 0.5 * DeltaTime;
        float turbZ = (rand_float(pcg_hash(turbSeed)) - 0.5) * 0.5 * DeltaTime;
        vel.x += turbX;
        vel.z += turbZ;

        float3 pos = p.Position.xyz + vel * DeltaTime;

        p.Position.xyz = pos;
        p.Position.w = life;
        p.Velocity.xyz = vel;
        g_Particles[idx] = p;

        ResolvedParticle resolved;

        if (life <= 0.0)
        {
            resolved.PositionAndSize = float4(0, 0, 0, 0);
            resolved.Color = float4(0, 0, 0, 0);
        }
        else
        {
            float t = saturate(life / maxLife); // 1.0 at birth, 0.0 at death

            // Size: grows slightly then shrinks (parabolic curve)
            float sizeCurve = t * (2.0 - t);
            float size = lerp(sys.StartSizeMin, sys.StartSizeMax, sizeCurve);

            // Color: lerp from StartColor to EndColor over lifetime
            float4 color = lerp(sys.EndColor, sys.StartColor, t);

            // Alpha: fade in quickly, fade out slowly
            float alpha = saturate(t * 3.0) * color.a * t;

            resolved.PositionAndSize = float4(pos, size);
            resolved.Color = float4(color.rgb, alpha);
        }

        g_Resolved[idx] = resolved;
    }
    else
    {
        // Dead particle not being respawned - invisible
        ResolvedParticle resolved;
        resolved.PositionAndSize = float4(0, 0, 0, 0);
        resolved.Color = float4(0, 0, 0, 0);
        g_Resolved[idx] = resolved;
    }
}
