using System.Runtime.CompilerServices;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Graphics.Renderer.Common;
using NiziKit.Graphics.Resources;
using NiziKit.UI;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.Graphics.Renderer;

public delegate void UiBuildCallback(UiFrame frame);

public partial class RenderFrame
{
    private UiContext? _uiContext;
    private AlphaBlitPass? _uiBlitPass;
    private readonly Semaphore[] _externalSemaphores = new Semaphore[4];
    private int _externalSemaphoreCount;

    public UiContext UiContext => _uiContext ?? throw new InvalidOperationException("UI not enabled. Call EnableUi first.");

    public void EnableUi(UiContextDesc desc)
    {
        _uiContext?.Dispose();
        _uiContext = new UiContext(desc);
        _uiBlitPass ??= new AlphaBlitPass();
    }

    public void EnableUi(UiContext context)
    {
        _uiContext = context;
        _uiBlitPass ??= new AlphaBlitPass();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void HandleUiEvent(Event ev)
    {
        _uiContext?.HandleEvent(ev);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUiViewportSize(uint width, uint height)
    {
        _uiContext?.SetViewportSize(width, height);
    }

    public void ExecuteUi(UiBuildCallback buildCallback, CycledTexture dest)
    {
        if (_uiContext == null)
        {
            throw new InvalidOperationException("UI not enabled. Call EnableUi first.");
        }

        _uiContext.UpdateScroll(Time.DeltaTime);
        var frame = _uiContext.BeginFrame();
        buildCallback(frame);
        var (texture, semaphore) = frame.End((uint)_currentFrame, Time.DeltaTime);

        if (_externalSemaphoreCount < _externalSemaphores.Length)
        {
            _externalSemaphores[_externalSemaphoreCount++] = semaphore;
        }

        var pass = AllocateBlitPass();
        pass.CommandList.Begin();
        _uiBlitPass!.Execute(pass.CommandList, texture, dest);
        pass.CommandList.End();
    }

    private void ResetUi()
    {
        _externalSemaphoreCount = 0;
    }

    private void DisposeUi()
    {
        _uiBlitPass?.Dispose();
        _uiBlitPass = null;
        _uiContext?.Dispose();
        _uiContext = null;
    }
}
