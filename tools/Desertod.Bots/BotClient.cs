using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Desertod.Bots;

/// <summary>A signed-in account with its own cookies.</summary>
public sealed class BotClient : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private BotClient(HttpClient http)
    {
        Http = http;
    }

    public HttpClient Http { get; }

    public static async Task<BotClient> SignInAsync(Uri baseUri, string accountName, CancellationToken cancellationToken)
    {
        var handler = new SocketsHttpHandler { CookieContainer = new CookieContainer(), UseCookies = true };
        var http = new HttpClient(handler) { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(30) };
        using var login = await http.PostAsJsonAsync("/auth/dev-login", new { name = accountName }, cancellationToken);
        if (!login.IsSuccessStatusCode)
        {
            http.Dispose();
            throw new InvalidOperationException(login.StatusCode == HttpStatusCode.NotFound
                ? "Development login is off. Run the API in Development."
                : $"Sign-in failed: {(int)login.StatusCode}.");
        }

        return new BotClient(http);
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await Http.GetAsync(path, cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken) : default;
    }

    /// <returns>Null on success, otherwise the game rule code or the status code.</returns>
    public async Task<string?> PostAsync(string path, object? body, CancellationToken cancellationToken)
    {
        using var response = body is null
            ? await Http.PostAsync(path, null, cancellationToken)
            : await Http.PostAsJsonAsync(path, body, Json, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("code", out var code)
                && code.GetString() is { Length: > 0 } value)
            {
                return value;
            }
        }
        catch (JsonException)
        {
        }

        return ((int)response.StatusCode).ToString();
    }

    public void Dispose() => Http.Dispose();
}

public sealed record PublicVillageView(string Slug, string Name, int Citizens);

public sealed record MeView(CharacterView? Character);

public sealed record CharacterView(string Name);

public sealed record LoadView(string Kind, int Amount);

public sealed record CitizenView(
    Guid Id,
    string State,
    DateTimeOffset? CycleEndsAt,
    DateTimeOffset ServerNow,
    double Energy,
    IReadOnlyList<LoadView> Load);

public sealed record VillageView(VillageStateView State);

public sealed record VillageStateView(IReadOnlyList<SiteView> Sites, string? CurrentResearch, PollView? Poll);

public sealed record SiteView(string Kind, string PlotId, int Stage, IReadOnlyList<StageView> Stages);

public sealed record StageView(IReadOnlyList<JobView> Jobs);

public sealed record JobView(string JobId, int Room);

public sealed record PollView(IReadOnlyList<PollOptionView> Options, IReadOnlyList<BallotView> Ballots, DateTimeOffset? ClosedAt);

public sealed record PollOptionView(string TechnologyId, int Votes);

public sealed record BallotView(Guid CitizenId, string TechnologyId);

public static class BotLog
{
    private static readonly Lock Gate = new();

    public static void Write(string who, string message)
    {
        lock (Gate)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {who,-8} {message}");
        }
    }
}
