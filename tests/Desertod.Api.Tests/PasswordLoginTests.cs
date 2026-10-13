using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Desertod.Api.Tests;

/// <summary>Email and password sign-in: one account per email, a few accounts per IP address.</summary>
[Collection(ApiCollection.Name)]
public sealed class PasswordLoginTests(DesertodApiFactory factory)
{
    private const string Password = "correct horse";

    /// <summary>Each test comes from its own address so the per-address limits do not leak between tests.</summary>
    private HttpClient ClientFrom(string address)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(DesertodApiFactory.AddressHeader, address);
        return client;
    }

    private static string NewEmail() => $"player-{Guid.NewGuid():N}@example.com";

    private static string RandomAddress() => $"10.{Random.Shared.Next(256)}.{Random.Shared.Next(256)}.{Random.Shared.Next(1, 255)}";

    private static async Task<string?> ProblemCodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString();

    [Fact]
    public async Task Providers_offer_password_login()
    {
        var providers = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/auth/providers");

        Assert.True(providers.GetProperty("password").GetBoolean());
    }

    [Fact]
    public async Task Registering_signs_in_and_the_password_signs_in_again()
    {
        var address = RandomAddress();
        var email = NewEmail();
        var registered = ClientFrom(address);

        var register = await registered.PostAsJsonAsync("/auth/password/register", new { email, password = Password, name = "Nomad" });
        Assert.Equal(HttpStatusCode.NoContent, register.StatusCode);
        var me = await registered.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.Equal("Nomad", me.GetProperty("displayName").GetString());
        Assert.Contains(me.GetProperty("providers").EnumerateArray(), provider => provider.GetString() == "email");

        var again = ClientFrom(address);
        var login = await again.PostAsJsonAsync("/auth/password/login", new { email = $"  {email.ToUpperInvariant()} ", password = Password });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        var sameMe = await again.GetFromJsonAsync<JsonElement>("/api/me");
        Assert.Equal(me.GetProperty("id").GetGuid(), sameMe.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var client = ClientFrom(RandomAddress());
        var email = NewEmail();
        await client.PostAsJsonAsync("/auth/password/register", new { email, password = Password, name = "Nomad" });

        var login = await ClientFrom(RandomAddress()).PostAsJsonAsync("/auth/password/login", new { email, password = "wrong password" });

        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        Assert.Equal("auth.wrongCredentials", await ProblemCodeAsync(login));
    }

    [Fact]
    public async Task Same_email_cannot_register_twice_in_any_case()
    {
        var email = NewEmail();
        await ClientFrom(RandomAddress()).PostAsJsonAsync("/auth/password/register", new { email, password = Password, name = "First" });

        var second = await ClientFrom(RandomAddress()).PostAsJsonAsync(
            "/auth/password/register", new { email = email.ToUpperInvariant(), password = Password, name = "Second" });

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal("auth.emailTaken", await ProblemCodeAsync(second));
    }

    [Fact]
    public async Task One_address_registers_only_a_few_accounts()
    {
        var client = ClientFrom(RandomAddress());
        for (var i = 0; i < 3; i++)
        {
            var allowed = await client.PostAsJsonAsync("/auth/password/register", new { email = NewEmail(), password = Password, name = $"Alt {i}" });
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        }

        var refused = await client.PostAsJsonAsync("/auth/password/register", new { email = NewEmail(), password = Password, name = "Alt 3" });

        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("auth.tooManyAccounts", await ProblemCodeAsync(refused));
    }

    [Fact]
    public async Task IPv6_addresses_of_one_network_share_the_limit()
    {
        var network = $"2001:db8:{Random.Shared.Next(1, 0xffff):x}:{Random.Shared.Next(1, 0xffff):x}";
        for (var i = 0; i < 3; i++)
        {
            var allowed = await ClientFrom($"{network}::{i + 1}").PostAsJsonAsync(
                "/auth/password/register", new { email = NewEmail(), password = Password, name = $"Alt {i}" });
            Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        }

        var refused = await ClientFrom($"{network}::99").PostAsJsonAsync(
            "/auth/password/register", new { email = NewEmail(), password = Password, name = "Alt 3" });

        Assert.Equal("auth.tooManyAccounts", await ProblemCodeAsync(refused));
    }

    [Theory]
    [InlineData("not-an-email", Password, "Nomad", "auth.invalidEmail")]
    [InlineData("Name <someone@example.com>", Password, "Nomad", "auth.invalidEmail")]
    [InlineData("someone@example.com", "short", "Nomad", "auth.passwordLength")]
    [InlineData("someone@example.com", Password, "   ", "auth.invalidName")]
    public async Task Invalid_registrations_are_rejected(string email, string password, string name, string code)
    {
        var response = await ClientFrom(RandomAddress()).PostAsJsonAsync("/auth/password/register", new { email, password, name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(code, await ProblemCodeAsync(response));
    }

    [Fact]
    public async Task Too_many_attempts_from_one_address_are_throttled()
    {
        var client = ClientFrom(RandomAddress());
        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            last = await client.PostAsJsonAsync("/auth/password/login", new { email = NewEmail(), password = Password });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.Equal("auth.tooManyAttempts", await ProblemCodeAsync(last));
    }
}
