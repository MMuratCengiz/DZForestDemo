using DenOfIz;

namespace NiziKit.Graphics.RootSignatures;

public class RootSignatureStore(LogicalDevice device, BindGroupLayoutStore layoutStore)
{
    public ForwardRootSignature Forward { get; } = new(layoutStore, device);
}