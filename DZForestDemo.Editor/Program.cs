using NiziKit.Editor;
using NiziKit.Graphics.Renderer.Renderer2D;

Editor.Run(new EditorDesc
{
    GameProjectDir = "../DZForestDemo",
    InitialScene = "Scenes/Sprite2DDemo.niziscene.json",
    RendererType = typeof(Renderer2D)
});
