using System.Numerics;
using NiziKit.Components;

namespace NiziKit.Particles;

public class ParticleSystem : NiziComponent
{
    // Emission
    [JsonProperty("maxParticles")] public int MaxParticles { get; set; } = 2048;
    [JsonProperty("emissionRate")] public float EmissionRate { get; set; } = 100f;
    [JsonProperty("isEmitting")] public bool IsEmitting { get; set; } = true;

    // Lifetime
    [JsonProperty("startLifetimeMin")] public float StartLifetimeMin { get; set; } = 0.8f;
    [JsonProperty("startLifetimeMax")] public float StartLifetimeMax { get; set; } = 2.0f;

    // Speed
    [JsonProperty("startSpeedMin")] public float StartSpeedMin { get; set; } = 1.0f;
    [JsonProperty("startSpeedMax")] public float StartSpeedMax { get; set; } = 3.0f;

    // Size
    [JsonProperty("startSizeMin")] public float StartSizeMin { get; set; } = 0.02f;
    [JsonProperty("startSizeMax")] public float StartSizeMax { get; set; } = 0.15f;

    // Physics
    [JsonProperty("gravityModifier")] public float GravityModifier { get; set; } = -2.0f;
    [JsonProperty("drag")] public float Drag { get; set; } = 1.5f;

    // Shape
    [JsonProperty("emitterRadius")] public float EmitterRadius { get; set; } = 0.3f;
    [JsonProperty("emitterAngle")] public float EmitterAngle { get; set; } = 0.2f;

    // Color over lifetime
    [JsonProperty("startColorR")] public float StartColorR { get; set; } = 1f;
    [JsonProperty("startColorG")] public float StartColorG { get; set; } = 0.6f;
    [JsonProperty("startColorB")] public float StartColorB { get; set; } = 0.15f;
    [JsonProperty("startColorA")] public float StartColorA { get; set; } = 1f;

    [JsonProperty("endColorR")] public float EndColorR { get; set; } = 0.3f;
    [JsonProperty("endColorG")] public float EndColorG { get; set; } = 0.05f;
    [JsonProperty("endColorB")] public float EndColorB { get; set; } = 0f;
    [JsonProperty("endColorA")] public float EndColorA { get; set; } = 0f;

    // Runtime state (not serialized)
    internal float EmitAccumulator;
    internal int NextEmitIndex;
    internal int BurstCount;
    internal int GpuSlotIndex = -1;

    [HideInInspector]
    public Vector4 StartColor => new(StartColorR, StartColorG, StartColorB, StartColorA);

    [HideInInspector]
    public Vector4 EndColor => new(EndColorR, EndColorG, EndColorB, EndColorA);

    public void Emit(int count) => BurstCount += count;
}
