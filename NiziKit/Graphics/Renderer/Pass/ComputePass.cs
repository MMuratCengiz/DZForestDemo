using DenOfIz;

namespace NiziKit.Graphics.Renderer.Pass;

public class ComputePass(CommandList commandList) : RenderPass(commandList)
{
    public override void Reset()
    {
    }

    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ)
    {
        _commandList.Dispatch(groupCountX, groupCountY, groupCountZ);
    }

    public void DispatchIndirect(DenOfIz.Buffer buffer, ulong offset)
    {
        _commandList.DispatchIndirect(buffer, offset);
    }
}
