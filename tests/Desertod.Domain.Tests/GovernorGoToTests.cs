using Desertod.Domain.Rules;
using Desertod.Domain.World;
using static Desertod.Domain.Tests.TestWorld;

namespace Desertod.Domain.Tests;

public class GovernorGoToTests
{
    [Fact]
    public void Governor_StandsAtTheCampfire_UntilSentSomewhere()
    {
        var village = NewVillage();
        Assert.Null(village.GovernorSpot);

        village.GovernorGoTo(new TilePoint(3.5, 4.25));

        Assert.Equal(new TilePoint(3.5, 4.25), village.GovernorSpot);
    }

    [Fact]
    public void SpotBeyondTheLand_IsRejected()
    {
        var village = NewVillage();
        var land = WorldMap.Land;

        var error = Assert.Throws<GameRuleException>(() => village.GovernorGoTo(new TilePoint(land.X + land.Width + 1, land.Y)));

        Assert.Equal("spot.outsideLand", error.Code);
        Assert.Null(village.GovernorSpot);
    }
}
