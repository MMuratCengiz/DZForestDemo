using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.Material;

public struct GpuMaterialDesc
{
    public GpuTexture? Albedo;
    public GpuTexture? Normal;
    public GpuTexture? Roughness;
    public GpuTexture? Metallic;
    public GpuMaterialData Material;
}

public class GpuMaterial
{
    private GpuTexture? _albedo;
    private GpuTexture? _normal;
    private GpuTexture? _roughness;
    private GpuTexture? _metallic;
    private GpuMaterialData _data;
    private GpuBufferView _dataBuffer;
    private BindGroup _bindGroup;
    private GraphicsContext _ctx;
    private bool _isDirty;

    public GpuMaterial(GraphicsContext context, GpuMaterialDesc desc)
    {
        _ctx = context;
        _albedo = desc.Albedo;
        _normal = desc.Normal;
        _roughness = desc.Roughness;
        _metallic = desc.Metallic;
        _data = desc.Material;
        
        var bindGroupDesc = new BindGroupDesc();
        bindGroupDesc.Layout = context.BindGroupLayoutStore.Material;
        _bindGroup = context.LogicalDevice.CreateBindGroup(bindGroupDesc);
        _isDirty = true;

        _dataBuffer = context.UniformBufferArena.Request(Marshal.SizeOf<GpuMaterialData>());
        _dataBuffer.Buffer.WriteData(in desc.Material, _dataBuffer.Offset);
        _bindGroup.BeginUpdate();
        var bindBufferDesc = new BindBufferDesc();

        BindTexture(GpuMaterialLayout.Albedo.Binding, _albedo);
        BindTexture(GpuMaterialLayout.Normal.Binding, _normal);
        BindTexture(GpuMaterialLayout.Roughness.Binding, _roughness);
        BindTexture(GpuMaterialLayout.Metallic.Binding, _metallic);
        bindBufferDesc.Binding = GpuMaterialLayout.Material.Binding;
        bindBufferDesc.Resource = _dataBuffer.Buffer;
        bindBufferDesc.ResourceOffset = _dataBuffer.Offset;
        _bindGroup.CbvWithDesc(bindBufferDesc);
        
        _bindGroup.EndUpdate();
    }

    public void BindTexture(uint binding, GpuTexture? texture)
    {
        if (texture == null)
        {
            _bindGroup.SrvTexture(binding, _ctx.NullTexture.Texture);
        }
        else
        {
            _bindGroup.SrvTexture(binding, texture.Texture);
        }
    }

    public BindGroup BindGroup
    {
        get
        {
            Update();
            return _bindGroup;
        }
    }

    public void SetData(GpuMaterialData data)
    {
        _data = data;
        _isDirty = true;
    }

    private void Update()
    {
        if (!_isDirty)
        {
            return;
        }
    }
}