using System.Security.Claims;
using Desertod.Api.Auth;

namespace Desertod.Api.Game;

public static class GameEndpoints
{
    public static void MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/catalog", () => DtoMapper.Catalog);

        api.MapGet("/villages/public", (GameService game, CancellationToken cancellationToken) =>
            game.ListPublicVillagesAsync(cancellationToken));

        api.MapGet("/villages/{slug}", (string slug, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.GetVillageAsync(slug, user.GetAccountId(), cancellationToken));

        var player = api.MapGroup("").RequireAuthorization();

        player.MapGet("/villages", (ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.ListVillagesAsync(user.RequireAccountId(), cancellationToken));

        player.MapPost("/villages", (CreateVillageRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.CreateVillageAsync(user.RequireAccountId(), request, cancellationToken));

        player.MapPost("/villages/{slug}/invite", (string slug, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.NewInviteCodeAsync(slug, user.RequireAccountId(), cancellationToken));

        player.MapPost("/villages/{slug}/live", (string slug, SetLiveRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.SetLiveAsync(slug, user.RequireAccountId(), request.IsLive, cancellationToken));

        player.MapGet("/villages/{slug}/me", async (string slug, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            await game.GetCitizenAsync(slug, user.RequireAccountId(), cancellationToken) is { } citizen
                ? Results.Ok(citizen)
                : Results.NotFound());

        player.MapPost("/villages/{slug}/citizens", (string slug, CreateCitizenRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.CreateCitizenAsync(slug, user.RequireAccountId(), request, cancellationToken));

        player.MapPut("/villages/{slug}/citizens/{citizenId:guid}/subtitle", (string slug, Guid citizenId, SetSubtitleRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.SetSubtitleAsync(slug, user.RequireAccountId(), citizenId, request.Subtitle, cancellationToken));

        player.MapPost("/villages/{slug}/me/task", (string slug, StartTaskRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.StartTaskAsync(slug, user.RequireAccountId(), request, cancellationToken));

        player.MapDelete("/villages/{slug}/me/task", (string slug, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.CancelTaskAsync(slug, user.RequireAccountId(), cancellationToken));

        player.MapPost("/villages/{slug}/me/drop", (string slug, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.DropLoadAsync(slug, user.RequireAccountId(), cancellationToken));

        player.MapPost("/villages/{slug}/me/take", (string slug, TakeRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.TakeFromStorageAsync(slug, user.RequireAccountId(), request, cancellationToken));

        player.MapPost("/villages/{slug}/me/goto", (string slug, GoToRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.GoToAsync(slug, user.RequireAccountId(), request, cancellationToken));

        player.MapPost("/villages/{slug}/me/job", (string slug, StartJobRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.StartJobAsync(slug, user.RequireAccountId(), request, cancellationToken));

        player.MapPost("/villages/{slug}/me/research", (string slug, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.StartResearchAsync(slug, user.RequireAccountId(), cancellationToken));

        player.MapPost("/villages/{slug}/me/vote", (string slug, VoteRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.VoteAsync(slug, user.RequireAccountId(), request.TechnologyId, cancellationToken));

        player.MapPost("/villages/{slug}/poll", (string slug, OpenPollRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.OpenPollAsync(slug, user.RequireAccountId(), request.TechnologyIds, cancellationToken));

        player.MapDelete("/villages/{slug}/poll", (string slug, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.CancelPollAsync(slug, user.RequireAccountId(), cancellationToken));

        player.MapPost("/villages/{slug}/blueprints", (string slug, PlaceBlueprintRequest request, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.PlaceBlueprintAsync(slug, user.RequireAccountId(), request, cancellationToken));

        player.MapDelete("/villages/{slug}/blueprints/{plotId}", (string slug, string plotId, ClaimsPrincipal user, GameService game, CancellationToken cancellationToken) =>
            game.CancelBlueprintAsync(slug, user.RequireAccountId(), plotId, cancellationToken));
    }
}
