namespace Desertod.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ExternalLogin> Logins { get; set; } = [];

    /// <summary>Created once after signing up; null until then. Every village the account joins gets a citizen with this name and look.</summary>
    public Character? Character { get; set; }
}

/// <summary>Only the identity is shared: stats, skills and renown belong to each village's citizen.</summary>
public sealed class Character
{
    public required string Name { get; set; }
    public required Appearance Appearance { get; set; }
}

public sealed class ExternalLogin
{
    /// <summary>"twitch", "google", "telegram" or "dev".</summary>
    public required string Provider { get; set; }
    public required string ProviderUserId { get; set; }
    public Guid AccountId { get; set; }
    public string? ProviderUserName { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
}

/// <summary>Email and password sign-in. The email is never verified: nothing is sent to it.</summary>
public sealed class PasswordLogin
{
    public const int EmailMaximumLength = 254;
    public const int PasswordMinimumLength = 8;
    public const int PasswordMaximumLength = 128;
    public const int AddressMaximumLength = 45;

    /// <summary>Trimmed and lower-case, so one mailbox maps to one account.</summary>
    public required string Email { get; set; }
    public Guid AccountId { get; set; }
    public required string PasswordHash { get; set; }
    /// <summary>IPv4 address, or the /64 network of an IPv6 address; null when unknown.</summary>
    public string? RegisteredFrom { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
}
