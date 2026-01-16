using DenOfIz;

namespace NiziKit.Graphics.Renderer.Pass;

public class RaytracingPass : RenderPass
{
    public RaytracingPass(CommandList commandList) : base(commandList)
    {
    }

    public override void Reset()
    {
    }

    public void BuildBottomLevelAS(in BuildBottomLevelASDesc desc)
    {
        _commandList.BuildBottomLevelAS(in desc);
    }

    public void BuildTopLevelAS(in BuildTopLevelASDesc desc)
    {
        _commandList.BuildTopLevelAS(in desc);
    }

    public void UpdateTopLevelAS(in UpdateTopLevelASDesc desc)
    {
        _commandList.UpdateTopLevelAS(in desc);
    }

    public void DispatchRays(in DispatchRaysDesc desc)
    {
        _commandList.DispatchRays(in desc);
    }
}
