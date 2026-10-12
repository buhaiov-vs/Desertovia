using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Desertod.Api.Tests;

/// <summary>
/// The real application on a throwaway PostgreSQL container. Runs as Development, so migrations apply, the demo
/// village is seeded and the name-only login is available. Needs Docker.
/// </summary>
public sealed class DesertodApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17").Build();

    public async Task InitializeAsync() => await database.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Desertod", database.GetConnectionString());
        builder.UseSetting("Authentication:Twitch:ClientId", "");
        builder.UseSetting("Authentication:Google:ClientId", "");
        builder.UseSetting("Authentication:Telegram:BotToken", "");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await database.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<DesertodApiFactory>
{
    public const string Name = "Api";
}
