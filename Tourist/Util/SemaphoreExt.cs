using System.Threading;

namespace Tourist.Util;

internal static class SemaphoreExt
{
    internal static OnDispose With(this SemaphoreSlim semaphore)
    {
        semaphore.Wait();
        return new OnDispose(() => semaphore.Release());
    }
}
