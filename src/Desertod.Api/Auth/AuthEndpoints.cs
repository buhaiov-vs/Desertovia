using System.Security.Claims;
using Desertod.Domain;
using Desertod.Domain.Entities;
using Desertod.Domain.Rules;
using Desertod.Infrastructure.Accounts;
using Desertod.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Desertod.Api.Auth;

public static class AuthEndpoints
{
    public sealed record DevelopmentLoginRequest(string Name);

    public sealed record PasswordRegisterRequest(string Email, string Password, string Name);

    public sealed record PasswordLoginRequest(string Email, string Password);

    public sealed record CreateCharacterRequest(string Name, Appearance? Appearance);

    /// <param name="Character">Null until the player creates it; villages cannot be joined before that.</param>
    public sealed record MeResponse(Guid Id, string DisplayName, IReadOnlyList<string> Providers, Character? Character);

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var logger = app.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AuthEndpoints));
        var auth = app.MapGroup("/auth");

        auth.MapGet("/providers", (LoginProviders providers) => providers);

        auth.MapGet("/login/{provider}", (string provider, string? returnUrl, LoginProviders providers) =>
        {
            var configured = provider switch
            {
                AuthenticationSetup.TwitchProvider => providers.Twitch,
                AuthenticationSetup.GoogleProvider => providers.Google,
                _ => false,
            };

            var scheme = AuthenticationSetup.SchemeFor(provider);
            if (!configured || scheme is null)
            {
                logger.ProviderNotConfigured(provider);
                return Results.NotFound();
            }

            return Results.Challenge(new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) }, [scheme]);
        });

        auth.MapGet("/telegram/callback", async (
            HttpContext http,
            TelegramLoginVerifier verifier,
            AccountService accounts,
            CancellationToken cancellationToken) =>
        {
            var user = verifier.Verify(http.Request.Query);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var account = await accounts.ResolveExternalLoginAsync(
                AuthenticationSetup.TelegramProvider, user.Id, user.DisplayName, cancellationToken);
            await SignInAsync(http, logger, account.Id, AuthenticationSetup.TelegramProvider, user.Id, user.DisplayName);

            return Results.LocalRedirect(SafeReturnUrl(http.Request.Query[TelegramLoginVerifier.ReturnUrlParameter]));
        });

        auth.MapPost("/dev-login", async (
            DevelopmentLoginRequest request,
            HttpContext http,
            LoginProviders providers,
            AccountService accounts,
            CancellationToken cancellationToken) =>
        {
            if (!providers.DevelopmentLogin)
            {
                return Results.NotFound();
            }

            var name = request.Name.Trim();
            if (name.Length is 0 or > 32)
            {
                return Results.BadRequest("Name must be 1 to 32 characters.");
            }

            var userId = name.ToLowerInvariant();
            var account = await accounts.ResolveExternalLoginAsync(
                AuthenticationSetup.DevelopmentProvider, userId, name, cancellationToken);
            await SignInAsync(http, logger, account.Id, AuthenticationSetup.DevelopmentProvider, userId, name);
            return Results.NoContent();
        });

        var password = auth.MapGroup("/password").RequireRateLimiting(AuthenticationSetup.PasswordRateLimit);

        password.MapPost("/register", async (
            PasswordRegisterRequest request,
            HttpContext http,
            LoginProviders providers,
            AccountService accounts,
            CancellationToken cancellationToken) =>
        {
            if (!providers.Password)
            {
                return Results.NotFound();
            }

            var account = await accounts.RegisterWithPasswordAsync(
                request.Email, request.Password, request.Name, ClientAddress.Of(http), cancellationToken);
            logger.Registered(account.Id);
            await SignInAsync(http, logger, account.Id, AuthenticationSetup.PasswordProvider, account.Id.ToString(), account.DisplayName);
            return Results.NoContent();
        });

        password.MapPost("/login", async (
            PasswordLoginRequest request,
            HttpContext http,
            LoginProviders providers,
            AccountService accounts,
            CancellationToken cancellationToken) =>
        {
            if (!providers.Password)
            {
                return Results.NotFound();
            }

            var account = await accounts.SignInWithPasswordAsync(request.Email, request.Password, cancellationToken);
            if (account is null)
            {
                logger.PasswordSignInFailed(ClientAddress.Of(http));
                throw new GameRuleException("auth.wrongCredentials", "Wrong email or password.");
            }

            await SignInAsync(http, logger, account.Id, AuthenticationSetup.PasswordProvider, account.Id.ToString(), account.DisplayName);
            return Results.NoContent();
        });

        auth.MapPost("/logout", async (HttpContext http) =>
        {
            logger.SignedOut(http.User.GetAccountId());
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        app.MapGet("/api/me", async (ClaimsPrincipal user, DesertodDbContext db, CancellationToken cancellationToken) =>
            user.GetAccountId() is { } accountId && await LoadMeAsync(db, accountId, cancellationToken) is { } me
                ? Results.Ok(me)
                : Results.Unauthorized());

        app.MapPost("/api/me/character", async (
            CreateCharacterRequest request,
            ClaimsPrincipal user,
            DesertodDbContext db,
            CancellationToken cancellationToken) =>
        {
            var accountId = user.RequireAccountId();
            var account = await db.Accounts.SingleAsync(a => a.Id == accountId, cancellationToken);
            if (account.Character is not null)
            {
                throw new GameRuleException("character.exists", "You already have a character.");
            }

            account.Character = CharacterCreation.CreateCharacter(request.Name ?? "", request.Appearance);
            await db.SaveChangesAsync(cancellationToken);
            logger.CharacterCreated(accountId);
            return Results.Ok(await LoadMeAsync(db, accountId, cancellationToken));
        }).RequireAuthorization();
    }

    private static async Task<MeResponse?> LoadMeAsync(DesertodDbContext db, Guid accountId, CancellationToken cancellationToken)
    {
        var account = await db.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => new MeResponse(a.Id, a.DisplayName, a.Logins.Select(l => l.Provider).ToList(), a.Character))
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null)
        {
            return null;
        }

        var hasPassword = await db.PasswordLogins.AnyAsync(l => l.AccountId == accountId, cancellationToken);
        return hasPassword
            ? account with { Providers = [.. account.Providers, AuthenticationSetup.PasswordProvider] }
            : account;
    }

    private static Task SignInAsync(HttpContext http, ILogger logger, Guid accountId, string provider, string providerUserId, string name)
    {
        logger.SignedIn(accountId, provider);
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, providerUserId),
                new Claim(ClaimTypes.Name, name),
                new Claim(AccountClaims.AccountId, accountId.ToString()),
                new Claim(AccountClaims.Provider, provider),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
        && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/";
}
