using DenOfIz;

namespace NiziKit.Graphics.RootSignatures;

public class ForwardRootSignature(BindGroupLayoutStore layoutStore, LogicalDevice device) : IDisposable
{
    public RootSignature Instance { get; } = device.CreateRootSignature(new RootSignatureDesc
    {
        BindGroupLayouts = BindGroupLayoutArray.Create([
            layoutStore.Camera,
            layoutStore.Material,
            layoutStore.Draw
        ])
    });

    public void Dispose()
    {
        Instance.Dispose();
    }
}
