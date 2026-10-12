using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Desertod.Api.Auth;

public sealed class TelegramOptions
{
    public string? BotToken { get; set; }
    public string? BotUsername { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BotToken) && !string.IsNullOrWhiteSpace(BotUsername);
}

public sealed record TelegramUser(string Id, string DisplayName);

/// <summary>
/// Verifies data from the Telegram Login Widget:
/// https://core.telegram.org/widgets/login#checking-authorization
/// </summary>
public sealed class TelegramLoginVerifier(IOptions<TelegramOptions> options, TimeProvider time)
{
    public const string ReturnUrlParameter = "returnUrl";

    private static readonly TimeSpan MaximumAge = TimeSpan.FromDays(1);
    private static readonly TimeSpan MaximumClockSkew = TimeSpan.FromMinutes(5);

    public TelegramUser? Verify(IQueryCollection query)
    {
        var botToken = options.Value.BotToken;
        if (string.IsNullOrWhiteSpace(botToken))
        {
            return null;
        }

        var hash = query["hash"].ToString();
        if (hash.Length != 64)
        {
            return null;
        }

        var dataCheckString = string.Join('\n', query
            .Where(pair => pair.Key != "hash" && pair.Key != ReturnUrlParameter)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));

        var secretKey = SHA256.HashData(Encoding.UTF8.GetBytes(botToken));
        var expected = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        byte[] received;
        try
        {
            received = Convert.FromHexString(hash);
        }
        catch (FormatException)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(expected, received))
        {
            return null;
        }

        if (!long.TryParse(query["auth_date"], NumberStyles.Integer, CultureInfo.InvariantCulture, out var authDate))
        {
            return null;
        }

        var age = time.GetUtcNow() - DateTimeOffset.FromUnixTimeSeconds(authDate);
        if (age > MaximumAge || age < -MaximumClockSkew)
        {
            return null;
        }

        var id = query["id"].ToString();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var username = query["username"].ToString();
        var fullName = $"{query["first_name"]} {query["last_name"]}".Trim();
        var displayName = !string.IsNullOrEmpty(username) ? username : fullName.Length > 0 ? fullName : $"telegram-{id}";

        return new TelegramUser(id, displayName);
    }
}
