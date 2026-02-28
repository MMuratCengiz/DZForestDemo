using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Assets;

public class AssetWorld : IWorldEventListener
{
    public void SceneReset()
    {
    }

    public void GameObjectCreated(GameObject go)
    {
        TryUploadMesh(go);
    }

    public void GameObjectDestroyed(GameObject go)
    {
    }

    public void ComponentAdded(GameObject go, NiziComponent component)
    {
        if (component is MeshComponent)
        {
            TryUploadMesh(go);
        }
    }

    public void ComponentRemoved(GameObject go, NiziComponent component)
    {
    }

    public void ComponentChanged(GameObject go, NiziComponent component)
    {
        if (component is MeshComponent)
        {
            TryUploadMesh(go);
        }
    }

    private static void TryUploadMesh(GameObject go)
    {
        var meshComp = go.GetComponent<MeshComponent>();
        if (meshComp?.Mesh is { IsUploaded: false })
        {
            NiziAssets.Upload(meshComp.Mesh);
        }
    }
}
