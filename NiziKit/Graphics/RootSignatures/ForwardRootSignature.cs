using DenOfIz;

namespace NiziKit.Graphics.RootSignatures;

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

    public void Dispose()
    {
        StaticRootSignature.Dispose();
        SkinnedRootSignature.Dispose();
    }
}