namespace Graphics.RenderGraph;

public abstract class RenderPassV2
{
    public abstract string Name { get; }

    public virtual ReadOnlySpan<string> Reads => [];
    public virtual ReadOnlySpan<string> Writes => [];

    public abstract void Execute(ref RenderPassContext ctx);
}
