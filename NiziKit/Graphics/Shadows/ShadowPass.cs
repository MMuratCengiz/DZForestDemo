using NiziKit.Core;
using NiziKit.Graphics.Resources;
using NiziKit.Light;

namespace NiziKit.Graphics.Shadows;

public class ShadowPass
{
    // 1st List => ShadowMaps, 2nd List => Cascades of each shadow map
    private List<List<CycledTexture>> _shadowMaps = [];


    public void Update()
    {
        var camera = World.CurrentScene.GetActiveCamera();

        var lights = World.CurrentScene.GetObjectsOfType<DirectionalLight>();
    }
}
