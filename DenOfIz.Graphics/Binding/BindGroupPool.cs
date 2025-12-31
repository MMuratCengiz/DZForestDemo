using DenOfIz;

namespace Graphics.Binding;

public class BindGroupPool : IDisposable
{
    private readonly LogicalDevice _device;
    private readonly RootSignature _rootSignature;
    private readonly uint _numFrames;
    private readonly List<ResourceBindGroup>[] _perFrameBindGroups;
    private readonly int[] _perFrameNextIndex;

    public BindGroupPool(LogicalDevice device, RootSignature rootSignature, uint numFrames)
    {
        _device = device;
        _rootSignature = rootSignature;
        _numFrames = numFrames;
        _perFrameBindGroups = new List<ResourceBindGroup>[numFrames];
        _perFrameNextIndex = new int[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _perFrameBindGroups[i] = [];
        }
    }

    public void BeginFrame(uint frameIndex)
    {
        _perFrameNextIndex[frameIndex] = 0;
    }

    public ResourceBindGroup Get(uint frameIndex, uint registerSpace = 0)
    {
        var list = _perFrameBindGroups[frameIndex];
        var index = _perFrameNextIndex[frameIndex]++;

        if (index < list.Count)
        {
            return list[index];
        }

        var bindGroup = _device.CreateResourceBindGroup(new ResourceBindGroupDesc
        {
            RootSignature = _rootSignature,
            RegisterSpace = registerSpace
        });
        list.Add(bindGroup);
        return bindGroup;
    }

    public void Dispose()
    {
        for (var i = 0; i < _numFrames; i++)
        {
            foreach (var bindGroup in _perFrameBindGroups[i])
            {
                bindGroup.Dispose();
            }
            _perFrameBindGroups[i].Clear();
        }
    }
}
