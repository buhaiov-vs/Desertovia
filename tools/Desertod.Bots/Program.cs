using System.Net.Http.Json;
using Desertod.Bots;

// Usage: dotnet run --project tools/Desertod.Bots -- --village <slug> [--count 5] [--invite CODE] [--base http://localhost:5080] [--prefix bot]
// The API must run in Development: bots sign in through /auth/dev-login.
var options = BotOptions.Parse(args);
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stop.Cancel();
};

var slug = options.Village ?? await PickPublicVillageAsync(options, stop.Token);
if (slug is null)
{
    BotLog.Write("setup", "No --village given and no open village found. Found one in the game first.");
    return 1;
}

BotLog.Write("setup", $"{options.Count} bot(s) join {options.BaseUri}v/{slug}. Ctrl+C stops them.");
var bots = Enumerable.Range(1, options.Count).Select(async index =>
{
    await Task.Delay(TimeSpan.FromMilliseconds(500 * index), stop.Token);
    var bot = new Bot(options, slug, $"{options.Prefix}{index}");
    await bot.RunAsync(stop.Token);
});

try
{
    await Task.WhenAll(bots);
}
catch (OperationCanceledException)
{
}

BotLog.Write("setup", "Stopped.");
return 0;

static async Task<string?> PickPublicVillageAsync(BotOptions options, CancellationToken cancellationToken)
{
    using var scout = await BotClient.SignInAsync(options.BaseUri, $"{options.Prefix}-scout", cancellationToken);
    var villages = await scout.Http.GetFromJsonAsync<List<PublicVillageView>>("/api/villages/public", BotClient.Json, cancellationToken) ?? [];
    return villages.OrderByDescending(v => v.Citizens).FirstOrDefault()?.Slug;
}

internal sealed record BotOptions(Uri BaseUri, string? Village, string? InviteCode, int Count, string Prefix)
{
    public static BotOptions Parse(string[] args)
    {
        string? Value(string name)
        {
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        return new BotOptions(
            new Uri(Value("--base") ?? "http://localhost:5080/"),
            Value("--village"),
            Value("--invite"),
            int.Parse(Value("--count") ?? "5"),
            Value("--prefix") ?? "bot");
    }
}
