using DenOfIz;

namespace Graphics.RenderGraph;

public class RGCommandList
{
    private CommandListPool _commandListPool;
    private List<Fence> _fences;
    private List<CommandList> _commandList;

    public RGCommandList(LogicalDevice logicalDevice, CommandQueue commandQueue)
    {
        CommandListPoolDesc commandListPoolDesc = new()
        {
            CommandQueue = commandQueue,
            NumCommandLists = 3
        };

        _commandListPool = logicalDevice.CreateCommandListPool(commandListPoolDesc);
        _commandList = new List<CommandList>(_commandListPool.GetCommandLists().ToArray()!);
        _fences = [];
        for (var i = 0; i < 3; i++)
        {
            _fences.Add(logicalDevice.CreateFence());
        }
    }

    public void SetShader(Shader shader)
    {
        
    }
}