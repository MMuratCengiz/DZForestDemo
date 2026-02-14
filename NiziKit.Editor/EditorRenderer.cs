using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.Gizmos;
using NiziKit.Editor.ViewModels;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;

namespace NiziKit.Editor;

public class EditorRenderer(IRenderer gameRenderer) : IDisposable
{
    private readonly RenderFrame _renderFrame = new();
    private readonly ViewData _viewData = new();

    private readonly CycledTexture _sceneDepth = CycledTexture.DepthAttachment("EditorSceneDepth");

    private readonly EditorOverlayPass _overlayPass = new();
    private EditorViewModel? _editorViewModel;

    public RenderFrame RenderFrame => _renderFrame;

    public EditorViewModel? EditorViewModel
    {
        get => _editorViewModel;
        set => _editorViewModel = value;
    }

    public EditorOverlayPass EditorOverlayPass => _overlayPass;

    public CameraComponent? Camera
    {
        get => _viewData.Camera;
        set
        {
            _viewData.Camera = value;
            gameRenderer.Camera = value;
        }
    }

    public CycledTexture RenderScene()
    {
        var sceneColor = gameRenderer.Render(_renderFrame);
        RenderGizmos(sceneColor);
        return sceneColor;
    }

    private void RenderGizmos(CycledTexture sceneColor)
    {
        var scene = World.CurrentScene;
        if (scene != null)
        {
            _viewData.Scene = scene;
            _viewData.DeltaTime = Time.DeltaTime;
            _viewData.TotalTime = Time.TotalTime;
        }

        _overlayPass.BeginFrame();

        var selected = _editorViewModel?.SelectedGameObject?.GameObject;
        _overlayPass.Gizmo.Target = selected;

        if (selected != null)
        {
            _overlayPass.AddSelectionBox(selected);
            _overlayPass.BuildColliderWireframes(selected);
            _overlayPass.BuildSkeletonOverlay(selected);

            if (_viewData.Camera != null)
            {
                _overlayPass.BuildGizmoGeometry(_viewData.Camera);
            }
        }

        if (scene != null && _viewData.Camera != null)
        {
            _overlayPass.BuildSceneIcons(scene, _viewData.Camera, selected);
        }

        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, sceneColor, LoadOp.Load);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Load);
        pass.Begin();

        _overlayPass.Render(pass, _viewData, _sceneDepth);

        pass.End();
    }

    public void OnResize(uint width, uint height)
    {
        gameRenderer.OnResize(width, height);
    }

    public void Dispose()
    {
        _overlayPass.Dispose();
        _sceneDepth.Dispose();
        _renderFrame.Dispose();
        gameRenderer.Dispose();
    }
}
