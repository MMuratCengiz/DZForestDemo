using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace Graphics.Binding;

public class FrameBindings : IDisposable
{
    private readonly ResourceBindGroup[] _bindGroups;
    private readonly uint _numFrames;
    private bool _isSetUp;

    public FrameBindings(LogicalDevice device, RootSignature rootSignature, uint numFrames, uint registerSpace = 0)
    {
        _numFrames = numFrames;
        _bindGroups = new ResourceBindGroup[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            _bindGroups[i] = device.CreateResourceBindGroup(new ResourceBindGroupDesc
            {
                RootSignature = rootSignature,
                RegisterSpace = registerSpace
            });
        }
    }

    public ResourceBindGroup this[uint frameIndex] => _bindGroups[frameIndex];
    public ResourceBindGroup this[int frameIndex] => _bindGroups[frameIndex];

    public void Setup(Action<ResourceBindGroup, int> configure)
    {
        if (_isSetUp)
        {
            return;
        }

        for (var i = 0; i < _numFrames; i++)
        {
            _bindGroups[i].BeginUpdate();
            configure(_bindGroups[i], i);
            _bindGroups[i].EndUpdate();
        }
        _isSetUp = true;
    }

    public void Setup(Buffer[] perFrameBuffers, uint binding)
    {
        if (_isSetUp)
        {
            return;
        }

        for (var i = 0; i < _numFrames; i++)
        {
            _bindGroups[i].BeginUpdate();
            _bindGroups[i].Cbv(binding, perFrameBuffers[i]);
            _bindGroups[i].EndUpdate();
        }
        _isSetUp = true;
    }

    public void Dispose()
    {
        foreach (var bindGroup in _bindGroups)
        {
            bindGroup.Dispose();
        }
    }
}
