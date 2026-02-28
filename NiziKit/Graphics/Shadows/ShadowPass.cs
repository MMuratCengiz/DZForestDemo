using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;
using NiziKit.Light;

namespace NiziKit.Graphics.Shadows;

/// <summary>
/// Renders cascaded shadow maps into a Texture2DArray using a single draw call per batch
/// with vertex amplification (no geometry shader).
/// </summary>
public sealed class ShadowPass : IDisposable
{
    // ── Configuration ────────────────────────────────────────────────────────────
    public const int MapSize = 4096;
    public const int NumCascades = 4;        // Must match NUM_CASCADES in View.hlsl
    private const float Lambda = 0.75f;      // Practical split blend (0 = linear, 1 = log)
    private const float MaxDistance = 300f;  // World-space shadow draw distance

    // Per-cascade depth bias (NDC units).
    private static readonly float[] CascadeBias = [0.00005f, 0.00005f, 0.0001f, 0.0001f];

    // Per-cascade normal-offset bias in world-space units.
    private static readonly float[] CascadeNormalBias = [0.001f, 0.002f, 0.003f, 0.005f];

    // ── Resources ────────────────────────────────────────────────────────────────

    /// <summary>Texture2DArray with <see cref="NumCascades"/> layers – one per cascade.</summary>
    public CycledTexture ShadowMapArray { get; }

    private readonly ViewData _viewData = new();
    private readonly List<ShadowCasterInfo> _casterList = new(NumCascades);
    private ShadowCasterInfo[] _casterArray = [];

    // ── Shaders ──────────────────────────────────────────────────────────────────
    private GpuShader _shadowCasterShader;
    private GpuShader _shadowCasterSkinnedShader;

    public ShadowPass(GpuShader shadowCasterShader, GpuShader shadowCasterSkinnedShader)
    {
        ShadowMapArray = CycledTexture.DepthArrayAttachment("ShadowMapArray", MapSize, MapSize, NumCascades);
        _shadowCasterShader = shadowCasterShader;
        _shadowCasterSkinnedShader = shadowCasterSkinnedShader;
    }

    /// <summary>Called after hot-reload to refresh shader references.</summary>
    public void UpdateShaders(GpuShader shadowCasterShader, GpuShader shadowCasterSkinnedShader)
    {
        _shadowCasterShader = shadowCasterShader;
        _shadowCasterSkinnedShader = shadowCasterSkinnedShader;
    }

    /// <summary>
    /// Builds cascade data and renders the shadow pass.
    /// Returns the shadow caster array (empty if no shadow-casting lights are found).
    /// </summary>
    public ShadowCasterInfo[] Execute(
        RenderFrame frame,
        Scene scene,
        CameraComponent? camera,
        IReadOnlyList<(GpuShader shader, SurfaceComponent surface, RenderBatch batch)> drawList)
    {
        var shadowCasters = BuildShadowCasters(scene, camera);
        if (shadowCasters.Length > 0)
        {
            Render(frame, scene, camera, shadowCasters, drawList);
        }
        return shadowCasters;
    }

    // ── Shadow rendering ─────────────────────────────────────────────────────────

    /// <summary>
    /// Renders all shadow casters into the cascade depth-array using a single
    /// draw call per batch (vertex amplification via instancing).
    /// </summary>
    private void Render(
        RenderFrame frame,
        Scene scene,
        CameraComponent? camera,
        ShadowCasterInfo[] shadowCasters,
        IReadOnlyList<(GpuShader shader, SurfaceComponent surface, RenderBatch batch)> drawList)
    {
        // Expose cascade LVP matrices through the ViewBinding so the shadow VS
        // can read them from LightConstants.Shadows[cascadeIdx].
        _viewData.Scene = scene;
        _viewData.Camera = camera;
        _viewData.DeltaTime = Time.DeltaTime;
        _viewData.TotalTime = Time.TotalTime;
        _viewData.ShadowCasters = shadowCasters;
        _viewData.ViewProjectionOverride = null;

        var pass = frame.BeginGraphicsPass();
        pass.SetDepthTarget(ShadowMapArray, LoadOp.Clear);
        pass.SetNumLayers((uint)NumCascades);
        pass.SetViewport(0, 0, MapSize, MapSize);
        pass.SetScissor(0, 0, MapSize, MapSize);
        pass.Begin();
        pass.Bind<ViewBinding>(_viewData);

        GpuShader? currentShader = null;

        foreach (var (_, _, batch) in drawList)
        {
            var shader = SelectShader(batch);

            if (currentShader != shader)
            {
                pass.BindShader(shader);
                currentShader = shader;
            }

            pass.Bind<BatchDrawBinding>(batch);

            // Multiply instance count by NumCascades: the VS uses
            //   cascadeIdx = instanceID % NUM_CASCADES
            //   objectIdx  = instanceID / NUM_CASCADES
            pass.DrawMesh(batch.Mesh, (uint)(batch.Count * NumCascades));
        }

        pass.End();
    }

    // ── Cascade setup ─────────────────────────────────────────────────────────────

    private ShadowCasterInfo[] BuildShadowCasters(Scene scene, CameraComponent? camera)
    {
        _casterList.Clear();
        var lightIndex = 0;

        foreach (var dl in scene.GetObjectsOfType<DirectionalLight>())
        {
            if (!dl.IsActive)
            {
                lightIndex++;
                continue;
            }

            if (dl.CastsShadows && _casterList.Count == 0 && camera != null)
            {
                // Only the first shadow-casting directional light gets cascades.
                AddCascadesForLight(dl, lightIndex, camera);
            }

            lightIndex++;
        }

        if (_casterList.Count == 0)
        {
            return Array.Empty<ShadowCasterInfo>();
        }

        if (_casterArray.Length != _casterList.Count)
        {
            _casterArray = new ShadowCasterInfo[_casterList.Count];
        }

        for (var i = 0; i < _casterList.Count; i++)
        {
            _casterArray[i] = _casterList[i];
        }

        return _casterArray;
    }

    private void AddCascadesForLight(DirectionalLight dl, int lightIndex, CameraComponent cam)
    {
        // Cap shadow distance so cascades cover a tight, high-quality area rather than
        // the full camera far plane which can be hundreds or thousands of units away.
        var shadowFar = MathF.Min(cam.FarPlane, MaxDistance);

        var splitDistances = LightShadowCascades.ComputeSplitDistances(
            cam.NearPlane, shadowFar, NumCascades, Lambda);

        var cascadeLVPs = LightShadowCascades.Compute(
            cam.ViewMatrix, cam.ProjectionMatrix,
            dl.Direction, NumCascades, splitDistances, MapSize);

        for (var i = 0; i < NumCascades; i++)
        {
            _casterList.Add(new ShadowCasterInfo
            {
                LightViewProjection = cascadeLVPs[i],
                SplitDistance = splitDistances[i + 1],
                Bias = CascadeBias[i],
                NormalBias = CascadeNormalBias[i],
                LightSize = dl.ShadowSoftness,
                LightIndex = lightIndex
            });
        }
    }

    // ── Shader selection ─────────────────────────────────────────────────────────

    private GpuShader SelectShader(RenderBatch batch)
    {
        foreach (var obj in batch.Objects)
        {
            if (obj.Tags?.GetBool("SKINNED") == true)
            {
                return _shadowCasterSkinnedShader;
            }
        }
        return _shadowCasterShader;
    }

    public void Dispose()
    {
        ShadowMapArray.Dispose();
    }
}
