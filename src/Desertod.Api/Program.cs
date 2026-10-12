using System.Text.Json.Serialization;
using Desertod.Api.Auth;
using Desertod.Api.Game;
using Desertod.Infrastructure;
using Desertod.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Desertod")
    ?? throw new InvalidOperationException("Connection string 'Desertod' is not configured."));

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDesertodAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GameExceptionHandler>();

builder.Services.AddSingleton<TelegramLoginVerifier>();
builder.Services.AddSingleton<VillageNotifier>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddHostedService<CycleSettlementWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<DesertodDbContext>();
    await db.Database.MigrateAsync();
    await DemoVillage.SeedAsync(db, app.Services.GetRequiredService<TimeProvider>(), CancellationToken.None);
}

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapGameEndpoints();
app.MapHub<VillageHub>("/hubs/village");
app.MapFallbackToFile("index.html");

app.Run();
