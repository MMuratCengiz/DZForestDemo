using RuntimeAssets;

namespace ECS.Components;

public struct MeshComponent
{
    public RuntimeMeshHandle Mesh;

    public MeshComponent(RuntimeMeshHandle mesh)
    {
        Mesh = mesh;
    }

    public bool IsValid => Mesh.IsValid;
}
