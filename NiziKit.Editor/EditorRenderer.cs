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

public class EditorRenderer : IRenderer
{
    private readonly RenderFrame _renderFrame;
    private readonly ViewData _viewData;
    private readonly DenOfIzTopLevel _topLevel;

    private CycledTexture _sceneColor = null!;
    private CycledTexture _sceneDepth = null!;
    private uint _width;
    private uint _height;

    private GizmoPass _gizmoPass = null!;
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
        set => _viewData.Camera = value;
    }

    public EditorRenderer(DenOfIzTopLevel topLevel)
    {
        _topLevel = topLevel;
        _renderFrame = new RenderFrame();
        _viewData = new ViewData();
        _width = GraphicsContext.Width;
        _height = GraphicsContext.Height;
        CreateRenderTargets();
        _gizmoPass = new GizmoPass();
    }

    private void CreateRenderTargets()
    {
        _sceneColor = CycledTexture.ColorAttachment("SceneColor");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth");
    }

    public void Render()
    {
        Render(0);
    }

    public void Render(float dt)
    {
        _renderFrame.BeginFrame();

        RenderScene();
        RenderGizmos();

        DenOfIzPlatform.TriggerRenderTick(TimeSpan.FromSeconds(dt));
        Dispatcher.UIThread.RunJobs();
        _topLevel.Render();

        if (_topLevel.Texture != null)
        {
            _renderFrame.AlphaBlit(_topLevel.Texture, _sceneColor);
        }

        _renderFrame.Submit();
        _renderFrame.Present(_sceneColor);
    }

    private void RenderScene()
    {
        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);
        pass.Begin();

        var scene = World.CurrentScene;
        if (scene != null)
        {
            var renderWorld = World.RenderWorld;

            _viewData.Scene = scene;
            _viewData.DeltaTime = Time.DeltaTime;
            _viewData.TotalTime = Time.TotalTime;

            pass.Bind<ViewBinding>(_viewData);

            foreach (var material in renderWorld.GetMaterials())
            {
                var gpuShader = material.GpuShader;
                if (gpuShader == null)
                {
                    continue;
                }

                pass.BindShader(gpuShader);
                pass.Bind<MaterialBinding>(material);

                foreach (var batch in renderWorld.GetDrawBatches(material))
                {
                    pass.Bind<BatchDrawBinding>(batch);
                    pass.DrawMesh(batch.Mesh, (uint)batch.Count);
                }
            }
        }

        pass.End();
    }

    private void RenderGizmos()
    {
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
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Load);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Load);
        pass.Begin();

        pass.Bind<ViewBinding>(_viewData);
        _gizmoPass.Render(pass, _viewData, _sceneDepth);

        pass.End();
    }

    public void OnResize(uint width, uint height)
    {
        if (_width == width && _height == height)
        {
            return;
        }

        GraphicsContext.WaitIdle();

        _sceneColor.Dispose();
        _sceneDepth.Dispose();

        _width = width;
        _height = height;
        CreateRenderTargets();
    }

    public void Dispose()
    {
        GraphicsContext.WaitIdle();
        _gizmoPass.Dispose();
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
        _renderFrame.Dispose();
    }
}
