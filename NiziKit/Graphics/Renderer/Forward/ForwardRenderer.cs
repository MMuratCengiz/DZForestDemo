using System.Numerics;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;
using NiziKit.Graphics.Renderer.Common;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private readonly ViewData _viewData;
    private readonly DefaultShader _defaultShaderSet;
    private GpuShader _defaultShader;
    private GpuShader _skinnedShader;
    private readonly SkyboxPass _skyboxPass;

    private readonly CycledTexture _sceneColor;
    private readonly CycledTexture _sceneDepth;

    private readonly List<(GpuShader shader, SurfaceComponent surface, RenderBatch batch)> _drawList = new(256);

    public CameraComponent? Camera
    {
        get => _viewData.Camera;
        set => _viewData.Camera = value;
    }

    public ForwardRenderer()
    {
        _viewData = new ViewData();

        _sceneColor = CycledTexture.ColorAttachment("SceneColor");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth");

        _defaultShaderSet = new DefaultShader();
        _defaultShader = _defaultShaderSet.StaticVariant;
        _skinnedShader = _defaultShaderSet.SkinnedVariant;

        ShaderHotReload.OnShadersReloaded += OnShadersReloaded;

        _skyboxPass = new SkyboxPass();
    }

    public CycledTexture Render(RenderFrame frame)
    {
        var renderWorld = World.RenderWorld;
        var scene = World.CurrentScene;
        var camera = _viewData.Camera ?? scene.GetActiveCamera();

        _viewData.Scene = scene;
        _viewData.DeltaTime = Time.DeltaTime;
        _viewData.TotalTime = Time.TotalTime;

        // Build sorted draw list.
        _drawList.Clear();
        foreach (var surface in renderWorld.GetSurfaces())
        {
            foreach (var batch in renderWorld.GetBatches(surface))
            {
                _drawList.Add((SelectShader(batch), surface, batch));
            }
        }
        _drawList.Sort((a, b) => a.shader.GetHashCode().CompareTo(b.shader.GetHashCode()));
        var pass = frame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);
        pass.Begin();

        var skybox = scene.Skybox;
        if (skybox is { IsValid: true } && camera != null)
        {
            Matrix4x4.Invert(camera.ViewProjectionMatrix, out var invVp);
            _skyboxPass.Execute(pass, invVp, skybox);
        }

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
            if (obj.Tags?.GetBool("SKINNED") == true)
            {
                return _skinnedShader;
            }
        }
        return _defaultShader;
    }

    private void OnShadersReloaded()
    {
        if (!_defaultShaderSet.Rebuild())
        {
            return;
        }

        _defaultShader = _defaultShaderSet.StaticVariant;
        _skinnedShader = _defaultShaderSet.SkinnedVariant;
    }

    public void OnResize(uint width, uint height)
    {
    }

    public void Dispose()
    {
        ShaderHotReload.OnShadersReloaded -= OnShadersReloaded;
        _defaultShaderSet.Dispose();
        _skyboxPass.Dispose();
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
    }
}
