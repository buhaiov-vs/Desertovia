namespace Desertod.Domain;

/// <summary>
/// A player action that the game rules do not allow. Safe to show to the player.
/// <see cref="Code"/> and <see cref="Arguments"/> let clients show the message in their own language;
/// <see cref="Exception.Message"/> is the English fallback.
/// </summary>
public sealed class GameRuleException(string code, string message, IReadOnlyDictionary<string, object>? arguments = null)
    : Exception(message)
{
    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object> Arguments { get; } = arguments ?? new Dictionary<string, object>();
}
