namespace Desertod.Domain.Rules;

public static class YieldMath
{
    public static double EffectiveStat(int primary, int secondary) =>
        GameRules.PrimaryStatWeight * primary + (1 - GameRules.PrimaryStatWeight) * secondary;

    public static double StatFactor(double effectiveStat) =>
        GameRules.StatFactorBase + GameRules.StatFactorPerPoint * effectiveStat;

    public static double StreakFactor(int consecutiveRenewals) =>
        1 + Math.Min(GameRules.StreakMaximumBonus, GameRules.StreakPerRenewal * consecutiveRenewals);

    /// <summary>
    /// Goods, work, experience and renown come in whole units: a scaled amount is rounded, and any work that
    /// earns something earns at least one.
    /// </summary>
    public static int Whole(double amount) =>
        amount <= 0 ? 0 : Math.Max(1, (int)Math.Round(amount, MidpointRounding.AwayFromZero));
}
