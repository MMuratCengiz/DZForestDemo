using DenOfIz;

namespace NiziKit.Build;

public abstract class OfflineShader(string shaderSourceDir)
{
    public abstract string Name { get; }

    public ShaderProgramDesc ProgramDesc(Dictionary<string, string?>? defines = null)
    {
        var stages = Stages();

        if (defines is { Count: > 0 })
        {
            var definesArray = CreateDefinesArray(defines);
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                stage.Defines = definesArray;
                stages[i] = stage;
            }
        }

        return new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create(stages.ToArray())
        };
    }

    private static StringViewArray CreateDefinesArray(Dictionary<string, string?> defines)
    {
        var defineStrings = new List<StringView>();
        foreach (var (key, value) in defines)
        {
            var defineStr = value != null ? $"{key}={value}" : $"{key}=1";
            defineStrings.Add(StringView.Create(defineStr));
        }
        return StringViewArray.Create(defineStrings.ToArray());
    }

    protected StringView ShaderPath(string rel) => StringView.Create(Path.Combine(shaderSourceDir, rel));
    protected abstract List<ShaderStageDesc> Stages();
}