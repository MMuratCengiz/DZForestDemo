namespace DenOfIz.World.Graphics.RootSignatures;

public class ForwardRootSignature(BindGroupLayoutStore layoutStore, LogicalDevice device) : IDisposable
{
    public RootSignature StaticRootSignature { get; } = device.CreateRootSignature(new RootSignatureDesc
    {
        BindGroupLayouts = BindGroupLayoutArray.Create([
            layoutStore.Camera,
            layoutStore.Material,
            layoutStore.Draw
        ])
    });

    public RootSignature SkinnedRootSignature { get; } = device.CreateRootSignature(new RootSignatureDesc
    {
        BindGroupLayouts = BindGroupLayoutArray.Create([
            layoutStore.Camera,
            layoutStore.Material,
            layoutStore.SkinnedDraw
        ])
    });

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        StaticRootSignature.Dispose();
        SkinnedRootSignature.Dispose();
    }
}