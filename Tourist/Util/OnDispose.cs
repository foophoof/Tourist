namespace Tourist.Util;

internal class OnDispose : IDisposable
{
    private readonly Action _action;
    private bool _disposed;

    internal OnDispose(Action action)
    {
        _action = action;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _action();
    }
}
