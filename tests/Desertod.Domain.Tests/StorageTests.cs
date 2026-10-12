using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Rules;
using Desertod.Domain.Storage;
using Desertod.Domain.World;

namespace Desertod.Domain.Tests;

public class StorageTests
{
    private static readonly DateTimeOffset Now = TestWorld.Now;

    private static Village NewCamp() => Village.Create("v", "V", Guid.NewGuid(), Now);

    [Fact]
    public void NewTribe_HasOnlyAHeapByTheFire()
    {
        var village = NewCamp();

        var heap = Assert.Single(village.Storages);
        Assert.Equal(StorageKind.Heap, heap.Kind);
        Assert.Equal(WorldMap.HeapPlot, heap.PlotId);
        Assert.Empty(village.Structures);
        Assert.Empty(village.Sites);
        Assert.Equal(40, village.FreeSpace(ResourceKind.Wood), 6);
    }

    [Fact]
    public void Slot_HoldsOneKindOfGood()
    {
        var village = NewCamp();
        foreach (var kind in new[] { ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Food, ResourceKind.Reeds })
        {
            village.Store(kind, 1);
        }

        Assert.Equal(0, village.FreeSpace(ResourceKind.Leather));
        Assert.Equal(0, village.Store(ResourceKind.Leather, 1));
        Assert.Equal(9, village.FreeSpace(ResourceKind.Wood), 6);
    }

    [Fact]
    public void Store_FillsSpecialisedBuildingsBeforeTheHeap()
    {
        var village = NewCamp();
        village.Storages.Add(StorageBuilding.Create(StorageKind.WoodPile, "plot-1", Now));

        village.Store(ResourceKind.Wood, 30);

        Assert.Equal(30, village.Storages.Single(s => s.Kind == StorageKind.WoodPile).Amount(ResourceKind.Wood), 6);
        Assert.Equal(0, village.Storages.Single(s => s.Kind == StorageKind.Heap).Amount(ResourceKind.Wood));
    }

    [Fact]
    public void Take_EmptiesTheHeapFirst()
    {
        var village = NewCamp();
        village.Store(ResourceKind.Wood, 10);
        village.Storages.Add(StorageBuilding.Create(StorageKind.WoodPile, "plot-1", Now));
        village.Store(ResourceKind.Wood, 10);

        village.Take(ResourceKind.Wood, 10);

        Assert.Equal(0, village.Storages.Single(s => s.Kind == StorageKind.Heap).Amount(ResourceKind.Wood));
        Assert.Equal(10, village.Stock(ResourceKind.Wood), 6);
    }

    [Fact]
    public void Knowledge_IsUnlimited_AndNotStoredInBuildings()
    {
        var village = NewCamp();

        Assert.Equal(5000, village.Store(ResourceKind.ResearchPoints, 5000));
        Assert.Equal(5000, village.Knowledge);
        Assert.Empty(village.Storages.Single().Goods);
    }

    [Fact]
    public void DropPoint_IsTheNearestBuildingWithRoom()
    {
        var village = NewCamp();
        var forest = WorldMap.Places["forest"];
        Assert.Equal(WorldMap.Yard(WorldMap.HeapPlot), village.DropPoint(ResourceKind.Wood, forest));

        village.Storages.Add(StorageBuilding.Create(StorageKind.WoodPile, "plot-5", Now));
        Assert.Equal(WorldMap.Yard("plot-5"), village.DropPoint(ResourceKind.Wood, forest));

        village.Store(ResourceKind.Wood, 60);
        Assert.Equal(WorldMap.Yard(WorldMap.HeapPlot), village.DropPoint(ResourceKind.Wood, forest));
    }

    [Fact]
    public void Take_MovesGoodsFromOneBuildingIntoHands_UpToTheLimit()
    {
        var village = NewCamp();
        village.Store(ResourceKind.Food, 10);
        village.Storages.Add(StorageBuilding.Create(StorageKind.Granary, "plot-1", Now));
        village.Store(ResourceKind.Food, 30);
        var citizen = TestWorld.NewCitizen(village);

        Assert.Equal(StorageRules.TakeLimit, StorageRules.Take(citizen, village, "plot-1", ResourceKind.Food, Now), 6);
        Assert.Equal(StorageRules.TakeLimit, citizen.Carried(ResourceKind.Food), 6);
        Assert.Equal(10, village.Storages.Single(s => s.Kind == StorageKind.Heap).Amount(ResourceKind.Food), 6);

        Assert.Equal("take.handsFull",
            Assert.Throws<GameRuleException>(() => StorageRules.Take(citizen, village, "plot-1", ResourceKind.Food, Now)).Code);
        Assert.Equal("take.nothing",
            Assert.Throws<GameRuleException>(() => StorageRules.Take(citizen, village, "plot-1", ResourceKind.Wood, Now)).Code);
        Assert.Equal("take.noStorage",
            Assert.Throws<GameRuleException>(() => StorageRules.Take(citizen, village, "plot-9", ResourceKind.Wood, Now)).Code);
    }

    [Fact]
    public void Take_NeedsAnIdleCitizen()
    {
        var village = TestWorld.NewVillage();
        var citizen = TestWorld.NewCitizen(village);
        CycleEngine.Start(citizen, TestWorld.Task("chop-wood"), WorkContext.For(village), Now);

        Assert.Equal("take.working",
            Assert.Throws<GameRuleException>(() => StorageRules.Take(citizen, village, "plot-2", ResourceKind.Food, Now)).Code);
    }

    [Fact]
    public void Drop_ThrowsAwayOnlyGoodsStorageHasNoRoomFor()
    {
        var village = NewCamp();
        village.Store(ResourceKind.Wood, 30);
        var citizen = TestWorld.NewCitizen(village);
        citizen.Carry(ResourceKind.Food, 5);

        Assert.Equal("drop.roomInStorage",
            Assert.Throws<GameRuleException>(() => StorageRules.Drop(citizen, village)).Code);

        // The last heap slot now holds some food: room for food, none for stone.
        village.Store(ResourceKind.Food, 5);
        citizen.Carry(ResourceKind.Stone, 3);
        StorageRules.Drop(citizen, village);

        Assert.Equal(0, citizen.Carried(ResourceKind.Stone));
        Assert.Equal(5, citizen.Carried(ResourceKind.Food), 6);
    }
}
