using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Desertod.Api.Game;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Desertod.Api.Tests;

/// <summary>The main path through the game: sign in, found a village, join one, start working.</summary>
[Collection(ApiCollection.Name)]
public sealed class PlayerFlowTests(DesertodApiFactory factory)
{
    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..24];

    private async Task<HttpClient> SignedInClientAsync(string name)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/auth/dev-login", new { name });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        return client;
    }

    [Fact]
    public async Task Development_login_signs_in_and_out()
    {
        var name = UniqueName("login");
        var client = await SignedInClientAsync(name);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.Equal(name, me.GetProperty("displayName").GetString());

        var logout = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Governor_founds_a_village_and_controls_the_stream()
    {
        var client = await SignedInClientAsync(UniqueName("governor"));

        var created = await client.PostAsJsonAsync("/api/villages", new { name = "Smoke Village", description = "Smoke test", isPrivate = true });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var slug = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("slug").GetString()!;

        var mine = await client.GetFromJsonAsync<JsonElement>("/api/villages");
        Assert.Contains(mine.EnumerateArray(), village => village.GetProperty("slug").GetString() == slug);

        var village = await client.GetFromJsonAsync<JsonElement>($"/api/villages/{slug}");
        Assert.True(village.GetProperty("isOwner").GetBoolean());
        Assert.False(string.IsNullOrEmpty(village.GetProperty("inviteCode").GetString()));

        var live = await client.PostAsJsonAsync($"/api/villages/{slug}/live", new { isLive = true });
        Assert.True((await live.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isLive").GetBoolean());

        var invite = await client.PostAsync($"/api/villages/{slug}/invite", null);
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);
    }

    [Fact]
    public async Task Citizen_joins_the_demo_village_and_starts_a_task()
    {
        var client = await SignedInClientAsync(UniqueName("citizen"));
        var path = $"/api/villages/{DemoVillage.Slug}";

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{path}/me")).StatusCode);

        var created = await client.PostAsJsonAsync($"{path}/citizens", new { name = "Smoky" });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var citizen = await client.GetFromJsonAsync<JsonElement>($"{path}/me");
        Assert.Equal("Smoky", citizen.GetProperty("name").GetString());

        var started = await client.PostAsJsonAsync($"{path}/me/task", new { taskId = "chop-wood" });
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal("Working", (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("state").GetString());
    }

    [Fact]
    public async Task A_crowd_joins_and_starts_work_in_one_village_at_once()
    {
        const int crowd = 12;
        var path = $"/api/villages/{DemoVillage.Slug}";
        var clients = await Task.WhenAll(Enumerable.Range(0, crowd).Select(_ => SignedInClientAsync(UniqueName("crowd"))));

        var joined = await Task.WhenAll(clients.Select((client, index) =>
            client.PostAsJsonAsync($"{path}/citizens", new { name = $"Crowd {index}" })));
        Assert.All(joined, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        var started = await Task.WhenAll(clients.Select(client => client.PostAsJsonAsync($"{path}/me/task", new { taskId = "chop-wood" })));
        Assert.All(started, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
    }

    [Fact]
    public async Task Village_hub_accepts_anonymous_viewers()
    {
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "hubs/village"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        await connection.StartAsync();
        await connection.InvokeAsync("JoinVillage", DemoVillage.Slug);
        await connection.InvokeAsync("LeaveVillage", DemoVillage.Slug);

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }
}
