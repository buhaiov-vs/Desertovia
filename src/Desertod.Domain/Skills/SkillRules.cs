using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Rules;

namespace Desertod.Domain.Skills;

public static class SkillRules
{
    /// <summary>Skill trained by researching any technology.</summary>
    public const Skill Research = Skill.Scholarship;

    public static int ExperienceToNext(int level) =>
        level is < 0 or >= GameRules.SkillMaximum ? 0 : Progression.Curve(level);

    /// <summary>Adds experience to a skill; returns the levels gained. Experience past the maximum level is lost.</summary>
    public static int AddExperience(Citizen citizen, Skill skill, int experience)
    {
        if (experience <= 0)
        {
            return 0;
        }

        var progress = citizen.Skills.SingleOrDefault(s => s.Kind == skill);
        if (progress is null)
        {
            progress = new SkillProgress { Kind = skill };
            citizen.Skills.Add(progress);
        }

        if (progress.Level >= GameRules.SkillMaximum)
        {
            return 0;
        }

        progress.Experience += experience;
        var levelsGained = 0;
        while (progress.Level < GameRules.SkillMaximum && progress.Experience >= ExperienceToNext(progress.Level))
        {
            progress.Experience -= ExperienceToNext(progress.Level);
            progress.Level++;
            levelsGained++;
        }

        if (progress.Level >= GameRules.SkillMaximum)
        {
            progress.Experience = 0;
        }

        return levelsGained;
    }

    /// <summary>Skill levels above what the work needs, counted up to <see cref="GameRules.SkillBonusLevels"/>.</summary>
    public static int Mastery(int level, int required) => Math.Clamp(level - required, 0, GameRules.SkillBonusLevels);

    public static double YieldMultiplier(Citizen citizen, Skill skill, int required) =>
        1 + GameRules.SkillYieldPerLevel * Mastery(citizen.SkillLevel(skill), required);

    /// <summary>Applies to the work only; walking takes as long as ever.</summary>
    public static TimeSpan WorkTime(Citizen citizen, Skill skill, int required, TimeSpan work) =>
        work * (1 - GameRules.SkillWorkTimePerLevel * Mastery(citizen.SkillLevel(skill), required));

    public static void Check(Citizen citizen, TaskDefinition task)
    {
        if (citizen.SkillLevel(task.Skill) >= task.SkillLevel)
        {
            return;
        }

        throw new GameRuleException("task.skillRequired", $"\"{task.Name}\" needs {task.Skill} {task.SkillLevel}.",
            new Dictionary<string, object> { ["taskId"] = task.Id, ["skill"] = task.Skill.ToString(), ["level"] = task.SkillLevel });
    }
}
