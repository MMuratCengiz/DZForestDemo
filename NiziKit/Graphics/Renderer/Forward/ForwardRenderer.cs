using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private readonly ViewData _viewData;
    private readonly GpuShader _defaultShader;
    private readonly GpuShader _skinnedShader;
    private readonly List<(GpuShader shader, SurfaceComponent surface, RenderBatch batch)> _drawList = new(256);

    private CycledTexture _sceneColor = null!;
    private CycledTexture _sceneDepth = null!;
    private uint _width;
    private uint _height;

    public CameraComponent? Camera
    {
        get => _viewData.Camera;
        set => _viewData.Camera = value;
    }

    public ForwardRenderer()
    {
        _viewData = new ViewData();
        _width = GraphicsContext.Width;
        _height = GraphicsContext.Height;
        CreateRenderTargets();

        var defaultShader = new DefaultShader();
        _defaultShader = defaultShader.StaticVariant;
        _skinnedShader = defaultShader.SkinnedVariant;
    }

    private void CreateRenderTargets()
    {
        _sceneColor = CycledTexture.ColorAttachment("SceneColor");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth");
    }

    public CycledTexture Render(RenderFrame frame)
    {
        var renderWorld = World.RenderWorld;
        var scene = World.CurrentScene;
        if (scene == null)
        {
            return _sceneColor;
        }

        _viewData.Scene = scene;
        _viewData.DeltaTime = Time.DeltaTime;
        _viewData.TotalTime = Time.TotalTime;

        _drawList.Clear();
        foreach (var surface in renderWorld.GetSurfaces())
        {
            foreach (var batch in renderWorld.GetBatches(surface))
            {
                var shader = SelectShader(batch);
                _drawList.Add((shader, surface, batch));
            }
        }

        _drawList.Sort((a, b) => a.shader.GetHashCode().CompareTo(b.shader.GetHashCode()));

        var pass = frame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);

        pass.Begin();

        pass.Bind<ViewBinding>(_viewData);

        GpuShader? currentShader = null;
        SurfaceComponent? currentSurface = null;

        foreach (var (shader, surface, batch) in _drawList)
        {
            if (currentShader != shader)
            {
                pass.BindShader(shader);
                currentShader = shader;
                currentSurface = null;
            }

            if (currentSurface != surface)
            {
                pass.Bind<SurfaceBinding>(surface);
                currentSurface = surface;
            }

            pass.Bind<BatchDrawBinding>(batch);
            pass.DrawMesh(batch.Mesh, (uint)batch.Count);
        }

        pass.End();

        return _sceneColor;
    }

    private GpuShader SelectShader(RenderBatch batch)
    {
        foreach (var obj in batch.Objects)
        {
            if (obj.Tags?.TryGetValue("variant", out var variant) == true
                && variant.Equals("SKINNED", StringComparison.OrdinalIgnoreCase))
            {
                return _skinnedShader;
            }
        }
        return _defaultShader;
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
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
    }
}
