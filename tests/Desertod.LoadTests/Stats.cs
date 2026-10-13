using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace Desertod.LoadTests;

/// <summary>What the watchers of one phase received.</summary>
public sealed class BroadcastStats
{
    private long villageUpdates;
    private long villageUpdateBytes;
    private long otherEvents;

    /// <summary>Milliseconds from a successful task start to the next village state the same player received.</summary>
    public ConcurrentBag<double> ActionToBroadcast { get; } = [];

    public ConcurrentBag<double> Joins { get; } = [];

    public void CountVillageUpdate(int bytes)
    {
        Interlocked.Increment(ref villageUpdates);
        Interlocked.Add(ref villageUpdateBytes, bytes);
    }

    public void CountOtherEvent() => Interlocked.Increment(ref otherEvents);

    public string Summary(TimeSpan duration, int connections)
    {
        var updates = Interlocked.Read(ref villageUpdates);
        var bytes = Interlocked.Read(ref villageUpdateBytes);
        var seconds = Math.Max(1, duration.TotalSeconds);
        return string.Join(Environment.NewLine,
            $"  joins (sign-in excluded):        {Percentiles(Joins)}",
            $"  task start -> next state:        {Percentiles(ActionToBroadcast)}",
            $"  villageUpdated received:         {updates} total, {updates / seconds:F1}/s over {connections} connections, {updates / seconds / Math.Max(1, connections):F2}/s per connection",
            $"  villageUpdated average size:     {(updates == 0 ? 0 : bytes / updates / 1024.0):F1} KiB, {bytes / seconds / 1024 / 1024:F2} MiB/s received in total",
            $"  other events received:           {Interlocked.Read(ref otherEvents)}");
    }

    public static string Percentiles(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0)
        {
            return "no samples";
        }

        double At(double share) => sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(share * sorted.Length) - 1)];
        return string.Create(CultureInfo.InvariantCulture,
            $"n={sorted.Length}, p50={At(0.5):F0} ms, p95={At(0.95):F0} ms, p99={At(0.99):F0} ms, max={sorted[^1]:F0} ms");
    }
}

/// <summary>Samples the processor and memory use of the API process and the database container.</summary>
public sealed class ResourceSampler : IAsyncDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly List<(double Cpu, double MemoryMiB)> api = [];
    private readonly List<string> database = [];
    private readonly Task loop;

    public ResourceSampler(int? apiProcessId, string? databaseContainer)
    {
        loop = Task.Run(() => RunAsync(apiProcessId, databaseContainer, stop.Token));
    }

    private async Task RunAsync(int? apiProcessId, string? databaseContainer, CancellationToken cancellationToken)
    {
        using var process = apiProcessId is { } id ? Process.GetProcessById(id) : null;
        var lastCpu = process?.TotalProcessorTime ?? TimeSpan.Zero;
        var lastWall = Stopwatch.GetTimestamp();
        var tick = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (process is not null)
            {
                process.Refresh();
                var cpu = process.TotalProcessorTime;
                var wall = Stopwatch.GetElapsedTime(lastWall);
                lock (api)
                {
                    api.Add(((cpu - lastCpu).TotalMilliseconds / wall.TotalMilliseconds / Environment.ProcessorCount * 100,
                        process.WorkingSet64 / 1024.0 / 1024.0));
                }

                lastCpu = cpu;
                lastWall = Stopwatch.GetTimestamp();
            }

            if (databaseContainer is not null && tick++ % 5 == 0)
            {
                var line = await DockerStatsAsync(databaseContainer);
                if (line is not null)
                {
                    lock (database)
                    {
                        database.Add(line);
                    }
                }
            }
        }
    }

    private static async Task<string?> DockerStatsAsync(string container)
    {
        var start = new ProcessStartInfo("docker", $"stats {container} --no-stream --format \"{{{{.CPUPerc}}}} {{{{.MemUsage}}}}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        using var docker = Process.Start(start);
        if (docker is null)
        {
            return null;
        }

        var output = await docker.StandardOutput.ReadToEndAsync();
        await docker.WaitForExitAsync();
        return output.Trim();
    }

    public string Summary()
    {
        var lines = new List<string>();
        lock (api)
        {
            if (api.Count > 0)
            {
                lines.Add(string.Create(CultureInfo.InvariantCulture,
                    $"  API process: CPU average {api.Average(s => s.Cpu):F1}% / peak {api.Max(s => s.Cpu):F1}% of all {Environment.ProcessorCount} cores, memory peak {api.Max(s => s.MemoryMiB):F0} MiB"));
            }
        }

        lock (database)
        {
            if (database.Count > 0)
            {
                lines.Add($"  Postgres container (CPU% of one core, memory): {string.Join(" | ", database)}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async ValueTask DisposeAsync()
    {
        await stop.CancelAsync();
        await loop;
        stop.Dispose();
    }
}
