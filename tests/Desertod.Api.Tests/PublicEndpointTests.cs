using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Desertod.Api.Game;

namespace Desertod.Api.Tests;

/// <summary>Everything an anonymous visitor or the stream overlay can reach.</summary>
[Collection(ApiCollection.Name)]
public sealed class PublicEndpointTests(DesertodApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Catalog_lists_tasks_roles_and_rules()
    {
        var catalog = await client.GetFromJsonAsync<JsonElement>("/api/catalog");

        Assert.NotEmpty(catalog.GetProperty("tasks").EnumerateArray());
        Assert.NotEmpty(catalog.GetProperty("roles").EnumerateArray());
        Assert.Equal(JsonValueKind.Object, catalog.GetProperty("rules").ValueKind);
    }

    [Fact]
    public async Task Login_providers_offer_the_development_login()
    {
        var providers = await client.GetFromJsonAsync<JsonElement>("/auth/providers");

        Assert.True(providers.GetProperty("developmentLogin").GetBoolean());
    }

    [Fact]
    public async Task Open_villages_include_the_demo_village()
    {
        var villages = await client.GetFromJsonAsync<JsonElement>("/api/villages/public");

        Assert.Contains(villages.EnumerateArray(), village => village.GetProperty("slug").GetString() == DemoVillage.Slug);
    }

    [Fact]
    public async Task Demo_village_state_loads_with_citizens()
    {
        var village = await client.GetFromJsonAsync<JsonElement>($"/api/villages/{DemoVillage.Slug}");

        Assert.Equal(DemoVillage.Slug, village.GetProperty("state").GetProperty("slug").GetString());
        Assert.NotEmpty(village.GetProperty("citizens").EnumerateArray());
    }

    [Fact]
    public async Task Unknown_village_is_not_found()
    {
        var response = await client.GetAsync("/api/villages/no-such-village");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/me")]
    [InlineData("/api/villages")]
    [InlineData($"/api/villages/{DemoVillage.Slug}/me")]
    public async Task Player_endpoints_need_a_login(string path)
    {
        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/new")]
    [InlineData($"/v/{DemoVillage.Slug}")]
    [InlineData($"/v/{DemoVillage.Slug}/overlay")]
    public async Task Client_routes_do_not_fail_on_the_server(string path)
    {
        var response = await client.GetAsync(path);

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound,
            $"{path} answered {(int)response.StatusCode}.");
    }
}
