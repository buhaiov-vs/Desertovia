using System.Net.Mail;
using Desertod.Domain;
using Desertod.Domain.Entities;
using Desertod.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Desertod.Infrastructure.Accounts;

public sealed class AccountService(DesertodDbContext db, TimeProvider time, IOptions<PasswordLoginOptions> passwordOptions)
{
    public const int DisplayNameMaximumLength = 32;

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

    /// <summary>
    /// Creates an account with an email and password. One account per email; one IP address may register only
    /// <see cref="PasswordLoginOptions.AccountsPerAddress"/> accounts per <see cref="PasswordLoginOptions.AddressWindow"/>.
    /// </summary>
    public async Task<Account> RegisterWithPasswordAsync(
        string email,
        string password,
        string displayName,
        string? address,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email)
                              ?? throw new GameRuleException("auth.invalidEmail", "Enter a valid email address.");
        ValidatePassword(password);
        var name = displayName.Trim();
        if (name.Length is 0 or > DisplayNameMaximumLength)
        {
            throw new GameRuleException(
                "auth.invalidName",
                $"Name must be 1 to {DisplayNameMaximumLength} characters.",
                new Dictionary<string, object> { ["maximum"] = DisplayNameMaximumLength });
        }

        if (await db.PasswordLogins.AnyAsync(l => l.Email == normalizedEmail, cancellationToken))
        {
            throw EmailTaken();
        }

        var now = time.GetUtcNow();
        var options = passwordOptions.Value;
        if (address is not null && options.AccountsPerAddress > 0)
        {
            var since = now - options.AddressWindow;
            var recent = await db.PasswordLogins
                .CountAsync(l => l.RegisteredFrom == address && l.RegisteredAt > since, cancellationToken);
            if (recent >= options.AccountsPerAddress)
            {
                throw new GameRuleException(
                    "auth.tooManyAccounts",
                    "Too many accounts were registered from your network recently. Sign in to an existing one or try later.");
            }
        }

        var account = new Account { DisplayName = name, CreatedAt = now };
        db.Accounts.Add(account);
        db.PasswordLogins.Add(new PasswordLogin
        {
            Email = normalizedEmail,
            AccountId = account.Id,
            PasswordHash = PasswordHasher.Hash(password),
            RegisteredFrom = address,
            RegisteredAt = now,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw EmailTaken();
        }

        return account;
    }

    /// <summary>The account of the email when the password matches; null otherwise, without saying which was wrong.</summary>
    public async Task<Account?> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var login = normalizedEmail is null || password.Length > PasswordLogin.PasswordMaximumLength
            ? null
            : await db.PasswordLogins.SingleOrDefaultAsync(l => l.Email == normalizedEmail, cancellationToken);

        if (login is null)
        {
            PasswordHasher.VerifyDecoy(password);
            return null;
        }

        return PasswordHasher.Verify(password, login.PasswordHash)
            ? await db.Accounts.SingleAsync(a => a.Id == login.AccountId, cancellationToken)
            : null;
    }

    /// <summary>Trimmed, lower-case address without a display name part; null when it is not an email address.</summary>
    public static string? NormalizeEmail(string email)
    {
        var trimmed = email.Trim().ToLowerInvariant();
        return trimmed.Length <= PasswordLogin.EmailMaximumLength
               && MailAddress.TryCreate(trimmed, out var parsed)
               && parsed.Address == trimmed
               && parsed.Host.Contains('.')
            ? trimmed
            : null;
    }

    private static void ValidatePassword(string password)
    {
        if (password.Length is < PasswordLogin.PasswordMinimumLength or > PasswordLogin.PasswordMaximumLength)
        {
            throw new GameRuleException(
                "auth.passwordLength",
                $"Password must be {PasswordLogin.PasswordMinimumLength} to {PasswordLogin.PasswordMaximumLength} characters.",
                new Dictionary<string, object>
                {
                    ["minimum"] = PasswordLogin.PasswordMinimumLength,
                    ["maximum"] = PasswordLogin.PasswordMaximumLength,
                });
        }
    }

    private static GameRuleException EmailTaken() =>
        new("auth.emailTaken", "An account with this email already exists. Sign in instead.");

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
