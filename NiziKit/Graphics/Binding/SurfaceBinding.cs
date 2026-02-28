using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Components;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.Binding;

public class SurfaceBinding : ShaderBinding<SurfaceComponent>
{
    private readonly GpuBufferView _dataBuffer = GraphicsContext.UniformBufferArena.Request(Marshal.SizeOf<GpuSurfaceData>());
    private readonly Sampler _sampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
    {
        AddressModeU = SamplerAddressMode.Repeat,
        AddressModeV = SamplerAddressMode.Repeat,
        AddressModeW = SamplerAddressMode.Repeat,
        MinFilter = Filter.Linear,
        MagFilter = Filter.Linear,
        MipmapMode = MipmapMode.Linear
    });
    private int _lastHash;

    public override BindGroupLayout Layout => GraphicsContext.BindGroupLayoutStore.Surface;
    public override bool RequiresCycling => false;

    protected override void OnUpdate(SurfaceComponent target)
    {
        var hash = HashCode.Combine(
            target.Albedo,
            target.Normal,
            target.Roughness,
            target.Metallic,
            target.Emissive,
            target.MetallicValue,
            target.RoughnessValue,
            HashCode.Combine(target.AlbedoColor, target.UVScale, target.EmissiveColor, target.EmissiveIntensity));

        if (hash == _lastHash)
        {
            return;
        }
        _lastHash = hash;

        var data = new GpuSurfaceData
        {
            MetallicValue = target.MetallicValue,
            RoughnessValue = target.RoughnessValue,
            UVScale = target.UVScale,
            UVOffset = target.UVOffset,
            AlbedoColor = target.AlbedoColor,
            EmissiveColor = target.EmissiveColor,
            EmissiveIntensity = target.EmissiveIntensity,
            HasAlbedoTexture = target.Albedo != null ? 1.0f : 0.0f,
            HasNormalTexture = target.Normal != null ? 1.0f : 0.0f,
            HasRoughnessTexture = target.Roughness != null ? 1.0f : 0.0f,
            HasMetallicTexture = target.Metallic != null ? 1.0f : 0.0f,
            HasEmissiveTexture = target.Emissive != null ? 1.0f : 0.0f
        };
        _dataBuffer.Buffer.WriteData(in data, _dataBuffer.Offset);

        var bg = BindGroups[0];
        bg.BeginUpdate();
        bg.SrvTexture(GpuSurfaceLayout.Albedo.Binding, target.Albedo?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(GpuSurfaceLayout.Normal.Binding, target.Normal?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(GpuSurfaceLayout.Roughness.Binding, target.Roughness?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(GpuSurfaceLayout.Metallic.Binding, target.Metallic?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.SrvTexture(GpuSurfaceLayout.Emissive.Binding, target.Emissive?.GpuTexture ?? ColorTexture.Missing.Texture);
        bg.Sampler(GpuSurfaceLayout.TextureSampler.Binding, _sampler);
        bg.CbvWithDesc(new BindBufferDesc
        {
            Binding = GpuSurfaceLayout.Constants.Binding,
            Resource = _dataBuffer.Buffer,
            ResourceOffset = _dataBuffer.Offset
        });
        bg.EndUpdate();
    }

    public override void Dispose()
    {
        _sampler.Dispose();
        base.Dispose();
    }
}
