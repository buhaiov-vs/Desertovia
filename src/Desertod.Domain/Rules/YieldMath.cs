namespace Desertod.Domain.Rules;

public static class YieldMath
{
    public static double EffectiveStat(int primary, int secondary) =>
        GameRules.PrimaryStatWeight * primary + (1 - GameRules.PrimaryStatWeight) * secondary;

    public static double StatFactor(double effectiveStat) =>
        GameRules.StatFactorBase + GameRules.StatFactorPerPoint * effectiveStat;

    public static double StreakFactor(int consecutiveRenewals) =>
        1 + Math.Min(GameRules.StreakMaximumBonus, GameRules.StreakPerRenewal * consecutiveRenewals);
}
