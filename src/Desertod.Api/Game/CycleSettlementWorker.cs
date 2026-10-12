namespace Desertod.Api.Game;

/// <summary>
/// Banks finished cycles even when the player is not looking, so the overlay and village storage stay current.
/// </summary>
public sealed class CycleSettlementWorker(GameService game, TimeProvider time, ILogger<CycleSettlementWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MoraleDriftInterval = TimeSpan.FromMinutes(1);
    private const double MoraleDriftRate = 0.02;
    private const int BatchSize = 200;

    /// <summary>Villages settled at the same time; each takes a database connection.</summary>
    private const int ParallelVillages = 4;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextDrift = time.GetUtcNow() + MoraleDriftInterval;
        using var timer = new PeriodicTimer(PollInterval, time);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var villages = (await game.FindDueCitizensAsync(BatchSize, stoppingToken))
                    .GroupBy(due => due.VillageId, due => due.CitizenId);
                await Parallel.ForEachAsync(
                    villages,
                    new ParallelOptions { MaxDegreeOfParallelism = ParallelVillages, CancellationToken = stoppingToken },
                    async (village, cancellationToken) =>
                    {
                        try
                        {
                            await game.SettleVillageAsync(village.Key, [.. village], cancellationToken);
                        }
                        catch (Exception exception) when (exception is not OperationCanceledException)
                        {
                            logger.LogError(exception, "Cycle settlement failed in village {VillageId}.", village.Key);
                        }
                    });

                if (time.GetUtcNow() >= nextDrift)
                {
                    await game.DriftMoraleAsync(MoraleDriftRate, stoppingToken);
                    nextDrift = time.GetUtcNow() + MoraleDriftInterval;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Cycle settlement failed.");
            }
        }
    }
}
