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
        var meshComp = go.GetComponent<MeshComponent>();
        if (meshComp?.Mesh != null && !meshComp.Mesh.IsUploaded)
        {
            assets.Upload(meshComp.Mesh);
        }
    }

    public void GameObjectDestroyed(GameObject go)
    {
    }
}
