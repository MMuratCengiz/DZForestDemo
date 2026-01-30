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

    public void AlphaBlit(CycledTexture source, CycledTexture dest)
    {
        var blitPass = GetOrCreateBlitPass(_blitPassIndex);
        _blitPassIndex++;

        var pass = AllocateBlitPass();
        pass.CommandList.Begin();
        blitPass.Execute(pass.CommandList, source, dest);
        pass.CommandList.End();
    }

    public void AlphaBlit(Texture source, CycledTexture dest)
    {
        var blitPass = GetOrCreateBlitPass(_blitPassIndex);
        _blitPassIndex++;

        var pass = AllocateBlitPass();
        pass.CommandList.Begin();
        blitPass.Execute(pass.CommandList, source, dest);
        pass.CommandList.End();
    }

    private BlitPass GetOrCreateBlitPass(int index)
    {
        _blitPasses[index] ??= new BlitPass();
        return _blitPasses[index];
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
