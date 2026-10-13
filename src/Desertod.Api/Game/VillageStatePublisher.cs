using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Desertod.Api.Game;

/// <summary>Villages whose state changed since it was last sent to the viewers.</summary>
public sealed class VillageStateChanges
{
    private readonly ConcurrentDictionary<Guid, byte> changed = new();
    private readonly Channel<bool> signal = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Mark(Guid villageId)
    {
        changed[villageId] = 0;
        Wake();
    }

    /// <summary>Wakes the publisher without a state change, for queued events.</summary>
    public void Wake() => signal.Writer.TryWrite(true);

    /// <summary>Waits until some village changed or events were queued, then takes all changed villages.</summary>
    public async Task<IReadOnlyList<Guid>> TakeAsync(CancellationToken cancellationToken)
    {
        await signal.Reader.ReadAsync(cancellationToken);
        var taken = new List<Guid>();
        foreach (var villageId in changed.Keys)
        {
            if (changed.TryRemove(villageId, out _))
            {
                taken.Add(villageId);
            }
        }

        return taken;
    }
}

/// <summary>
/// Sends the queued events and the state of each changed village at most once per <see cref="Interval"/>,
/// so a burst of actions in one village costs one state build and two broadcasts.
/// </summary>
public sealed class VillageStatePublisher(
    VillageStateChanges changes,
    GameService game,
    VillageNotifier notifier,
    TimeProvider time,
    ILogger<VillageStatePublisher> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

    /// <summary>Villages built at the same time; each takes a database connection.</summary>
    private const int ParallelVillages = 4;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var villageIds = await changes.TakeAsync(stoppingToken);
            var started = time.GetTimestamp();
            var pendingEvents = notifier.TakeEvents();
            foreach (var (slug, events) in pendingEvents)
            {
                try
                {
                    await notifier.VillageEvents(slug, events, stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "Sending the events of village {Slug} failed.", slug);
                }
            }

            await Parallel.ForEachAsync(
                villageIds,
                new ParallelOptions { MaxDegreeOfParallelism = ParallelVillages, CancellationToken = stoppingToken },
                async (villageId, cancellationToken) =>
                {
                    try
                    {
                        if (await game.GetVillageStateAsync(villageId, cancellationToken) is { } state)
                        {
                            await notifier.VillageUpdated(state);
                        }
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        logger.LogError(exception, "Sending the state of village {VillageId} failed.", villageId);
                    }
                });

            if (pendingEvents.Count > 0 || villageIds.Count > 0)
            {
                logger.Published(pendingEvents.Count, villageIds.Count, (long)time.GetElapsedTime(started).TotalMilliseconds);
            }

            await Task.Delay(Interval, time, stoppingToken);
        }
    }
}
