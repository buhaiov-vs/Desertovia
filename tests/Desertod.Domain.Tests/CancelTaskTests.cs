using Desertod.Domain.Entities;
using Desertod.Domain.Rules;
using static Desertod.Domain.Tests.TestWorld;

namespace Desertod.Domain.Tests;

public class CancelTaskTests
{
    [Fact]
    public void Cancelled_Task_EarnsNothing_AndLeavesTheCitizenIdle()
    {
        var village = NewVillage();
        var citizen = NewCitizen(village);
        var energy = citizen.CurrentEnergy(Now, village);
        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), Now);
        var later = Now.AddSeconds(10);
        var spent = citizen.CurrentEnergy(later, village);

        CycleEngine.Cancel(citizen, later);

        Assert.Equal(WorkState.Idle, citizen.WorkState(later));
        Assert.True(citizen.CycleSettled);
        Assert.Null(citizen.CurrentTaskId);
        Assert.Null(CycleEngine.Settle(citizen, WorkContext.For(village), later.AddHours(1)));
        Assert.Equal(spent, citizen.CurrentEnergy(later, village));
        Assert.True(spent < energy);
        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), later);
    }

    [Fact]
    public void Cancelling_ARenewal_KeepsTheStreak()
    {
        var village = NewVillage();
        var citizen = NewCitizen(village);
        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), Now);
        var ended = citizen.CycleEndsAt!.Value;
        CycleEngine.Settle(citizen, WorkContext.For(village), ended);
        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), ended);
        Assert.Equal(1, citizen.Streak);

        var later = ended.AddSeconds(10);
        CycleEngine.Cancel(citizen, later);
        Assert.Equal(0, citizen.Streak);

        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), later);
        Assert.Equal(1, citizen.Streak);
    }

    [Fact]
    public void Cancelling_AFirstTask_GrantsNoStreak()
    {
        var village = NewVillage();
        var citizen = NewCitizen(village);
        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), Now);

        CycleEngine.Cancel(citizen, Now.AddSeconds(10));
        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), Now.AddSeconds(11));

        Assert.Equal(0, citizen.Streak);
    }

    [Fact]
    public void Nothing_Running_CannotBeCancelled()
    {
        var village = NewVillage();
        var citizen = NewCitizen(village);

        var error = Assert.Throws<GameRuleException>(() => CycleEngine.Cancel(citizen, Now));
        Assert.Equal("task.notRunning", error.Code);

        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), Now);
        error = Assert.Throws<GameRuleException>(() => CycleEngine.Cancel(citizen, citizen.CycleEndsAt!.Value));
        Assert.Equal("task.notRunning", error.Code);
    }
}
