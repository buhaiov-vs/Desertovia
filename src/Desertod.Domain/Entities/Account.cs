namespace Desertod.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<ExternalLogin> Logins { get; set; } = [];
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
