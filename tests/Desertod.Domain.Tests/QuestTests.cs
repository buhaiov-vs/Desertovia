using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Quests;
using Desertod.Domain.Rules;
using Desertod.Domain.Storage;
using static Desertod.Domain.Tests.TestWorld;

namespace Desertod.Domain.Tests;

public class QuestTests
{
    private static WorkContext Context(Village village) => new() { Village = village, RecentCitizenCount = 1 };

    private static CycleReport Work(Citizen citizen, string taskId, WorkContext context, ref DateTimeOffset now)
    {
        CycleEngine.Start(citizen, Task(taskId), context, now);
        now = citizen.CycleEndsAt!.Value;
        return CycleEngine.Settle(citizen, context, now)!;
    }

    private static CycleReport Job(Citizen citizen, string plotId, int stage, string jobId, WorkContext context, ref DateTimeOffset now)
    {
        CycleEngine.StartJob(citizen, plotId, stage, jobId, context, now);
        now = citizen.CycleEndsAt!.Value;
        return CycleEngine.Settle(citizen, context, now)!;
    }

    private static QuestObjective Objective(VillageQuest quest, Village village, int citizens, string id) =>
        QuestRules.Objectives(quest, village, citizens).Single(o => o.Id == id);

    [Fact]
    public void Cold_OpensWhenBedsAreShort_AndShelterBuildersShareTheReward()
    {
        var village = NewVillage();
        village.Store(ResourceKind.Wood, 20);
        village.Store(ResourceKind.Reeds, 20);
        var builder = NewCitizen(village, "Builder", Now);
        var idler = NewCitizen(village, "Idler", Now.AddMinutes(1));
        var needs = new VillageNeeds(2, 0);

        var quest = Assert.Single(QuestRules.Open(village, [], needs, null, Now));
        Assert.Equal(QuestKind.Cold, quest.Kind);
        Assert.False(Objective(quest, village, 2, "plan-shelter").Done);

        ConstructionRules.Place(village, BuildingKind.LeanTo, 9, 11, Now);
        Assert.True(Objective(quest, village, 2, "plan-shelter").Done);
        Assert.False(QuestRules.IsComplete(quest, village, 2));

        var context = Context(village);
        var now = Now;
        foreach (var (stage, jobId) in new[]
                 {
                     (0, "clear-ground"), (0, "bring-sticks"), (0, "bring-sticks"), (0, "lean-poles"), (0, "lean-poles"),
                     (1, "bring-reeds"), (1, "bring-reeds"), (1, "lay-cover"), (1, "lay-cover"),
                 })
        {
            QuestRules.Credit(village, [quest], Job(builder, "9,11", stage, jobId, context, ref now));
        }

        Assert.True(QuestRules.IsComplete(quest, village, 2));
        var morale = village.Morale;
        var completion = QuestRules.Complete(quest, village, new Dictionary<Guid, Citizen> { [builder.Id] = builder, [idler.Id] = idler }, now);

        Assert.Equal(QuestStatus.Completed, quest.Status);
        Assert.Equal(QuestRules.Rewards[QuestKind.Cold].RenownPool, completion.RenownByCitizen[builder.Id], 6);
        Assert.False(completion.RenownByCitizen.ContainsKey(idler.Id));
        Assert.Equal(morale + QuestRules.Rewards[QuestKind.Cold].Morale, village.Morale, 6);
    }

    [Fact]
    public void Hunger_NeedsAFoodStore_AndStockedFood()
    {
        var village = Village.Create("v", "V", Guid.NewGuid(), Now);
        var citizen = NewCitizen(village);
        var needs = new VillageNeeds(1, 1);

        var quest = Assert.Single(QuestRules.Open(village, [], needs, null, Now), q => q.Kind == QuestKind.Hunger);
        Assert.Equal(QuestRules.FoodMinimum, quest.Target);
        Assert.False(Objective(quest, village, 1, "plan-food-storage").Done);

        village.Storages.Add(StorageBuilding.Create(StorageKind.DryingRack, "9,11", Now));
        Assert.True(Objective(quest, village, 1, "build-food-storage").Done);
        Assert.False(Objective(quest, village, 1, "stock-food").Done);

        var context = Context(village);
        var now = Now;
        for (var cycle = 0; cycle < 100 && !QuestRules.IsComplete(quest, village, 1); cycle++)
        {
            QuestRules.Credit(village, [quest], Work(citizen, "forage", context, ref now));
        }

        Assert.True(QuestRules.IsComplete(quest, village, 1));
        var completion = QuestRules.Complete(quest, village, new Dictionary<Guid, Citizen> { [citizen.Id] = citizen }, now);
        Assert.Equal(QuestRules.Rewards[QuestKind.Hunger].RenownPool, completion.RenownByCitizen[citizen.Id], 6);
    }

    [Fact]
    public void Storage_OpensOncePerGood_AndClosesWhenThereIsRoom()
    {
        var village = Village.Create("v", "V", Guid.NewGuid(), Now);
        village.Trees.Clear();
        village.Store(ResourceKind.Food, 10);
        village.Store(ResourceKind.Wood, 30);
        var citizen = NewCitizen(village);
        var needs = new VillageNeeds(1, 0);
        var now = Now;

        var report = Work(citizen, "chop-wood", Context(village), ref now);
        Assert.NotEmpty(report.Carried);

        var quest = Assert.Single(QuestRules.Open(village, [], needs, report, now), q => q.Kind == QuestKind.Storage);
        Assert.Equal(ResourceKind.Wood, quest.Resource);
        Assert.DoesNotContain(QuestRules.Open(village, [quest], needs, report, now), q => q.Kind == QuestKind.Storage);
        Assert.False(Objective(quest, village, 1, "plan-storage").Done);

        ConstructionRules.Place(village, BuildingKind.WoodPile, 9, 11, now);
        Assert.True(Objective(quest, village, 1, "plan-storage").Done);
        Assert.False(QuestRules.IsComplete(quest, village, 1));

        village.Sites.Clear();
        village.Storages.Add(StorageBuilding.Create(StorageKind.WoodPile, "9,11", now));
        Assert.True(QuestRules.IsComplete(quest, village, 1));
    }

    [Fact]
    public void SolvedProblems_DoNotOpenQuests()
    {
        var village = NewVillage();
        village.Structures.Add(new Structure { Kind = BuildingKind.Hut, PlotId = "9,11", BuiltAt = Now });

        Assert.Empty(QuestRules.Open(village, [], new VillageNeeds(4, 1), null, Now));
    }
}
