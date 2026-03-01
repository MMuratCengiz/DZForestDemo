using DenOfIz;
using DZForestDemo.UI;
using NiziKit.Application;
using NiziKit.Core;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.UI;

namespace DZForestDemo;
//
// public sealed class DemoGame(GameDesc? desc = null) : Game(desc)
// {
//     private IRenderer _renderer = null!;
//     private RenderFrame _renderFrame = null!;
//     private readonly DemoGameUi _ui = new();
//
//     public override Type RendererType => typeof(ForwardRenderer);
//
//     protected override void Load(Game game)
//     {
//         _renderFrame = new RenderFrame();
//         _renderFrame.EnableDebugOverlay(DebugOverlayConfig.Default);
//         _renderFrame.EnableUi(UiContextDesc.Default);
//
//         _renderer = new ForwardRenderer();
//         World.LoadScene("Scenes/CombatDemo.niziscene.json");
//     }
//
//     private void RenderUi()
//     {
//         _ui.Render();
//     }
//
//     protected override void Update(float dt)
//     {
//         _renderFrame.BeginFrame();
//         var sceneTexture = _renderer.Render(_renderFrame);
//
//         var debugOverlay = _renderFrame.RenderDebugOverlay();
//         _renderFrame.AlphaBlit(debugOverlay, sceneTexture);
//
//         var ui = _renderFrame.RenderUi(RenderUi);
//         _renderFrame.AlphaBlit(ui, sceneTexture);
//
//         _renderFrame.Submit();
//         _renderFrame.Present(sceneTexture);
//     }
//
//     protected override void OnEvent(ref Event ev)
//     {
//     }
//
//     protected override void FixedUpdate(float fixedDt)
//     {
//     }
//
//     protected override void OnShutdown()
//     {
//         _renderer?.Dispose();
//         _renderFrame?.Dispose();
//     }
// }
