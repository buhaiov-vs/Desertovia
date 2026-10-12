using Desertod.Domain.Entities;
using Desertod.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Desertod.Infrastructure.Accounts;

public sealed class AccountService(DesertodDbContext db, TimeProvider time)
{
    /// <summary>Finds the account linked to an external login, creating both on first sign-in.</summary>
    public async Task<Account> ResolveExternalLoginAsync(
        string provider,
        string providerUserId,
        string displayName,
        CancellationToken cancellationToken)
    {
        var login = await db.ExternalLogins
            .SingleOrDefaultAsync(l => l.Provider == provider && l.ProviderUserId == providerUserId, cancellationToken);

        if (login is not null)
        {
            login.ProviderUserName = displayName;
            await db.SaveChangesAsync(cancellationToken);
            return await db.Accounts.SingleAsync(a => a.Id == login.AccountId, cancellationToken);
        }

        var now = time.GetUtcNow();
        var account = new Account { DisplayName = Truncate(displayName, 64), CreatedAt = now };
        account.Logins.Add(new ExternalLogin
        {
            Provider = provider,
            ProviderUserId = providerUserId,
            ProviderUserName = Truncate(displayName, 128),
            AccountId = account.Id,
            LinkedAt = now,
        });

        db.Accounts.Add(account);
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
