using DenOfIz;

namespace NiziKit.Graphics.Binding;

public interface IShaderBinding : IDisposable
{
    Type TargetType { get; }
    BindGroupLayout Layout { get; }
    bool RequiresCycling { get; }
    void Update(object target);
    BindGroup BindGroup { get; }
}
