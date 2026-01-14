using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Assets;

public class AssetWorld(Assets assets) : IWorldEventListener
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

    public void ComponentAdded(GameObject go, IComponent component)
    {
        if (component is MeshComponent)
        {
            TryUploadMesh(go);
        }
    }

    public void ComponentRemoved(GameObject go, IComponent component)
    {
    }

    private void TryUploadMesh(GameObject go)
    {
        var meshComp = go.GetComponent<MeshComponent>();
        if (meshComp?.Mesh != null && !meshComp.Mesh.IsUploaded)
        {
            assets.Upload(meshComp.Mesh);
        }
    }
}
