using DenOfIz;

namespace NiziKit.Graphics.Binding;

public abstract class ShaderBinding<TTarget> : IShaderBinding
{
    public Type TargetType => typeof(TTarget);
    public abstract BindGroupLayout Layout { get; }
    public virtual bool RequiresCycling => false;

    protected int NumBindGroups => RequiresCycling ? (int)GraphicsContext.NumFrames : 1;
    protected BindGroup[] BindGroups { get; }

    public BindGroup BindGroup => RequiresCycling ? BindGroups[GraphicsContext.FrameIndex] : BindGroups[0];

    protected ShaderBinding()
    {
        BindGroups = new BindGroup[NumBindGroups];

        var bindGroupDesc = new BindGroupDesc
        {
            Layout = Layout
        };

        for (var i = 0; i < NumBindGroups; i++)
        {
            BindGroups[i] = GraphicsContext.Device.CreateBindGroup(bindGroupDesc);
        }

        OnCreated();
    }

    protected virtual void OnCreated()
    {
    }

    public void Update(object target)
    {
        OnUpdate((TTarget)target);
    }

    protected abstract void OnUpdate(TTarget target);

    public virtual void Dispose()
    {
        foreach (var bindGroup in BindGroups)
        {
            bindGroup.Dispose();
        }
    }
}
