using DenOfIz;

namespace NiziKit.Graphics.Binding.Layout;

public interface ILayout : IDisposable
{
    public BindGroupLayout Layout { get; }
}