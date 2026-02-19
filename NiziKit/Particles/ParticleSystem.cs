using NiziKit.Components;

namespace NiziKit.Particles;

public class ParticleSystem : NiziComponent
{
    public int NumParticles { get; set; } = 1000;

    private List<Particle> particles;

    public ParticleSystem()
    {
        particles = new List<Particle>(NumParticles);
        var materialComponent = GetComponent<MaterialComponent>() ?? AddComponent<MaterialComponent>();
        materialComponent.Tags["ParticleSystem"] = "TRUE";
    }

    public void Emit(Particle particle)
    {
        particles.Add(particle);
    }

    public void Update(float dt)
    {
    }
}
