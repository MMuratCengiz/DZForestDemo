namespace NiziKit.Core;

public static class Disposer
{
    private static readonly Stack<IDisposable> _disposables = new();

    public static void Register(IDisposable disposable)
    {
        _disposables.Push(disposable);
    }

    public static void DisposeAll()
    {
        while (_disposables.Count > 0)
        {
            _disposables.Pop().Dispose();
        }
    }
}
