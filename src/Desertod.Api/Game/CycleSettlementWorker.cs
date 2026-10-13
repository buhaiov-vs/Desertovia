namespace Desertod.Api.Game;

/// <summary>
/// Banks finished cycles even when the player is not looking, so village storage stays current.
/// </summary>
public sealed class CycleSettlementWorker(GameService game, TimeProvider time, ILogger<CycleSettlementWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 200;

    /// <summary>Villages settled at the same time; each takes a database connection.</summary>
    private const int ParallelVillages = 4;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, time);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var due = await game.FindDueCitizensAsync(BatchSize, stoppingToken);
                if (due.Count == 0)
                {
                    continue;
                }

                var villages = due.GroupBy(citizen => citizen.VillageId, citizen => citizen.CitizenId).ToList();
                logger.DueCitizensFound(due.Count, villages.Count);
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
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Cycle settlement failed.");
            }
        }
    }
}
