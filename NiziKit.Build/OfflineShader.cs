using System.Text;
using DenOfIz;
using NiziKit.Offline;

namespace NiziKit.Build;

public abstract class OfflineShader(string shaderSourceDir)
{
    public abstract string Name { get; }

    public ShaderProgramDesc ProgramDesc() =>
        new()
        {
            ShaderStages = ShaderStageDescArray.Create(Stages().ToArray())
        };

    protected StringView ShaderPath(string rel) => StringView.Create(Path.Combine(shaderSourceDir, rel));
    protected abstract List<ShaderStageDesc> Stages();
}