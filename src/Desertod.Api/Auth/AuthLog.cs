namespace Desertod.Api.Auth;

internal static partial class AuthLog
{
    [LoggerMessage(2000, LogLevel.Information, "Account {AccountId} signed in with {Provider}.")]
    public static partial void SignedIn(this ILogger logger, Guid accountId, string provider);

    [LoggerMessage(2001, LogLevel.Information, "Account {AccountId} registered with a password.")]
    public static partial void Registered(this ILogger logger, Guid accountId);

    [LoggerMessage(2002, LogLevel.Information, "Password sign-in failed from {Address}.")]
    public static partial void PasswordSignInFailed(this ILogger logger, string? address);

    [LoggerMessage(2003, LogLevel.Debug, "Telegram login rejected: {Reason}.")]
    public static partial void TelegramRejected(this ILogger logger, string reason);

    [LoggerMessage(2004, LogLevel.Debug, "Login with {Provider} refused: the provider is not configured.")]
    public static partial void ProviderNotConfigured(this ILogger logger, string provider);

    [LoggerMessage(2005, LogLevel.Debug, "Account {AccountId} signed out.")]
    public static partial void SignedOut(this ILogger logger, Guid? accountId);

    [LoggerMessage(2006, LogLevel.Information, "Account {AccountId} created its character.")]
    public static partial void CharacterCreated(this ILogger logger, Guid accountId);
}
