using DenOfIz;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.RootSignatures;

public class BindGroupLayoutStore(LogicalDevice device)
{
    private readonly GpuCameraLayout _cameraLayout = new(device);
    private readonly GpuMaterialLayout _materialLayout = new(device);
    private readonly GpuDrawLayout _drawLayout = new(device, includeBones: false);
    private readonly GpuDrawLayout _skinnedDrawLayout = new(device, includeBones: true);

    public BindGroupLayout Camera => _cameraLayout.Layout;
    public BindGroupLayout Material => _materialLayout.Layout;
    public BindGroupLayout Draw => _drawLayout.Layout;
    public BindGroupLayout SkinnedDraw => _skinnedDrawLayout.Layout;
}