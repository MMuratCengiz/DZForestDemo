using DenOfIz;

namespace NiziKit.Graphics.RootSignatures;

public class RootSignatureStore(LogicalDevice device, BindGroupLayoutStore layoutStore) : IDisposable
{
    public ForwardRootSignature Forward { get; } = new(layoutStore, device);

    public void Dispose()
    {
        Forward.Dispose();
    }
}
