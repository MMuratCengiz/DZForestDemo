using DenOfIz;
using DenOfIz.Tasks;

namespace Graphics.RenderGraph;

public class RenderGraphV2
{
    private struct FrameContext(Fence fence, uint index)
    {
        public Fence Fence = fence;
        public uint Index = index;
        public TaskGraph Graph = new(); 
        
    }
    
    private TaskExecutor _executor = new();
    private readonly GraphicsResource _resource;

    public RenderGraphV2(GraphicsResource resource, int numThreads = 0)
    {
        _resource = resource;
    }
}