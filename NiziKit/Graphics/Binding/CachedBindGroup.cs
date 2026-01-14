using DenOfIz;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.Binding;

public class CachedBindGroup : IDisposable
{
    private struct CbvEntry
    {
        public Buffer? Buffer;
        public ulong Offset;
    }

    private struct SrvEntry
    {
        public Buffer? Buffer;
        public ulong BufferOffset;
        public Texture? Texture;
    }

    private struct SamplerEntry
    {
        public Sampler? Sampler;
    }

    private readonly BindGroup[] _bindGroups;
    private readonly bool[] _isDirty;

    private readonly CbvEntry[][] _cbvEntries;
    private readonly SrvEntry[][] _srvEntries;
    private readonly SamplerEntry[][] _samplerEntries;

    private readonly int _maxBindings;
    private readonly int _numCopies;
    private int _currentFrame;

    public CachedBindGroup(LogicalDevice device, BindGroupLayout layout, int numCopies, int maxBindings = 16)
    {
        _maxBindings = maxBindings;
        _numCopies = numCopies;
        _bindGroups = new BindGroup[numCopies];
        _isDirty = new bool[numCopies];

        _cbvEntries = new CbvEntry[numCopies][];
        _srvEntries = new SrvEntry[numCopies][];
        _samplerEntries = new SamplerEntry[numCopies][];

        for (var i = 0; i < numCopies; i++)
        {
            _bindGroups[i] = device.CreateBindGroup(new BindGroupDesc
            {
                Layout = layout,
            });
            _isDirty[i] = true;
            _cbvEntries[i] = new CbvEntry[maxBindings];
            _srvEntries[i] = new SrvEntry[maxBindings];
            _samplerEntries[i] = new SamplerEntry[maxBindings];
        }
    }

    public void BeginUpdate(int frameIndex)
    {
        _currentFrame = frameIndex;
    }

    public void Cbv(uint binding, Buffer buffer, ulong offset = 0)
    {
        ref var entry = ref _cbvEntries[_currentFrame][binding];
        if (entry.Buffer != buffer || entry.Offset != offset)
        {
            entry.Buffer = buffer;
            entry.Offset = offset;
            _isDirty[_currentFrame] = true;
        }
    }

    public void SrvBuffer(uint binding, Buffer buffer, ulong offset = 0)
    {
        ref var entry = ref _srvEntries[_currentFrame][binding];
        if (entry.Buffer != buffer || entry.BufferOffset != offset || entry.Texture != null)
        {
            entry.Buffer = buffer;
            entry.BufferOffset = offset;
            entry.Texture = null;
            _isDirty[_currentFrame] = true;
        }
    }

    public void SrvTexture(uint binding, Texture texture)
    {
        ref var entry = ref _srvEntries[_currentFrame][binding];
        if (entry.Texture != texture || entry.Buffer != null)
        {
            entry.Texture = texture;
            entry.Buffer = null;
            entry.BufferOffset = 0;
            _isDirty[_currentFrame] = true;
        }
    }

    public void Sampler(uint binding, Sampler sampler)
    {
        ref var entry = ref _samplerEntries[_currentFrame][binding];
        if (entry.Sampler != sampler)
        {
            entry.Sampler = sampler;
            _isDirty[_currentFrame] = true;
        }
    }

    public BindGroup EndUpdate()
    {
        if (_isDirty[_currentFrame])
        {
            Update(_currentFrame);
        }
        return _bindGroups[_currentFrame];
    }

    private void Update(int frameIndex)
    {
        _isDirty[frameIndex] = false;

        var bindGroup = _bindGroups[frameIndex];
        bindGroup.BeginUpdate();

        for (uint i = 0; i < _maxBindings; i++)
        {
            ref var cbv = ref _cbvEntries[frameIndex][i];
            if (cbv.Buffer != null)
            {
                bindGroup.CbvWithDesc(new BindBufferDesc
                {
                    Binding = i,
                    Resource = cbv.Buffer,
                    ResourceOffset = cbv.Offset
                });
            }

            ref var srv = ref _srvEntries[frameIndex][i];
            if (srv.Texture != null)
            {
                bindGroup.SrvTexture(i, srv.Texture);
            }
            else if (srv.Buffer != null)
            {
                bindGroup.SrvBufferWithDesc(new BindBufferDesc
                {
                    Binding = i,
                    Resource = srv.Buffer,
                    ResourceOffset = srv.BufferOffset
                });
            }

            ref var sampler = ref _samplerEntries[frameIndex][i];
            if (sampler.Sampler != null)
            {
                bindGroup.Sampler(i, sampler.Sampler);
            }
        }

        bindGroup.EndUpdate();
    }

    public void Dispose()
    {
        foreach (var bindGroup in _bindGroups)
        {
            bindGroup.Dispose();
        }
    }
}
