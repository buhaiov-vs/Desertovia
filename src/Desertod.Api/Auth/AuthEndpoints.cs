using System.Security.Claims;
using Desertod.Infrastructure.Accounts;
using Desertod.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Desertod.Api.Auth;

public static class AuthEndpoints
{
    public sealed record DevelopmentLoginRequest(string Name);

    public sealed record MeResponse(Guid Id, string DisplayName, IReadOnlyList<string> Providers);

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
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
            await SignInAsync(http, account.Id, AuthenticationSetup.TelegramProvider, user.Id, user.DisplayName);

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
            await SignInAsync(http, account.Id, AuthenticationSetup.DevelopmentProvider, userId, name);
            return Results.NoContent();
        });

        auth.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        app.MapGet("/api/me", async (ClaimsPrincipal user, DesertodDbContext db, CancellationToken cancellationToken) =>
        {
            var accountId = user.GetAccountId();
            if (accountId is null)
            {
                return Results.Unauthorized();
            }

            var account = await db.Accounts
                .Where(a => a.Id == accountId)
                .Select(a => new MeResponse(a.Id, a.DisplayName, a.Logins.Select(l => l.Provider).ToList()))
                .SingleOrDefaultAsync(cancellationToken);

            return account is null ? Results.Unauthorized() : Results.Ok(account);
        });
    }

    private static Task SignInAsync(HttpContext http, Guid accountId, string provider, string providerUserId, string name)
    {
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
