using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.Binding;

public class MaterialBinding : ShaderBinding<Material>
{
    private readonly GpuBufferView _dataBuffer;
    private readonly Sampler _sampler;
    private int _lastTextureHash;

    public override BindGroupLayout Layout => GraphicsContext.BindGroupLayoutStore.Material;
    public override bool RequiresCycling => false;

    public MaterialBinding()
    {
        _dataBuffer = GraphicsContext.UniformBufferArena.Request(Marshal.SizeOf<GpuMaterialData>());
        _sampler = GraphicsContext.Device.CreateSampler(new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Linear
        });
    }

    protected override void OnUpdate(Material target)
    {
        var textureHash = HashCode.Combine(target.Albedo, target.Normal, target.Roughness, target.Metallic);
        if (textureHash == _lastTextureHash)
        {
            return;
        }
        _lastTextureHash = textureHash;

        var data = new GpuMaterialData();
        _dataBuffer.Buffer.WriteData(in data, _dataBuffer.Offset);

        var bg = BindGroups[0];
        bg.BeginUpdate();
        bg.SrvTexture(GpuMaterialLayout.Albedo.Binding, target.Albedo?.GpuTexture ?? GraphicsContext.MissingTexture.Texture);
        bg.SrvTexture(GpuMaterialLayout.Normal.Binding, target.Normal?.GpuTexture ?? GraphicsContext.MissingTexture.Texture);
        bg.SrvTexture(GpuMaterialLayout.Roughness.Binding, target.Roughness?.GpuTexture ?? GraphicsContext.MissingTexture.Texture);
        bg.SrvTexture(GpuMaterialLayout.Metallic.Binding, target.Metallic?.GpuTexture ?? GraphicsContext.MissingTexture.Texture);
        bg.Sampler(GpuMaterialLayout.TextureSampler.Binding, _sampler);
        bg.CbvWithDesc(new BindBufferDesc
        {
            Binding = GpuMaterialLayout.Constants.Binding,
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
