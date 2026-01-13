using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.Binding;

public class GpuMaterial
{
    private static readonly Dictionary<Material, GpuMaterial> Instances = new();
    
    private readonly Material _material;
    private readonly Texture2d? _albedo;
    private readonly Texture2d? _normal;
    private readonly Texture2d? _roughness;
    private readonly Texture2d? _metallic;
    private readonly GpuBufferView _dataBuffer;
    private readonly BindGroup _bindGroup;
    private readonly GraphicsContext _ctx;
    private GpuMaterialData _data; // TODO
    private bool _isDirty;


    public static GpuMaterial Get(GraphicsContext context, Material material)
    {
        if (Instances.TryGetValue(material, out var instance1))
        {
            return instance1;
        }
        var instance = new GpuMaterial(context, material);
        Instances.Add(material, instance);
        return instance;
    }

    public GpuMaterial(GraphicsContext context, Material material)
    {
        _ctx = context;
        _material = material;
        _albedo = material.Albedo;
        _normal = material.Normal;
        _roughness = material.Roughness;
        _metallic = material.Metallic;

        var bindGroupDesc = new BindGroupDesc
        {
            Layout = context.BindGroupLayoutStore.Material
        };
        _bindGroup = context.LogicalDevice.CreateBindGroup(bindGroupDesc);
        _dataBuffer = _ctx.UniformBufferArena.Request(Marshal.SizeOf<GpuMaterialData>());
        _isDirty = true;
        Update();
    }

    public void BindTexture(uint binding, Texture? texture)
    {
        if (texture == null)
        {
            _bindGroup.SrvTexture(binding, _ctx.NullTexture.Texture);
        }
        else
        {
            _bindGroup.SrvTexture(binding, texture);
        }
    }

    public BindGroup BindGroup
    {
        get
        {
            Validate();
            Update();
            return _bindGroup;
        }
    }

    private void Validate()
    {
        _isDirty = _material.Albedo != _albedo;
        _isDirty = _isDirty || _material.Normal != _normal;
        _isDirty = _isDirty || _material.Roughness != _roughness;
        _isDirty = _isDirty || _material.Metallic != _metallic;
        _isDirty = true;
    }

    private void Update()
    {
        if (!_isDirty)
        {
            return;
        }

        _isDirty = false;
        var placeHolder = new GpuMaterialData();
        _dataBuffer.Buffer.WriteData(in placeHolder, _dataBuffer.Offset);
        _bindGroup.BeginUpdate();
        var bindBufferDesc = new BindBufferDesc();

        BindTexture(GpuMaterialLayout.Albedo.Binding, _albedo?.GpuTexture);
        BindTexture(GpuMaterialLayout.Normal.Binding, _normal?.GpuTexture);
        BindTexture(GpuMaterialLayout.Roughness.Binding, _roughness?.GpuTexture);
        BindTexture(GpuMaterialLayout.Metallic.Binding, _metallic?.GpuTexture);
        bindBufferDesc.Binding = GpuMaterialLayout.Material.Binding;
        bindBufferDesc.Resource = _dataBuffer.Buffer;
        bindBufferDesc.ResourceOffset = _dataBuffer.Offset;
        _bindGroup.CbvWithDesc(bindBufferDesc);
        _bindGroup.EndUpdate();
    }
}