using System.Numerics;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;
using NiziKit.Light;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private const int ShadowMapSize = 2048;
    private const int MaxShadowCasters = 4;
    private const float ShadowOrthoSize = 50f;
    private const float ShadowNearPlane = 0.1f;
    private const float ShadowFarPlane = 200f;

    private readonly ViewData _viewData;
    private readonly ViewData _shadowViewData;
    private readonly GpuShader _defaultShader;
    private readonly GpuShader _skinnedShader;
    private readonly GpuShader _shadowCasterShader;
    private readonly GpuShader _shadowCasterSkinnedShader;
    private readonly SkyboxPass _skyboxPass;
    private readonly List<(GpuShader shader, SurfaceComponent surface, RenderBatch batch)> _drawList = new(256);

    private readonly CycledTexture _sceneColor;
    private readonly CycledTexture _sceneDepth;
    private readonly CycledTexture _shadowDepth;
    private readonly List<ShadowCasterInfo> _shadowCasterList = new(MaxShadowCasters);
    private ShadowCasterInfo[] _shadowCasterArray = [];

    public CameraComponent? Camera
    {
        get => _viewData.Camera;
        set => _viewData.Camera = value;
    }

    public ForwardRenderer()
    {
        _viewData = new ViewData();
        _shadowViewData = new ViewData();
        _sceneColor = CycledTexture.ColorAttachment("SceneColor");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth");
        _shadowDepth = CycledTexture.DepthAttachment("ShadowDepth", ShadowMapSize, ShadowMapSize);

        var defaultShader = new DefaultShader();
        _defaultShader = defaultShader.StaticVariant;
        _skinnedShader = defaultShader.SkinnedVariant;
        _shadowCasterShader = defaultShader.ShadowCasterVariant;
        _shadowCasterSkinnedShader = defaultShader.ShadowCasterSkinnedVariant;

        _skyboxPass = new SkyboxPass();
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

        var shadowCasters = BuildShadowCasters(scene);
        _viewData.ShadowAtlas = shadowCasters.Length > 0 ? _shadowDepth : null;
        _viewData.ShadowCasters = shadowCasters;

        if (shadowCasters.Length > 0)
        {
            RenderShadowPass(frame, scene, shadowCasters);
        }

        var pass = frame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);

        pass.Begin();

        var skybox = scene.Skybox;
        if (skybox is { IsValid: true })
        {
            var cam = _viewData.Camera ?? scene.GetActiveCamera();
            if (cam != null)
            {
                Matrix4x4.Invert(cam.ViewProjectionMatrix, out var invVp);
                _skyboxPass.Execute(pass, invVp, skybox);
            }
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

    private void RenderShadowPass(RenderFrame frame, Scene scene, ShadowCasterInfo[] shadowCasters)
    {
        for (var i = 0; i < shadowCasters.Length; i++)
        {
            _shadowViewData.Scene = scene;
            _shadowViewData.DeltaTime = Time.DeltaTime;
            _shadowViewData.TotalTime = Time.TotalTime;
            _shadowViewData.ViewProjectionOverride = shadowCasters[i].LightViewProjection;
            _shadowViewData.ShadowCasters = Array.Empty<ShadowCasterInfo>();

            var shadowPass = frame.BeginGraphicsPass();
            shadowPass.SetDepthTarget(_shadowDepth, i == 0 ? LoadOp.Clear : LoadOp.Load);

            shadowPass.Begin();
            shadowPass.Bind<ViewBinding>(_shadowViewData);

            GpuShader? currentShader = null;

            foreach (var (shader, surface, batch) in _drawList)
            {
                var shadowShader = SelectShadowShader(batch);

                if (currentShader != shadowShader)
                {
                    shadowPass.BindShader(shadowShader);
                    currentShader = shadowShader;
                }

                shadowPass.Bind<BatchDrawBinding>(batch);
                shadowPass.DrawMesh(batch.Mesh, (uint)batch.Count);
            }

            shadowPass.End();
        }
    }

    private ShadowCasterInfo[] BuildShadowCasters(Scene scene)
    {
        _shadowCasterList.Clear();
        var lightIndex = 0;

        foreach (var dl in scene.GetObjectsOfType<DirectionalLight>())
        {
            if (!dl.IsActive)
            {
                lightIndex++;
                continue;
            }

            if (dl.CastsShadows && _shadowCasterList.Count < MaxShadowCasters)
            {
                var lightDir = Vector3.Normalize(dl.Direction);
                var lightView = Matrix4x4.CreateLookAtLeftHanded(
                    -lightDir * (ShadowFarPlane * 0.5f),
                    Vector3.Zero,
                    MathF.Abs(Vector3.Dot(lightDir, Vector3.UnitY)) > 0.99f
                        ? Vector3.UnitZ
                        : Vector3.UnitY
                );
                var lightProj = Matrix4x4.CreateOrthographicOffCenterLeftHanded(
                    -ShadowOrthoSize, ShadowOrthoSize,
                    -ShadowOrthoSize, ShadowOrthoSize,
                    ShadowNearPlane, ShadowFarPlane
                );

                _shadowCasterList.Add(new ShadowCasterInfo
                {
                    LightViewProjection = lightView * lightProj,
                    AtlasScaleOffset = new Vector4(1, 1, 0, 0), // Full texture, no atlas subdivision
                    Bias = 0.002f,
                    NormalBias = 0.05f,
                    LightIndex = lightIndex
                });
            }

            lightIndex++;
        }

        if (_shadowCasterList.Count == 0)
        {
            return Array.Empty<ShadowCasterInfo>();
        }

        if (_shadowCasterArray.Length != _shadowCasterList.Count)
        {
            _shadowCasterArray = new ShadowCasterInfo[_shadowCasterList.Count];
        }

        for (var i = 0; i < _shadowCasterList.Count; i++)
        {
            _shadowCasterArray[i] = _shadowCasterList[i];
        }

        return _shadowCasterArray;
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

    private GpuShader SelectShadowShader(RenderBatch batch)
    {
        foreach (var obj in batch.Objects)
        {
            if (obj.Tags?.GetBool("SKINNED") == true)
            {
                return _skinnedShader;
            }
        }
        return _shadowCasterShader;
    }

    public void OnResize(uint width, uint height)
    {
    }

    public void Dispose()
    {
        _skyboxPass.Dispose();
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
        _shadowDepth.Dispose();
    }
}
