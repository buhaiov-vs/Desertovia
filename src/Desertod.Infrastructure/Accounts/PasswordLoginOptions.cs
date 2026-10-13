namespace Desertod.Infrastructure.Accounts;

public sealed class PasswordLoginOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Accounts one IP address (or IPv6 /64 network) may register within <see cref="AddressWindow"/>; 0 turns the limit off.</summary>
    public int AccountsPerAddress { get; set; } = 3;

    public TimeSpan AddressWindow { get; set; } = TimeSpan.FromDays(1);

    /// <summary>Sign-in and registration requests one IP address (or IPv6 /64 network) may make per minute.</summary>
    public int AttemptsPerMinute { get; set; } = 10;
}
