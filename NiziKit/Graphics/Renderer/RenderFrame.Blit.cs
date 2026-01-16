using DenOfIz;
using NiziKit.Graphics.Renderer.Common;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer;

public partial class RenderFrame
{
    private const int MaxBlitPassesPerFrame = 8;

    private readonly BlitPass[] _blitPasses = new BlitPass[MaxBlitPassesPerFrame];
    private readonly CycledTexture?[] _blitDestinations = new CycledTexture?[MaxBlitPassesPerFrame];
    private int _blitPassIndex;

    public CycledTexture Blit(CycledTexture source)
    {
        var dest = GetOrCreateBlitDestination(_blitPassIndex, source.Format, source.Width, source.Height);
        var blitPass = GetOrCreateBlitPass(_blitPassIndex);
        _blitPassIndex++;

        var pass = AllocateBlitPass();
        pass.CommandList.Begin();
        blitPass.Execute(pass.CommandList, source, dest);
        pass.CommandList.End();

        return dest;
    }

    public CycledTexture Blit(CycledTexture source, CycledTexture dest)
    {
        var blitPass = GetOrCreateBlitPass(_blitPassIndex);
        _blitPassIndex++;

        var pass = AllocateBlitPass();
        pass.CommandList.Begin();
        blitPass.Execute(pass.CommandList, source, dest);
        pass.CommandList.End();

        return dest;
    }

    private BlitPass GetOrCreateBlitPass(int index)
    {
        _blitPasses[index] ??= new BlitPass();
        return _blitPasses[index];
    }

    private CycledTexture GetOrCreateBlitDestination(int index, Format format, uint width, uint height)
    {
        var existing = _blitDestinations[index];
        if (existing != null && existing.Format == format && existing.Width == width && existing.Height == height)
        {
            return existing;
        }

        existing?.Dispose();
        _blitDestinations[index] = CycledTexture.ColorAttachment($"BlitDest_{index}", (int)width, (int)height, format);
        return _blitDestinations[index]!;
    }

    private void ResetBlitPassIndex()
    {
        _blitPassIndex = 0;
    }

    private void DisposeBlitResources()
    {
        for (var i = 0; i < MaxBlitPassesPerFrame; i++)
        {
            _blitPasses[i]?.Dispose();
            _blitDestinations[i]?.Dispose();
        }
    }
}
