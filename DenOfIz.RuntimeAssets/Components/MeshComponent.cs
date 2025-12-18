namespace RuntimeAssets.Components;

public struct MeshComponent(RuntimeMeshHandle mesh)
{
    public RuntimeMeshHandle Mesh = mesh;

    public bool IsValid => Mesh.IsValid;
}