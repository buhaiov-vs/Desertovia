using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Rules;
using Desertod.Domain.Stats;
using Desertod.Domain.Storage;
using Desertod.Domain.Technologies;
using static Desertod.Domain.Tests.TestWorld;

namespace Desertod.Domain.Tests;

public class TechnologyTests
{
    private static WorkContext Context(Village village, int recent = 2) =>
        new() { Village = village, RecentCitizenCount = recent };

    private static void Discover(Village village, string technologyId) =>
        village.Research.Add(new TechnologyProgress { TechnologyId = technologyId, ResearchedAt = Now });

    private static void Choose(Village village, string technologyId) => village.ChosenTechnology = technologyId;

    private static CycleReport Research(Citizen citizen, WorkContext context, ref DateTimeOffset now)
    {
        CycleEngine.StartResearch(citizen, context, now);
        now = citizen.CycleEndsAt!.Value;
        return CycleEngine.Settle(citizen, context, now)!;
    }

    [Fact]
    public void Poll_OnlyForAvailableTechnologies()
    {
        var village = NewVillage();

        Assert.Equal("technology.requires",
            Assert.Throws<GameRuleException>(() =>
                TechnologyRules.OpenPoll(village, [TechnologyCatalog.Agriculture, TechnologyCatalog.HerbLore], 3, Now)).Code);
        Assert.Equal("poll.tooFewOptions",
            Assert.Throws<GameRuleException>(() => TechnologyRules.OpenPoll(village, [TechnologyCatalog.HerbLore], 3, Now)).Code);
        Assert.Equal("poll.noCitizens",
            Assert.Throws<GameRuleException>(() =>
                TechnologyRules.OpenPoll(village, [TechnologyCatalog.HerbLore, TechnologyCatalog.StoneTools], 0, Now)).Code);

        var poll = TechnologyRules.OpenPoll(village, [TechnologyCatalog.HerbLore, TechnologyCatalog.StoneTools], 3, Now);

        Assert.True(poll.IsOpen);
        Assert.Equal("poll.alreadyOpen",
            Assert.Throws<GameRuleException>(() =>
                TechnologyRules.OpenPoll(village, [TechnologyCatalog.HerbLore, TechnologyCatalog.StoneTools], 3, Now)).Code);
    }

    [Fact]
    public void Poll_ClosesWhenMoreThanHalfVoted()
    {
        var village = NewVillage();
        var ann = NewCitizen(village, "Ann", Now);
        var bob = NewCitizen(village, "Bob", Now);
        TechnologyRules.OpenPoll(village, [TechnologyCatalog.HerbLore, TechnologyCatalog.StoneTools], 4, Now);

        Assert.Equal("poll.notAnOption",
            Assert.Throws<GameRuleException>(() => TechnologyRules.CastVote(village, ann, TechnologyCatalog.Weaving, 4, Now)).Code);

        Assert.False(TechnologyRules.CastVote(village, ann, TechnologyCatalog.HerbLore, 4, Now));
        Assert.False(TechnologyRules.CastVote(village, ann, TechnologyCatalog.StoneTools, 4, Now));
        Assert.Single(village.Poll!.Ballots);
        Assert.Null(TechnologyRules.Current(village));

        // Two of four is half, not more than half.
        Assert.False(TechnologyRules.CastVote(village, bob, TechnologyCatalog.StoneTools, 4, Now));
        var carl = NewCitizen(village, "Carl", Now);
        Assert.True(TechnologyRules.CastVote(village, carl, TechnologyCatalog.HerbLore, 4, Now));

        Assert.False(village.Poll.IsOpen);
        Assert.Equal(TechnologyCatalog.StoneTools, village.Poll.Winner);
        Assert.Equal(4, village.Poll.Electorate);
        Assert.Equal(TechnologyCatalog.StoneTools, TechnologyRules.Current(village)!.Id);
        Assert.Equal("poll.notOpen",
            Assert.Throws<GameRuleException>(() => TechnologyRules.CastVote(village, bob, TechnologyCatalog.HerbLore, 4, Now)).Code);
    }

    [Fact]
    public void Poll_TieGoesToTheEarlierTechnology_AndCancelKeepsTheChoice()
    {
        var village = NewVillage();
        var ann = NewCitizen(village, "Ann", Now);
        var bob = NewCitizen(village, "Bob", Now);
        Choose(village, TechnologyCatalog.HerbLore);

        TechnologyRules.OpenPoll(village, [TechnologyCatalog.Weaving, TechnologyCatalog.StoneTools], 2, Now);
        TechnologyRules.CancelPoll(village, Now);

        Assert.Null(village.Poll!.Winner);
        Assert.Equal(TechnologyCatalog.HerbLore, TechnologyRules.Current(village)!.Id);

        TechnologyRules.OpenPoll(village, [TechnologyCatalog.Weaving, TechnologyCatalog.StoneTools], 2, Now);
        TechnologyRules.CastVote(village, ann, TechnologyCatalog.Weaving, 2, Now);
        TechnologyRules.CastVote(village, bob, TechnologyCatalog.StoneTools, 2, Now);

        Assert.Equal(TechnologyCatalog.StoneTools, TechnologyRules.Current(village)!.Id);
    }

    [Fact]
    public void CurrentResearch_EndsWhenDiscovered()
    {
        var village = NewVillage();
        Assert.Null(TechnologyRules.Current(village));

        Choose(village, TechnologyCatalog.StoneTools);
        Assert.Equal(TechnologyCatalog.StoneTools, TechnologyRules.Current(village)!.Id);

        Discover(village, TechnologyCatalog.StoneTools);
        Assert.Null(TechnologyRules.Current(village));
    }

    [Fact]
    public void Research_NeedsTheBuildingAndTheSkills()
    {
        var village = NewVillage();
        var weak = NewCitizen(village);

        Assert.Equal("research.none",
            Assert.Throws<GameRuleException>(() => CycleEngine.StartResearch(weak, Context(village), Now)).Code);
        Choose(village, TechnologyCatalog.StoneTools);
        Assert.Equal("research.needsBuilding",
            Assert.Throws<GameRuleException>(() => CycleEngine.StartResearch(weak, Context(village), Now)).Code);

        village.Storages.Add(StorageBuilding.Create(StorageKind.StonePile, "9,11", Now));
        Assert.Equal("research.skillRequired",
            Assert.Throws<GameRuleException>(() => CycleEngine.StartResearch(weak, Context(village), Now)).Code);

        var strong = NewCitizen(village, (Stat.Strength, 2));
        CycleEngine.StartResearch(strong, Context(village), Now);
        Assert.Equal(TechnologyCatalog.ResearchTaskId, strong.CurrentTaskId);
        Assert.Equal("9,11", strong.CurrentSiteId);
    }

    [Fact]
    public void Research_NeedsTheWorkAndACrewOfDifferentCitizens()
    {
        var village = NewVillage();
        village.Storages.Add(StorageBuilding.Create(StorageKind.StonePile, "9,11", Now));
        Choose(village, TechnologyCatalog.StoneTools);
        var context = Context(village);
        var first = NewCitizen(village, "Ann", Now, (Stat.Strength, 2));
        var second = NewCitizen(village, "Bob", Now, (Stat.Strength, 2));
        var now = Now;

        var technology = TechnologyCatalog.Get(TechnologyCatalog.StoneTools);
        CycleReport report;
        do
        {
            report = Research(first, context, ref now);
            Assert.Equal(TechnologyCatalog.StoneTools, report.TechnologyId);
        }
        while (TechnologyRules.Remaining(technology, TechnologyRules.ProgressOf(village, technology.Id)!) > 0);

        Assert.False(report.TechnologyDiscovered);
        Assert.False(TechnologyRules.Knows(village, TechnologyCatalog.StoneTools));
        Assert.Equal("research.needOthers",
            Assert.Throws<GameRuleException>(() => CycleEngine.StartResearch(first, context, now)).Code);

        report = Research(second, context, ref now);

        Assert.True(report.TechnologyDiscovered);
        Assert.True(TechnologyRules.Knows(village, TechnologyCatalog.StoneTools));
        Assert.Equal(Era.Camp, TechnologyRules.EraOf(village));
        Assert.Equal(0, report.ProjectContribution);
    }

    [Fact]
    public void Buildings_NeedTheirTechnology()
    {
        var village = NewVillage();

        Assert.Equal("building.needsTechnology",
            Assert.Throws<GameRuleException>(() => ConstructionRules.Place(village, BuildingKind.Farm, 21, 12, Now)).Code);

        Discover(village, TechnologyCatalog.Agriculture);
        var site = ConstructionRules.Place(village, BuildingKind.Farm, 21, 12, Now);

        Assert.Equal(BuildingKind.Farm, site.Kind);
    }

    [Fact]
    public void Roles_NeedTheirTechnology()
    {
        var village = NewVillage();
        village.Structures.Add(new Structure { Kind = BuildingKind.Workshop, PlotId = "21,12", BuiltAt = Now });
        village.Store(ResourceKind.Wood, 10);
        var carpenter = NewCitizen(village, (Stat.Dexterity, 4), (Stat.Strength, 2));

        Assert.Equal("task.needsTechnology",
            Assert.Throws<GameRuleException>(() => CycleEngine.Start(carpenter, Task("saw-planks"), WorkContext.For(village), Now)).Code);

        Discover(village, TechnologyCatalog.Carpentry);
        CycleEngine.Start(carpenter, Task("saw-planks"), WorkContext.For(village), Now);

        Assert.Equal("saw-planks", carpenter.CurrentTaskId);
    }

    [Fact]
    public void Bonuses_RaiseTheYieldOfTheirTasks()
    {
        var plain = NewVillage();
        var skilled = NewVillage();
        Discover(skilled, TechnologyCatalog.StoneTools);

        var withoutTools = NewCitizen(plain);
        var withTools = NewCitizen(skilled);
        CycleEngine.Start(withoutTools, Task("chop-wood"), WorkContext.For(plain), Now);
        CycleEngine.Start(withTools, Task("chop-wood"), WorkContext.For(skilled), Now);

        var before = Finish(withoutTools, WorkContext.For(plain)).Produced.Single().Amount;
        var after = Finish(withTools, WorkContext.For(skilled)).Produced.Single().Amount;

        Assert.Equal(before * 1.25, after, 6);
        Assert.Equal(1, TechnologyRules.YieldMultiplier(skilled, "forage"));
    }

    [Fact]
    public void Catalog_RequirementsAndUnlocksAreConsistent()
    {
        foreach (var technology in TechnologyCatalog.Technologies)
        {
            Assert.All(technology.Requires, required => Assert.NotNull(TechnologyCatalog.Find(required)));
            Assert.All(technology.Requires, required => Assert.True(TechnologyCatalog.Get(required).Era <= technology.Era));
            Assert.NotEmpty(technology.Researcher);
        }

        Assert.All(BuildingCatalog.Buildings.Where(b => b.Technology is not null),
            building => Assert.NotNull(TechnologyCatalog.Find(building.Technology!)));
    }
}
