using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace Desertod.LoadTests;

/// <summary>A signed-in account with its own cookies, optionally a citizen of one village.</summary>
public sealed class Player : IAsyncDisposable
{
    private static readonly string[] Events = ["villageEvents"];

    private readonly CookieContainer cookies;
    private long pendingActionTimestamp;

    private Player(HttpClient http, CookieContainer cookies, string slug)
    {
        Http = http;
        this.cookies = cookies;
        Slug = slug;
    }

    public HttpClient Http { get; }
    public string Slug { get; }
    public HubConnection? Hub { get; private set; }

    public static async Task<Player> SignInAsync(Uri baseUri, string accountName, string slug)
    {
        var cookies = new CookieContainer();
        var handler = new SocketsHttpHandler { CookieContainer = cookies, UseCookies = true };
        var http = new HttpClient(handler) { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(30) };
        var login = await http.PostAsJsonAsync("/auth/dev-login", new { name = accountName });
        login.EnsureSuccessStatusCode();
        return new Player(http, cookies, slug);
    }

    public async Task JoinAsync(string citizenName)
    {
        var me = await Http.GetFromJsonAsync<JsonElement>("/api/me");
        if (me.GetProperty("character").ValueKind == JsonValueKind.Null)
        {
            var appearance = new { gender = "Male", style = 0, skinTone = 0, hairColor = 0, outfitColor = 0 };
            var created = await Http.PostAsJsonAsync("/api/me/character", new { name = citizenName, appearance });
            created.EnsureSuccessStatusCode();
        }

        var joined = await Http.PostAsJsonAsync($"/api/villages/{Slug}/citizens", new { });
        joined.EnsureSuccessStatusCode();
    }

    /// <summary>Watches the village like the game page does; reports when a state broadcast follows this player's action.</summary>
    public async Task WatchAsync(Uri baseUri, BroadcastStats stats)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(baseUri, "/hubs/village"), options => options.Cookies = cookies)
            .Build();
        connection.On<JsonElement>("villageUpdated", state =>
        {
            stats.CountVillageUpdate(state.GetRawText().Length);
            var started = Interlocked.Exchange(ref pendingActionTimestamp, 0);
            if (started != 0)
            {
                stats.ActionToBroadcast.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
        });
        foreach (var name in Events)
        {
            connection.On<JsonElement>(name, _ => stats.CountOtherEvent());
        }

        await connection.StartAsync();
        await connection.InvokeAsync("JoinVillage", Slug);
        Hub = connection;
    }

    public void MarkAction() => Interlocked.Exchange(ref pendingActionTimestamp, Stopwatch.GetTimestamp());

    public async ValueTask DisposeAsync()
    {
        if (Hub is not null)
        {
            await Hub.DisposeAsync();
        }

        Http.Dispose();
    }
}