using System.Numerics;
using DenOfIz;
using NiziKit.Core;
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

    public override BindGroupLayout Layout => GraphicsContext.BindGroupLayoutStore.Camera;
    public override bool RequiresCycling => true;

    public ViewBinding()
    {
        _cameraBuffer = new MappedBuffer<GpuCamera>(true, "ViewBinding_Camera");
        _lightBuffer = new MappedBuffer<LightConstants>(true, "ViewBinding_Lights");

        for (var i = 0; i < NumBindGroups; i++)
        {
            var bg = BindGroups[i];
            bg.BeginUpdate();
            bg.Cbv(GpuCameraLayout.Camera.Binding, _cameraBuffer[i]);
            bg.Cbv(GpuCameraLayout.Lights.Binding, _lightBuffer[i]);
            bg.EndUpdate();
        }
    }

    protected override void OnUpdate(ViewData target)
    {
        var camera = BuildCamera(target);
        var lights = BuildLights(target.Scene);

        _cameraBuffer.Write(in camera);
        _lightBuffer.Write(in lights);
    }

    private static GpuCamera BuildCamera(ViewData viewData)
    {
        var cam = viewData.Camera ?? viewData.Scene.MainCamera;
        if (cam == null)
        {
            return default;
        }

        var deltaTime = viewData.DeltaTime;
        var totalTime = viewData.TotalTime;

        Matrix4x4.Invert(cam.ViewProjectionMatrix, out var invVp);
        return new GpuCamera
        {
            View = cam.ViewMatrix,
            Projection = cam.ProjectionMatrix,
            ViewProjection = cam.ViewProjectionMatrix,
            InverseViewProjection = invVp,
            CameraPosition = cam.WorldPosition,
            CameraForward = cam.Forward,
            ScreenSize = new Vector2(GraphicsContext.Width, GraphicsContext.Height),
            NearPlane = cam.NearPlane,
            FarPlane = cam.FarPlane,
            Time = totalTime,
            DeltaTime = deltaTime
        };
    }

    private static unsafe LightConstants BuildLights(Scene scene)
    {
        const int maxLights = LightConstantsCapacity.MaxLights;

        var lights = stackalloc GpuLightData[maxLights];
        var lightIndex = 0;

        foreach (var dl in scene.GetObjectsOfType<DirectionalLight>())
        {
            if (lightIndex >= maxLights || !dl.IsActive)
            {
                continue;
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
                ShadowIndex = -1
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
            AmbientSkyColor = new Vector3(0.4f, 0.5f, 0.6f),
            AmbientGroundColor = new Vector3(0.2f, 0.18f, 0.15f),
            AmbientIntensity = 0.3f,
            NumLights = (uint)lightIndex,
            NumShadows = 0
        };

        var lightPtr = (GpuLightData*)result.Lights;
        for (var i = 0; i < lightIndex; i++)
        {
            lightPtr[i] = lights[i];
        }

        return result;
    }

    public override void Dispose()
    {
        _cameraBuffer.Dispose();
        _lightBuffer.Dispose();
        base.Dispose();
    }
}
