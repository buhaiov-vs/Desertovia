using Desertod.Domain.Stats;

namespace Desertod.Domain.Rules;

public readonly record struct ExperienceResult(int LevelsGained, double RenownGained);

public static class Progression
{
    public static double ExperienceToNext(int level)
    {
        if (level < GameRules.StatMinimum || level >= GameRules.StatMaximum)
        {
            return 0;
        }

        if (level == 0)
        {
            return GameRules.FirstLevelExperience;
        }

        return Math.Round(GameRules.ExperienceCurveBase * Math.Pow(level, GameRules.ExperienceCurveExponent));
    }

    /// <summary>
    /// Adds experience to a stat. Experience that cannot raise power (stat maxed or talent cap reached)
    /// is converted into Renown.
    /// </summary>
    public static ExperienceResult AddExperience(StatBlock stats, Stat stat, double experience)
    {
        if (experience <= 0)
        {
            return new ExperienceResult(0, 0);
        }

        var level = stats.Get(stat);
        if (level >= GameRules.StatMaximum || stats.Total >= GameRules.TalentCap)
        {
            return new ExperienceResult(0, experience);
        }

        var pool = stats.GetExperience(stat) + experience;
        var levelsGained = 0;

        while (level < GameRules.StatMaximum
               && stats.Total < GameRules.TalentCap
               && pool >= ExperienceToNext(level))
        {
            pool -= ExperienceToNext(level);
            level++;
            levelsGained++;
            stats.Set(stat, level);
        }

        double renown = 0;
        if (level >= GameRules.StatMaximum || stats.Total >= GameRules.TalentCap)
        {
            renown = pool;
            pool = 0;
        }

        stats.SetExperience(stat, pool);
        return new ExperienceResult(levelsGained, renown);
    }

    public static double ChallengeMultiplier(int statLevel, int tierRequirement) =>
        statLevel <= tierRequirement + GameRules.ChallengeThreshold ? 1.0 : GameRules.ChallengePenalty;
}
