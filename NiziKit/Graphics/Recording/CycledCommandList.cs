using DenOfIz;

namespace NiziKit.Graphics.Recording;

public class CycledCommandList : IDisposable
{
    private readonly CommandListPool _commandListPool;
    private readonly CommandList[]  _commandLists;
    
    public CycledCommandList(QueueType queueType)
    {
        var poolDesc = new CommandListPoolDesc();
        poolDesc.NumCommandLists = GraphicsContext.NumFrames;
        switch (queueType)
        {
            case QueueType.Graphics:
                poolDesc.CommandQueue = GraphicsContext.GraphicsCommandQueue;
                break;
            case QueueType.Compute:
                poolDesc.CommandQueue = GraphicsContext.ComputeCommandQueue;
                break;
            case QueueType.Copy:
                poolDesc.CommandQueue = GraphicsContext.CopyCommandQueue;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(queueType), queueType, null);
        }
        
        _commandListPool = GraphicsContext.Device.CreateCommandListPool(poolDesc);
        _commandLists = _commandListPool.GetCommandLists().ToArray();
    }
    
    public CommandList this[int index] => _commandLists[index];

    public void Dispose()
    {
        _commandListPool.Dispose();
    }
}