using System.Collections.Concurrent;

namespace Desertod.Api.Game;

/// <summary>
/// One writer per village at a time. Settlements and most actions rewrite the village row, so without this they fail
/// each other's concurrency check and redo all their work. Works within one process only: a second server instance
/// would need a database lock instead.
/// </summary>
public sealed class VillageLocks
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> locks = new();

    public async Task<IDisposable> AcquireAsync(Guid villageId, CancellationToken cancellationToken)
    {
        var semaphore = locks.GetOrAdd(villageId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        private int released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref released, 1) == 0)
            {
                semaphore.Release();
            }
        }
    }
}
