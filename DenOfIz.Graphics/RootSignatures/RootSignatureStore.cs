using DenOfIz;

namespace Graphics.RootSignatures;

public class RootSignatureStore
{
    public ForwardRootSignature Forward { get; }

    public RootSignatureStore(LogicalDevice device, BindGroupLayoutStore layoutStore)
    {
        Forward = new ForwardRootSignature(layoutStore, device);
    }
}