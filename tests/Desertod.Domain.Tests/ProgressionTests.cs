using Desertod.Domain.Rules;
using Desertod.Domain.Stats;

namespace Desertod.Domain.Tests;

public class ProgressionTests
{
    [Theory]
    [InlineData(1, 60)]
    [InlineData(2, 170)]
    [InlineData(4, 480)]
    [InlineData(9, 1620)]
    [InlineData(10, 0)]
    public void ExperienceToNext_MatchesDesignTable(int level, double expected) =>
        Assert.Equal(expected, Progression.ExperienceToNext(level));

    [Fact]
    public void JourneymanLevel_TakesUnderTwoActiveHours()
    {
        var total = Enumerable.Range(0, 4).Sum(Progression.ExperienceToNext);
        var hours = total / (GameRules.BaseExperiencePerCycle * 40);

        Assert.Equal(592, total);
        Assert.InRange(hours, 1, 2);
    }

    [Fact]
    public void ReachingLevelTen_TakesAboutSeventeenActiveHours()
    {
        var total = Enumerable.Range(0, 10).Sum(Progression.ExperienceToNext);
        var hours = total / (GameRules.BaseExperiencePerCycle * 40);

        Assert.Equal(6714, total);
        Assert.InRange(hours, 16, 18);
    }

    [Fact]
    public void AddExperience_LevelsUpAndKeepsRemainder()
    {
        var stats = new StatBlock();

        var result = Progression.AddExperience(stats, Stat.Strength, 600);

        Assert.Equal(4, result.LevelsGained);
        Assert.Equal(4, stats.Strength);
        Assert.Equal(8, stats.StrengthExperience);
        Assert.Equal(0, result.RenownGained);
    }

    [Fact]
    public void AddExperience_AtTalentCap_BecomesRenown()
    {
        var stats = new StatBlock { Strength = 10, Dexterity = 10, Intelligence = 8, Charisma = 1, Endurance = 1 };

        var result = Progression.AddExperience(stats, Stat.Charisma, 1000);

        Assert.Equal(1, stats.Charisma);
        Assert.Equal(1000, result.RenownGained);
    }

    [Fact]
    public void AddExperience_ReachingTalentCapMidway_ConvertsLeftoverToRenown()
    {
        var stats = new StatBlock { Strength = 10, Dexterity = 10, Intelligence = 7, Charisma = 1, Endurance = 1 };

        var result = Progression.AddExperience(stats, Stat.Charisma, 1000);

        Assert.Equal(2, stats.Charisma);
        Assert.Equal(GameRules.TalentCap, stats.Total);
        Assert.Equal(940, result.RenownGained);
        Assert.Equal(0, stats.CharismaExperience);
    }

    [Theory]
    [InlineData(4, 1, 1.0)]
    [InlineData(5, 1, 0.25)]
    [InlineData(7, 4, 1.0)]
    [InlineData(8, 4, 0.25)]
    [InlineData(10, 7, 1.0)]
    public void ChallengeMultiplier_PenalizesTrivialTasks(int stat, int requirement, double expected) =>
        Assert.Equal(expected, Progression.ChallengeMultiplier(stat, requirement));
}
