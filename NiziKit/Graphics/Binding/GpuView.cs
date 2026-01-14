using System.Numerics;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Core;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;
using NiziKit.Light;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Binding;

public class GpuView : IDisposable
{
    private const uint LightTypeDirectional = 0;
    private const uint LightTypePoint = 1;
    private const uint LightTypeSpot = 2;

    private readonly GraphicsContext _ctx;
    private readonly uint _numFrames;

    private readonly Buffer[] _cameraBuffers;
    private readonly Buffer[] _lightBuffers;
    private readonly IntPtr[] _cameraMappedPtrs;
    private readonly IntPtr[] _lightMappedPtrs;
    private readonly BindGroup[] _bindGroups;

    private GpuCamera _camera;
    private LightConstants _lights;
    private Texture? _shadowAtlas;
    private readonly Sampler? _shadowSampler;
    private readonly object _updateLock = new();
    private bool _isDirty = true;

    public GpuView(GraphicsContext ctx)
    {
        _ctx = ctx;
        _numFrames = ctx.NumFrames;

        _cameraBuffers = new Buffer[_numFrames];
        _lightBuffers = new Buffer[_numFrames];
        _cameraMappedPtrs = new IntPtr[_numFrames];
        _lightMappedPtrs = new IntPtr[_numFrames];
        _bindGroups = new BindGroup[_numFrames];

        var cameraSize = (uint)Marshal.SizeOf<GpuCamera>();
        var lightSize = (uint)Marshal.SizeOf<LightConstants>();

        for (var i = 0; i < _numFrames; i++)
        {
            _cameraBuffers[i] = ctx.LogicalDevice.CreateBuffer(new BufferDesc
            {
                NumBytes = cameraSize,
                HeapType = HeapType.CpuGpu,
                Usage = (uint)BufferUsageFlagBits.Uniform,
                DebugName = StringView.Create($"GpuView_Camera_{i}")
            });
            _cameraMappedPtrs[i] = _cameraBuffers[i].MapMemory();

            _lightBuffers[i] = ctx.LogicalDevice.CreateBuffer(new BufferDesc
            {
                NumBytes = lightSize,
                HeapType = HeapType.CpuGpu,
                Usage = (uint)BufferUsageFlagBits.Uniform,
                DebugName = StringView.Create($"GpuView_Lights_{i}")
            });
            _lightMappedPtrs[i] = _lightBuffers[i].MapMemory();

            _bindGroups[i] = ctx.LogicalDevice.CreateBindGroup(new BindGroupDesc
            {
                Layout = ctx.BindGroupLayoutStore.Camera
            });
        }

        _shadowSampler = ctx.LogicalDevice.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToBorder,
            AddressModeV = SamplerAddressMode.ClampToBorder,
            AddressModeW = SamplerAddressMode.ClampToBorder,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest,
            CompareOp = CompareOp.LessOrEqual
        });
    }

    public void Update(Scene scene, uint frameIndex, float deltaTime, float totalTime)
    {
        BuildCameraFromScene(scene, deltaTime, totalTime);
        BuildLightsFromScene(scene);

        Marshal.StructureToPtr(_camera, _cameraMappedPtrs[frameIndex], false);
        Marshal.StructureToPtr(_lights, _lightMappedPtrs[frameIndex], false);
    }

    private void BuildCameraFromScene(Scene scene, float deltaTime, float totalTime)
    {
        var cam = scene.MainCamera;
        if (cam == null)
        {
            return;
        }

        Matrix4x4.Invert(cam.ViewProjectionMatrix, out var invVp);

        _camera = new GpuCamera
        {
            View = cam.ViewMatrix,
            Projection = cam.ProjectionMatrix,
            ViewProjection = cam.ViewProjectionMatrix,
            InverseViewProjection = invVp,
            CameraPosition = cam.WorldPosition,
            CameraForward = cam.Forward,
            ScreenSize = new Vector2(_ctx.Width, _ctx.Height),
            NearPlane = cam.NearPlane,
            FarPlane = cam.FarPlane,
            Time = totalTime,
            DeltaTime = deltaTime
        };
        _isDirty = true;
    }

    private unsafe void BuildLightsFromScene(Scene scene)
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

        _lights = new LightConstants
        {
            AmbientSkyColor = new Vector3(0.4f, 0.5f, 0.6f),
            AmbientGroundColor = new Vector3(0.2f, 0.18f, 0.15f),
            AmbientIntensity = 0.3f,
            NumLights = (uint)lightIndex,
            NumShadows = 0
        };

        fixed (byte* lightsBytes = _lights.Lights)
        {
            var lightPtr = (GpuLightData*)lightsBytes;
            for (var i = 0; i < lightIndex; i++)
            {
                lightPtr[i] = lights[i];
            }
        }

        _isDirty = true;
    }

    public void SetCamera(in GpuCamera camera)
    {
        _camera = camera;
        _isDirty = true;
    }

    public void SetLights(in LightConstants lights)
    {
        _lights = lights;
        _isDirty = true;
    }

    public void SetShadowAtlas(Texture? shadowAtlas)
    {
        _shadowAtlas = shadowAtlas;
        _isDirty = true;
    }

    public BindGroup GetBindGroup(uint frameIndex)
    {
        lock (_updateLock)
        {
            if (_isDirty)
            {
                UpdateBindings(frameIndex);
            }

            return _bindGroups[frameIndex];
        }
    }

    [Obsolete("Use Update(Scene, frameIndex, deltaTime, totalTime) instead")]
    public void Update(uint frameIndex)
    {
        // Upload camera data
        Marshal.StructureToPtr(_camera, _cameraMappedPtrs[frameIndex], false);

        // Upload light data
        Marshal.StructureToPtr(_lights, _lightMappedPtrs[frameIndex], false);
    }

    private void UpdateBindings(uint frameIndex)
    {
        var bg = _bindGroups[frameIndex];
        bg.BeginUpdate();

        bg.Cbv(GpuCameraLayout.Camera.Binding, _cameraBuffers[frameIndex]);
        bg.Cbv(GpuCameraLayout.Lights.Binding, _lightBuffers[frameIndex]);

        if (_shadowAtlas != null)
        {
            bg.SrvTexture(GpuCameraLayout.ShadowAtlas.Binding, _shadowAtlas);
        }
        else
        {
            bg.SrvTexture(GpuCameraLayout.ShadowAtlas.Binding, _ctx.NullTexture.Texture);
        }

        if (_shadowSampler != null)
        {
            bg.Sampler(GpuCameraLayout.ShadowSampler.Binding, _shadowSampler);
        }

        bg.EndUpdate();
        _isDirty = false;
    }

    public void Dispose()
    {
        for (var i = 0; i < _numFrames; i++)
        {
            _cameraBuffers[i].UnmapMemory();
            _cameraBuffers[i].Dispose();
            _lightBuffers[i].UnmapMemory();
            _lightBuffers[i].Dispose();
            _bindGroups[i].Dispose();
        }

        _shadowSampler?.Dispose();
    }
}