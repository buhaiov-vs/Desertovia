using System.Security.Cryptography;

namespace Desertod.Infrastructure.Accounts;

/// <summary>PBKDF2 with HMAC-SHA512. The algorithm and iteration count are stored with each hash so they can grow later.</summary>
public static class PasswordHasher
{
    private const string Algorithm = "pbkdf2-sha512";
    private const int Iterations = 210_000;
    private const int SaltLength = 16;
    private const int HashLength = 32;

    /// <summary>Verified when an email has no account, so a wrong email takes as long as a wrong password.</summary>
    private static readonly string DecoyHash = Hash("decoy-password");

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashLength);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static void VerifyDecoy(string password) => Verify(password, DecoyHash);
}
