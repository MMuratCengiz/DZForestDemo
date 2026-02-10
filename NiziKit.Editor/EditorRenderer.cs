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

    private readonly GizmoPass _gizmoPass = new();
    private EditorViewModel? _editorViewModel;

    public RenderFrame RenderFrame => _renderFrame;

    public EditorViewModel? EditorViewModel
    {
        get => _editorViewModel;
        set => _editorViewModel = value;
    }

    public GizmoPass GizmoPass => _gizmoPass;

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

        _gizmoPass.BeginFrame();

        var selected = _editorViewModel?.SelectedGameObject?.GameObject;
        _gizmoPass.Gizmo.Target = selected;

        if (selected != null)
        {
            _gizmoPass.AddSelectionBox(selected);

            if (_viewData.Camera != null)
            {
                _gizmoPass.BuildGizmoGeometry(_viewData.Camera);
            }
        }

        if (scene != null && _viewData.Camera != null)
        {
            _gizmoPass.BuildSceneIcons(scene, _viewData.Camera, selected);
        }

        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, sceneColor, LoadOp.Load);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Load);
        pass.Begin();

        _gizmoPass.Render(pass, _viewData, _sceneDepth);

        pass.End();
    }

    public void OnResize(uint width, uint height)
    {
        gameRenderer.OnResize(width, height);
    }

    public void Dispose()
    {
        _gizmoPass.Dispose();
        _sceneDepth.Dispose();
        _renderFrame.Dispose();
        gameRenderer.Dispose();
    }
}
