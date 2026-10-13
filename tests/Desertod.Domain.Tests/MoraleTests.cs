using Desertod.Domain.Entities;
using Desertod.Domain.Rules;

namespace Desertod.Domain.Tests;

public class MoraleTests
{
    private static readonly DateTimeOffset Now = TestWorld.Now;

    private static Village New() => Village.Create("v", "Sand Town", null, false, Guid.NewGuid(), Now);

    [Fact]
    public void Morale_DriftsBackToOne_AsTimePasses()
    {
        var village = New();
        village.AddMorale(0.2, Now);

        Assert.Equal(1.2, village.MoraleAt(Now), 6);
        Assert.Equal(1.0 + 0.2 * (1 - GameRules.MoraleDriftPerMinute), village.MoraleAt(Now.AddMinutes(1)), 6);
        Assert.Equal(1.0, village.MoraleAt(Now.AddDays(1)), 6);
    }

    [Fact]
    public void AddMorale_StartsFromTheDriftedValue()
    {
        var village = New();
        village.AddMorale(-0.2, Now);
        var later = Now.AddMinutes(10);
        var drifted = village.MoraleAt(later);

        village.AddMorale(0.05, later);

        Assert.True(drifted > 0.8);
        Assert.Equal(drifted + 0.05, village.MoraleAt(later), 6);
    }
}
