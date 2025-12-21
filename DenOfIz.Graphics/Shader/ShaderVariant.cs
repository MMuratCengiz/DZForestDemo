using DenOfIz;

namespace Graphics.Shader;

public struct ShaderVariant(Pipeline pipeline, ShaderProgram program)
{
    public readonly ShaderProgram Program = program;
    public readonly Pipeline Pipeline = pipeline;
}