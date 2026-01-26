using DenOfIz;
using NiziKit.Graphics.Binding.Layout;

namespace NiziKit.Graphics.RootSignatures;

public class BindGroupLayoutStore(LogicalDevice device) : IDisposable
{
    private readonly GpuCameraLayout _cameraLayout = new(device);
    private readonly GpuSurfaceLayout _surfaceLayout = new(device);
    private readonly GpuDrawLayout _drawLayout = new(device);

    public BindGroupLayout Camera => _cameraLayout.Layout;
    public BindGroupLayout Surface => _surfaceLayout.Layout;
    public BindGroupLayout Draw => _drawLayout.Layout;

    public void Dispose()
    {
        _cameraLayout.Dispose();
        _surfaceLayout.Dispose();
        _drawLayout.Dispose();
    }
}
