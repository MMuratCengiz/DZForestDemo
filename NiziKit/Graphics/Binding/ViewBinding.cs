using System.Numerics;
using DenOfIz;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;
using NiziKit.Graphics.Buffers;
using NiziKit.Light;

namespace NiziKit.Graphics.Binding;

public class ViewBinding : ShaderBinding<ViewData>
{
    private const uint LightTypeDirectional = 0;
    private const uint LightTypePoint = 1;
    private const uint LightTypeSpot = 2;

    private readonly MappedBuffer<GpuCamera> _cameraBuffer;
    private readonly MappedBuffer<LightConstants> _lightBuffer;
    private readonly Sampler _shadowSampler;

    public override BindGroupLayout Layout => GraphicsContext.BindGroupLayoutStore.Camera;
    public override bool RequiresCycling => true;

    public ViewBinding()
    {
        _cameraBuffer = new MappedBuffer<GpuCamera>(true, "ViewBinding_Camera");
        _lightBuffer = new MappedBuffer<LightConstants>(true, "ViewBinding_Lights");

        _shadowSampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest,
            CompareOp = CompareOp.LessOrEqual
        });

        for (var i = 0; i < NumBindGroups; i++)
        {
            var bg = BindGroups[i];
            bg.BeginUpdate();
            bg.Cbv(GpuCameraLayout.Camera.Binding, _cameraBuffer[i]);
            bg.Cbv(GpuCameraLayout.Lights.Binding, _lightBuffer[i]);
            bg.SrvTexture(GpuCameraLayout.ShadowAtlas.Binding, ColorTexture.Missing.Texture);
            bg.Sampler(GpuCameraLayout.ShadowSampler.Binding, _shadowSampler);
            bg.EndUpdate();
        }
    }

    protected override void OnUpdate(ViewData target)
    {
        var camera = BuildCamera(target);
        var lights = BuildLights(target);

        _cameraBuffer.Write(in camera);
        _lightBuffer.Write(in lights);

        if (target.ShadowAtlas != null)
        {
            var bg = BindGroups[GraphicsContext.FrameIndex];
            bg.BeginUpdate();
            bg.Cbv(GpuCameraLayout.Camera.Binding, _cameraBuffer[GraphicsContext.FrameIndex]);
            bg.Cbv(GpuCameraLayout.Lights.Binding, _lightBuffer[GraphicsContext.FrameIndex]);
            bg.SrvTexture(GpuCameraLayout.ShadowAtlas.Binding, target.ShadowAtlas[GraphicsContext.FrameIndex]);
            bg.Sampler(GpuCameraLayout.ShadowSampler.Binding, _shadowSampler);
            bg.EndUpdate();
        }
    }

    private static GpuCamera BuildCamera(ViewData viewData)
    {
        var cam = viewData.Camera ?? viewData.Scene.GetActiveCamera();
        if (cam == null && viewData.ViewProjectionOverride == null)
        {
            return default;
        }

        var deltaTime = viewData.DeltaTime;
        var totalTime = viewData.TotalTime;

        var vp = viewData.ViewProjectionOverride ?? cam!.ViewProjectionMatrix;
        Matrix4x4.Invert(vp, out var invVp);

        return new GpuCamera
        {
            View = cam?.ViewMatrix ?? Matrix4x4.Identity,
            Projection = cam?.ProjectionMatrix ?? Matrix4x4.Identity,
            ViewProjection = vp,
            InverseViewProjection = invVp,
            CameraPosition = cam?.WorldPosition ?? Vector3.Zero,
            CameraForward = cam?.Forward ?? Vector3.UnitZ,
            ScreenSize = new Vector2(GraphicsContext.Width, GraphicsContext.Height),
            NearPlane = cam?.NearPlane ?? 0.1f,
            FarPlane = cam?.FarPlane ?? 1000f,
            Time = totalTime,
            DeltaTime = deltaTime
        };
    }

    private static unsafe LightConstants BuildLights(ViewData viewData)
    {
        var scene = viewData.Scene;
        const int maxLights = LightConstantsCapacity.MaxLights;
        const int maxShadows = LightConstantsCapacity.MaxShadowLights;

        var lights = stackalloc GpuLightData[maxLights];
        var lightIndex = 0;
        var shadowCasters = viewData.ShadowCasters;

        foreach (var dl in scene.GetObjectsOfType<DirectionalLight>())
        {
            if (lightIndex >= maxLights || !dl.IsActive)
            {
                continue;
            }

            var shadowIdx = -1;
            for (var s = 0; s < shadowCasters.Length; s++)
            {
                if (shadowCasters[s].LightIndex == lightIndex)
                {
                    shadowIdx = s;
                    break;
                }
            }

            lights[lightIndex++] = new GpuLightData
            {
                PositionOrDirection = dl.Direction,
                Type = LightTypeDirectional,
                Color = dl.Color,
                Intensity = dl.Intensity,
                SpotDirection = dl.Direction,
                Radius = 0,
                InnerConeAngle = 0,
                OuterConeAngle = 0,
                ShadowIndex = shadowIdx
            };
        }

        foreach (var pl in scene.GetObjectsOfType<PointLight>())
        {
            if (lightIndex >= maxLights || !pl.IsActive)
            {
                continue;
            }

            lights[lightIndex++] = new GpuLightData
            {
                PositionOrDirection = pl.WorldPosition,
                Type = LightTypePoint,
                Color = pl.Color,
                Intensity = pl.Intensity,
                SpotDirection = Vector3.Zero,
                Radius = pl.Range,
                InnerConeAngle = 0,
                OuterConeAngle = 0,
                ShadowIndex = -1
            };
        }

        foreach (var sl in scene.GetObjectsOfType<SpotLight>())
        {
            if (lightIndex >= maxLights || !sl.IsActive)
            {
                continue;
            }

            lights[lightIndex++] = new GpuLightData
            {
                PositionOrDirection = sl.WorldPosition,
                Type = LightTypeSpot,
                Color = sl.Color,
                Intensity = sl.Intensity,
                SpotDirection = sl.Direction,
                Radius = sl.Range,
                InnerConeAngle = sl.InnerConeAngle,
                OuterConeAngle = sl.OuterConeAngle,
                ShadowIndex = -1
            };
        }

        var result = new LightConstants
        {
            AmbientSkyColor = scene.AmbientSkyColor,
            AmbientGroundColor = scene.AmbientGroundColor,
            AmbientIntensity = scene.AmbientIntensity,
            NumLights = (uint)lightIndex,
            NumShadows = (uint)shadowCasters.Length
        };

        var lightPtr = (GpuLightData*)result.Lights;
        for (var i = 0; i < lightIndex; i++)
        {
            lightPtr[i] = lights[i];
        }

        var shadowPtr = (GpuShadowData*)result.Shadows;
        for (var s = 0; s < shadowCasters.Length && s < maxShadows; s++)
        {
            shadowPtr[s] = new GpuShadowData
            {
                LightViewProjection = shadowCasters[s].LightViewProjection,
                AtlasScaleOffset = shadowCasters[s].AtlasScaleOffset,
                Bias = shadowCasters[s].Bias,
                NormalBias = shadowCasters[s].NormalBias
            };
        }

        return result;
    }

    public override void Dispose()
    {
        _shadowSampler.Dispose();
        _cameraBuffer.Dispose();
        _lightBuffer.Dispose();
        base.Dispose();
    }
}
