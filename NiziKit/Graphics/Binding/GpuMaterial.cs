using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DenOfIz;
using NiziKit.Assets;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.Binding;

public class GpuMaterial
{
    private static readonly ConcurrentDictionary<Material, GpuMaterial> Instances = new();
    private static readonly object Lock = new();
    
    private readonly Material _material;
    private readonly Texture2d? _albedo;
    private readonly Texture2d? _normal;
    private readonly Texture2d? _roughness;
    private readonly Texture2d? _metallic;
    private readonly GpuBufferView _dataBuffer;
    private readonly BindGroup _bindGroup;
    private readonly Sampler _sampler;
    private readonly object _updateLock = new();
    private GpuMaterialData _data; // TODO
    private bool _isDirty;


    public static GpuMaterial Get(Material material)
    {
        if (Instances.TryGetValue(material, out var existing))
        {
            return existing;
        }

        lock (Lock)
        {
            if (Instances.TryGetValue(material, out existing))
            {
                return existing;
            }

            var instance = new GpuMaterial(material);
            Instances.TryAdd(material, instance);
            return instance;
        }
    }

    public GpuMaterial(Material material)
    {
        _material = material;
        _albedo = material.Albedo;
        _normal = material.Normal;
        _roughness = material.Roughness;
        _metallic = material.Metallic;

        var bindGroupDesc = new BindGroupDesc
        {
            Layout = GraphicsContext.BindGroupLayoutStore.Material
        };
        _bindGroup = GraphicsContext.Device.CreateBindGroup(bindGroupDesc);
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
        _isDirty = true;
        Update();
    }

    private void BindTexture(uint binding, Texture? texture)
    {
        _bindGroup.SrvTexture(binding, texture ?? GraphicsContext.MissingTexture.Texture);
    }

    public BindGroup BindGroup
    {
        get
        {
            lock (_updateLock)
            {
                Validate();
                Update();
                return _bindGroup;
            }
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

        BindTexture(GpuMaterialLayout.Albedo.Binding, _albedo?.GpuTexture);
        BindTexture(GpuMaterialLayout.Normal.Binding, _normal?.GpuTexture);
        BindTexture(GpuMaterialLayout.Roughness.Binding, _roughness?.GpuTexture);
        BindTexture(GpuMaterialLayout.Metallic.Binding, _metallic?.GpuTexture);
        _bindGroup.Sampler(GpuMaterialLayout.TextureSampler.Binding, _sampler);
        var bindBufferDesc = new BindBufferDesc
        {
            Binding = GpuMaterialLayout.Constants.Binding,
            Resource = _dataBuffer.Buffer,
            ResourceOffset = _dataBuffer.Offset
        };
        _bindGroup.CbvWithDesc(bindBufferDesc);
        _bindGroup.EndUpdate();
    }
}