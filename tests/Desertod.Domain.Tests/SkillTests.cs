using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Rules;
using Desertod.Domain.Skills;
using Desertod.Domain.Stats;
using Desertod.Domain.Technologies;
using static Desertod.Domain.Tests.TestWorld;

namespace Desertod.Domain.Tests;

public class SkillTests
{
    private static Village BakeryVillage()
    {
        var village = NewVillage();
        foreach (var technology in new[] { TechnologyCatalog.Agriculture, TechnologyCatalog.Baking })
        {
            village.Research.Add(new TechnologyProgress { TechnologyId = technology, ResearchedAt = Now });
        }

        village.Structures.Add(new Structure { Kind = BuildingKind.Bakery, PlotId = "21,12", BuiltAt = Now });
        village.Store(ResourceKind.Grain, 10);
        return village;
    }

    private static Citizen Baker(Village village, int bakingLevel = 0)
    {
        var baker = NewCitizen(village, (Stat.Dexterity, 4), (Stat.Endurance, 2), (Stat.Intelligence, 2));
        if (bakingLevel > 0)
        {
            baker.Skills.Add(new SkillProgress { Kind = Skill.Baking, Level = bakingLevel });
        }

        return baker;
    }

    [Fact]
    public void BakingBread_TrainsTheBakingSkill()
    {
        var village = BakeryVillage();
        var baker = Baker(village);

        CycleEngine.Start(baker, Task("bake-bread"), WorkContext.For(village), Now);
        var report = Finish(baker, WorkContext.For(village));

        Assert.Equal(Skill.Baking, report.Skill);
        Assert.Equal(report.PrimaryExperience, report.SkillExperience);
        Assert.Equal(report.PrimaryExperience, baker.Skills.Single(s => s.Kind == Skill.Baking).Experience);
        Assert.Equal(0, baker.SkillLevel(Skill.Woodcutting));
    }

    [Fact]
    public void ConstructionJobs_TrainTheirCraft()
    {
        Assert.Equal(Skill.Hauling, BuildingCatalog.FindJob("bring-logs")!.Skill);
        Assert.Equal(Skill.Building, BuildingCatalog.FindJob("raise-posts")!.Skill);
        Assert.Equal(Skill.Woodcutting, BuildingCatalog.FindJob(BuildingCatalog.FellTrees)!.Skill);
    }

    [Fact]
    public void Experience_RaisesTheLevel_OnTheStatCurve()
    {
        var citizen = NewCitizen(NewVillage());

        var gained = SkillRules.AddExperience(citizen, Skill.Baking, GameRules.FirstLevelExperience + 10);

        Assert.Equal(1, gained);
        var progress = citizen.Skills.Single();
        Assert.Equal(1, progress.Level);
        Assert.Equal(10, progress.Experience);
    }

    [Fact]
    public void Experience_PastTheMaximum_IsLost()
    {
        var citizen = NewCitizen(NewVillage());
        citizen.Skills.Add(new SkillProgress { Kind = Skill.Baking, Level = GameRules.SkillMaximum });

        Assert.Equal(0, SkillRules.AddExperience(citizen, Skill.Baking, 1000));
        Assert.Equal(0, citizen.Skills.Single().Experience);
        Assert.Equal(0, SkillRules.ExperienceToNext(GameRules.SkillMaximum));
    }

    [Fact]
    public void SkilledBaker_BakesMore_AndFaster()
    {
        var village = BakeryVillage();
        var novice = Baker(village);
        var master = Baker(village, bakingLevel: GameRules.SkillBonusLevels);

        CycleEngine.Start(novice, Task("bake-bread"), WorkContext.For(village), Now);
        CycleEngine.Start(master, Task("bake-bread"), WorkContext.For(village), Now);

        var saved = TimeSpan.FromSeconds(Task("bake-bread").WorkSeconds) * GameRules.SkillWorkTimePerLevel * GameRules.SkillBonusLevels;
        Assert.Equal(novice.CycleEndsAt - saved, master.CycleEndsAt);

        var noviceBread = Finish(novice, WorkContext.For(village)).Produced.Single().Amount;
        var masterBread = Finish(master, WorkContext.For(village)).Produced.Single().Amount;
        Assert.True(masterBread > noviceBread);
    }

    [Fact]
    public void Bonus_StopsGrowing_PastTheBonusLevels()
    {
        Assert.Equal(GameRules.SkillBonusLevels, SkillRules.Mastery(GameRules.SkillMaximum, 0));
        Assert.Equal(0, SkillRules.Mastery(2, 3));
        Assert.Equal(1, SkillRules.Mastery(4, 3));
    }

    [Fact]
    public void MasterWork_NeedsSkillInItsCraft()
    {
        var citizen = NewCitizen(NewVillage());
        var task = Task("mine-gold");

        var error = Assert.Throws<GameRuleException>(() => SkillRules.Check(citizen, task));
        Assert.Equal("task.skillRequired", error.Code);
        Assert.Equal(nameof(Skill.Mining), error.Arguments["skill"]);

        citizen.Skills.Add(new SkillProgress { Kind = Skill.Mining, Level = task.SkillLevel });
        SkillRules.Check(citizen, task);
    }

    [Fact]
    public void EveryMasterTask_NeedsSkill_AndNoOtherDoes()
    {
        foreach (var task in GameCatalog.Tasks)
        {
            var master = GameCatalog.RoleOf(task).Tier == RoleTier.Master;
            Assert.Equal(master, task.SkillLevel > 0);
        }
    }
}
