namespace Desertod.Domain.Rules;

/// <summary>
/// Tuning parameters. Values mirror the table in docs/game-design.md section 10.
/// </summary>
public static class GameRules
{
    /// <summary>Everyone starts wild: every stat at 0, grown only by working.</summary>
    public const int StatMinimum = 0;
    public const int StatMaximum = 10;
    public const int TalentCap = 30;

    /// <summary>Work time of an ordinary task. Walking to the place and to storage is added on top.</summary>
    public static readonly TimeSpan CycleLength = TimeSpan.FromSeconds(90);
    public static readonly TimeSpan GraceWindow = TimeSpan.FromSeconds(60);

    /// <summary>Citizens active within this window count towards the crew a construction stage needs.</summary>
    public static readonly TimeSpan RecentActivityWindow = TimeSpan.FromHours(2);

    public const double BaseExperiencePerCycle = 10;
    public const double SecondaryExperienceShare = 0.3;
    public const double ExperienceCurveBase = 60;
    public const double ExperienceCurveExponent = 1.5;
    public const double FirstLevelExperience = 50;

    public const double FoodPerCycle = 0.2;
    public const double HungryExperienceMultiplier = 0.5;
    public const double HungryRegenerationMultiplier = 0.5;
    public const double UnderTreesRegeneration = 0.5;

    public const int ChallengeThreshold = 3;
    public const double ChallengePenalty = 0.25;

    public const double EnergyBase = 60;
    public const double EnergyPerEndurance = 8;
    public const double EnergyFullRegenerationHours = 20;
    public const double EnergyCostPerCycle = 1;

    public const double TiredExperienceMultiplier = 0;
    public const double TiredYieldMultiplier = 0.25;
    public const double OverflowExperienceMultiplier = 0.5;
    public const double LiveExperienceMultiplier = 1.25;

    public const double StatFactorBase = 0.4;
    public const double StatFactorPerPoint = 0.12;
    public const double PrimaryStatWeight = 0.7;

    public const double StreakPerRenewal = 0.01;
    public const double StreakMaximumBonus = 0.20;

    public const double MoraleMinimum = 0.8;
    public const double MoraleMaximum = 1.2;
    public const double MoralePerPerformYield = 0.002;
}
