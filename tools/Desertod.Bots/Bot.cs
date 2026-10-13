namespace Desertod.Bots;

/// <summary>One citizen that keeps working like a casual player: renews when the cycle ends, unloads, helps build.</summary>
internal sealed class Bot(BotOptions options, string slug, string accountName)
{
    private const string CarryToStorage = "carry-to-storage";

    private static readonly string[] Tasks =
    [
        "chop-wood", "quarry-stone", "forage", "cut-reeds", "hunt", "fish", "tend-fire",
        "tell-stories", "study-plants", "roam", "explore-land", "read-sky",
    ];

    private static readonly string[] Names =
    [
        "Bogdan", "Mirslav", "Lada", "Dobrynya", "Zlata", "Radim", "Vesna", "Yaromir", "Milava", "Stoyan",
        "Lyuba", "Gorislav", "Dana", "Vyshata", "Zhdana", "Tverd",
    ];

    /// <summary>Tasks the village or the citizen is not ready for are skipped for a while instead of retried every cycle.</summary>
    private static readonly TimeSpan BlockTime = TimeSpan.FromMinutes(10);

    private readonly Dictionary<string, DateTimeOffset> blocked = [];
    private readonly Random random = new();
    private readonly string path = $"/api/villages/{slug}";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = await BotClient.SignInAsync(options.BaseUri, accountName, cancellationToken);
                await JoinAsync(client, cancellationToken);
                await PlayAsync(client, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
            {
                BotLog.Write(accountName, $"error: {exception.Message} Retrying in 15 s.");
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }
    }

    private async Task JoinAsync(BotClient client, CancellationToken cancellationToken)
    {
        var me = await client.GetAsync<MeView>("/api/me", cancellationToken);
        if (me?.Character is null)
        {
            var gender = random.Next(2) == 0 ? "Male" : "Female";
            var appearance = new
            {
                gender,
                style = random.Next(gender == "Female" ? 2 : 5),
                skinTone = random.Next(6),
                hairColor = random.Next(8),
                outfitColor = random.Next(8),
            };
            var name = $"{Names[random.Next(Names.Length)]} {accountName}";
            var failed = await client.PostAsync("/api/me/character", new { name, appearance }, cancellationToken);
            if (failed is not null and not "character.exists")
            {
                throw new InvalidOperationException($"Character creation failed: {failed}.");
            }
        }

        var joined = await client.PostAsync($"{path}/citizens", new { inviteCode = options.InviteCode }, cancellationToken);
        if (joined is not null and not "citizen.alreadyLives")
        {
            throw new InvalidOperationException($"Joining {slug} failed: {joined}.");
        }

        BotLog.Write(accountName, joined is null ? "joined the village" : "is back in the village");
    }

    private async Task PlayAsync(BotClient client, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var citizen = await client.GetAsync<CitizenView>($"{path}/me", cancellationToken)
                          ?? throw new InvalidOperationException("Not a citizen of the village.");

            if (citizen is { State: "Working", CycleEndsAt: { } endsAt })
            {
                var left = endsAt - citizen.ServerNow + TimeSpan.FromSeconds(random.Next(1, 5));
                await Task.Delay(Clamp(left, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5)), cancellationToken);
                continue;
            }

            var acted = await ActAsync(client, citizen, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(acted ? random.Next(1, 3) : random.Next(20, 40)), cancellationToken);
        }
    }

    /// <returns>False when nothing could be started.</returns>
    private async Task<bool> ActAsync(BotClient client, CitizenView citizen, CancellationToken cancellationToken)
    {
        if (citizen.Load.Count > 0)
        {
            var carried = await client.PostAsync($"{path}/me/task", new { taskId = CarryToStorage }, cancellationToken);
            if (carried is null)
            {
                BotLog.Write(accountName, $"carries {string.Join(", ", citizen.Load.Select(l => $"{l.Amount} {l.Kind}"))} to storage");
                return true;
            }

            await client.PostAsync($"{path}/me/drop", null, cancellationToken);
            BotLog.Write(accountName, $"drops the load ({carried})");
        }

        var village = await client.GetAsync<VillageView>(path, cancellationToken);
        if (village is not null)
        {
            await VoteAsync(client, citizen, village.State.Poll, cancellationToken);

            if (random.NextDouble() < 0.5 && await TryJobAsync(client, village.State.Sites, cancellationToken))
            {
                return true;
            }

            if (village.State.CurrentResearch is { } research && random.NextDouble() < 0.2
                && await TryAsync(client, "research", $"{path}/me/research", null, $"researches {research}", cancellationToken))
            {
                return true;
            }
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var taskId in Tasks.Where(t => !blocked.TryGetValue(t, out var until) || until < now).OrderBy(_ => random.Next()).Take(4))
        {
            if (await TryAsync(client, taskId, $"{path}/me/task", new { taskId }, taskId, cancellationToken))
            {
                return true;
            }
        }

        BotLog.Write(accountName, "rests: nothing could be started");
        return false;
    }

    private async Task<bool> TryJobAsync(BotClient client, IReadOnlyList<SiteView> sites, CancellationToken cancellationToken)
    {
        var jobs = sites
            .Where(site => site.Stage >= 0 && site.Stage < site.Stages.Count)
            .SelectMany(site => site.Stages[site.Stage].Jobs
                .Where(job => job.Room > 0)
                .Select(job => (Site: site, Job: job)))
            .OrderBy(_ => random.Next())
            .Take(2);

        foreach (var (site, job) in jobs)
        {
            var body = new { plotId = site.PlotId, stage = site.Stage, jobId = job.JobId };
            if (await TryAsync(client, job.JobId, $"{path}/me/job", body, $"{job.JobId} at the {site.Kind} site", cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> TryAsync(BotClient client, string key, string endpoint, object? body, string description, CancellationToken cancellationToken)
    {
        var failed = await client.PostAsync(endpoint, body, cancellationToken);
        if (failed is null)
        {
            BotLog.Write(accountName, description);
            return true;
        }

        if (failed.StartsWith("task.", StringComparison.Ordinal) && failed != "task.cycleRunning")
        {
            blocked[key] = DateTimeOffset.UtcNow + BlockTime;
        }

        return false;
    }

    private async Task VoteAsync(BotClient client, CitizenView citizen, PollView? poll, CancellationToken cancellationToken)
    {
        if (poll is not { ClosedAt: null, Options.Count: > 0 } || poll.Ballots.Any(b => b.CitizenId == citizen.Id))
        {
            return;
        }

        var technologyId = poll.Options[random.Next(poll.Options.Count)].TechnologyId;
        if (await client.PostAsync($"{path}/me/vote", new { technologyId }, cancellationToken) is null)
        {
            BotLog.Write(accountName, $"votes for {technologyId}");
        }
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan minimum, TimeSpan maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
}
