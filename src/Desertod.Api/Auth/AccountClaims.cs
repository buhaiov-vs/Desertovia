using System.Security.Claims;

namespace Desertod.Api.Auth;

public static class AccountClaims
{
    public const string AccountId = "desertod:account_id";
    public const string Provider = "desertod:provider";

    public static Guid? GetAccountId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(AccountId), out var id) ? id : null;

    public static Guid RequireAccountId(this ClaimsPrincipal user) =>
        user.GetAccountId() ?? throw new UnauthorizedAccessException();
}
