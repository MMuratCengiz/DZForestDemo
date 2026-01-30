using Avalonia.Threading;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.Gizmos;
using NiziKit.Editor.ViewModels;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;
using NiziKit.Skia.Avalonia;

namespace NiziKit.Editor;

public class EditorRenderer : IDisposable
{
    private readonly RenderFrame _renderFrame;
    private readonly ViewData _viewData;
    private readonly DenOfIzTopLevel _topLevel;
    private readonly IRenderer _gameRenderer;

    private readonly CycledTexture _sceneDepth;

    private readonly GizmoPass _gizmoPass;
    private EditorViewModel? _editorViewModel;

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
            _gameRenderer.Camera = value;
        }
    }

    public EditorRenderer(DenOfIzTopLevel topLevel, IRenderer gameRenderer)
    {
        _topLevel = topLevel;
        _gameRenderer = gameRenderer;
        _renderFrame = new RenderFrame();
        _viewData = new ViewData();
        _sceneDepth = CycledTexture.DepthAttachment("EditorSceneDepth");
        _gizmoPass = new GizmoPass();
    }

    public void Render(float dt)
    {
        _renderFrame.BeginFrame();

        var sceneColor = _gameRenderer.Render(_renderFrame);

        RenderGizmos(sceneColor);

        DenOfIzPlatform.TriggerRenderTick(TimeSpan.FromSeconds(dt));
        Dispatcher.UIThread.RunJobs();
        _topLevel.Render();

        if (_topLevel.Texture != null)
        {
            _renderFrame.AlphaBlit(_topLevel.Texture, sceneColor);
        }

        _renderFrame.Submit();
        _renderFrame.Present(sceneColor);
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

        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, sceneColor, LoadOp.Load);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Load);
        pass.Begin();

        _gizmoPass.Render(pass, _viewData, _sceneDepth);

        pass.End();
    }

    public void OnResize(uint width, uint height)
    {
        _gameRenderer.OnResize(width, height);
    }

    public void Dispose()
    {
        _gizmoPass.Dispose();
        _sceneDepth.Dispose();
        _renderFrame.Dispose();
        _gameRenderer.Dispose();
    }
}
