using NiziKit.Components;
using NiziKit.Core;

namespace NiziKit.Assets;

public class AssetWorld : IWorldEventListener
{
    private readonly Assets _assets;

    public AssetWorld(Assets assets)
    {
        _assets = assets;
    }

    public void SceneReset()
    {
    }

    public void GameObjectCreated(GameObject go)
    {
        var meshComp = go.GetComponent<MeshComponent>();
        if (meshComp?.Mesh != null && !meshComp.Mesh.IsUploaded)
        {
            _assets.Upload(meshComp.Mesh);
        }
    }

    public void GameObjectDestroyed(GameObject go)
    {
    }
}
