using DenOfIz;

namespace Graphics.Binding;

public class BindGroupPool(LogicalDevice device, RootSignature rootSignature) : IDisposable
{
    private readonly List<List<ResourceBindGroup>> _spaceBindGroups = [];

    public ResourceBindGroup NewBindGroup(uint registerSpace)
    {
        while (_spaceBindGroups.Count <= registerSpace)
        {
            _spaceBindGroups.Add([]);
        }

        ResourceBindGroupDesc desc = new();
        desc.RootSignature = rootSignature;
        desc.RegisterSpace = registerSpace;
        var result = device.CreateResourceBindGroup(desc);
        _spaceBindGroups[(int)registerSpace].Add(result);
        return result;
    }

    public void Dispose()
    {
        foreach (var bindGroups in _spaceBindGroups)
        {
            foreach (var bindGroup in bindGroups)
            {
                bindGroup.Dispose();
            }
        }
    }
}