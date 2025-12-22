using DenOfIz;
using Graphics.Binding;

namespace Graphics.RenderGraph;

public struct DrawState
{
    Shader Shader;
    Dictionary<string, SrvUavData> SrvBindings;
    Dictionary<string, GPUBufferView> UavBindings;
}