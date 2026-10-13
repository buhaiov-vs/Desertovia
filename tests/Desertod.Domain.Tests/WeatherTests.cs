using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Farming;
using Desertod.Domain.Rules;
using Desertod.Domain.Skills;
using Desertod.Domain.Stats;
using Desertod.Domain.Technologies;
using Desertod.Domain.Weather;
using Desertod.Domain.World;
using static Desertod.Domain.Tests.TestWorld;

namespace Desertod.Domain.Tests;

public class WeatherTests
{
    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
    }

    private static Village SynopticVillage()
    {
        var village = NewVillage();
        village.Forecasts.Clear();
        village.Research.Add(new TechnologyProgress { TechnologyId = TechnologyCatalog.WeatherLore, ResearchedAt = Now });
        return village;
    }

    private static Citizen Synoptic(Village village) => NewCitizen(village, (Stat.Intelligence, 4), (Stat.Endurance, 2));

    [Fact]
    public void Sky_RainsOnFiveToTenPercentOfDays()
    {
        var village = Village.Create("v", "V", Guid.NewGuid(), Now);
        const int days = 20000;

        var rainy = Enumerable.Range(0, days).Count(day => WeatherRules.IsRaining(village, day));

        Assert.InRange((double)rainy / days, 0.06, 0.09);
        Assert.All(Enumerable.Range(0, 100), day => Assert.InRange(WeatherRules.RainChance(village, day), 0.05, 0.10));
    }

    [Fact]
    public void Sky_IsTheSameOnEveryRead()
    {
        var village = Village.Create("v", "V", Guid.NewGuid(), Now);

        Assert.All(Enumerable.Range(0, 200), day => Assert.Equal(WeatherRules.SkyRains(village, day), WeatherRules.SkyRains(village, day)));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Forecast_BringsTheOppositeWeather_WhenItWorks(bool saysRain, bool rains)
    {
        var village = SynopticVillage();
        var synoptic = Synoptic(village);

        var forecast = WeatherRules.Forecast(village, synoptic, saysRain, new FixedRandom(0), Now);

        Assert.Equal(WeatherRules.DayOf(Now) + 1, forecast.Day);
        Assert.Equal(rains, WeatherRules.IsRaining(village, forecast.Day));
        Assert.Equal(rains, WeatherRules.IsRaining(village, WeatherRules.DayStart(forecast.Day)));
    }

    [Fact]
    public void Forecast_ThatFails_LeavesTheSkyAlone()
    {
        var village = SynopticVillage();
        var synoptic = Synoptic(village);
        var day = WeatherRules.DayOf(Now) + 1;

        WeatherRules.Forecast(village, synoptic, true, new FixedRandom(0.99), Now);

        Assert.Equal(WeatherRules.SkyRains(village, day), WeatherRules.IsRaining(village, day));
    }

    [Fact]
    public void Weatherlore_MakesTheForecastMoreSurelyWrong()
    {
        Assert.Equal(WeatherRules.MinimumReliability, WeatherRules.Reliability(0), 6);
        Assert.Equal(WeatherRules.MaximumReliability, WeatherRules.Reliability(GameRules.SkillMaximum), 6);
        Assert.True(WeatherRules.Reliability(5) > WeatherRules.Reliability(4));
    }

    [Fact]
    public void Forecast_NeedsWeatherLore_AndASynoptic()
    {
        var unknown = NewVillage();
        var error = Assert.Throws<GameRuleException>(() => WeatherRules.Forecast(unknown, Synoptic(unknown), true, new FixedRandom(0), Now));
        Assert.Equal("forecast.needsTechnology", error.Code);

        var village = SynopticVillage();
        error = Assert.Throws<GameRuleException>(() => WeatherRules.Forecast(village, NewCitizen(village), true, new FixedRandom(0), Now));
        Assert.Equal("task.roleRequired", error.Code);
    }

    [Fact]
    public void Forecast_ComesRightAfterSleep_OncePerDay()
    {
        var village = SynopticVillage();
        var synoptic = Synoptic(village);
        CycleEngine.Start(synoptic, Task("read-sky"), WorkContext.For(village), Now);

        var error = Assert.Throws<GameRuleException>(() => WeatherRules.Forecast(village, synoptic, true, new FixedRandom(0), Now));
        Assert.Equal("forecast.notRested", error.Code);

        var rested = synoptic.GraceEndsAt!.Value.AddSeconds(1);
        Finish(synoptic, WorkContext.For(village));
        WeatherRules.Forecast(village, synoptic, true, new FixedRandom(0), rested);

        var other = Synoptic(village);
        error = Assert.Throws<GameRuleException>(() => WeatherRules.Forecast(village, other, false, new FixedRandom(0), rested));
        Assert.Equal("forecast.alreadyMade", error.Code);
    }

    [Fact]
    public void ReadingTheSky_TrainsWeatherlore()
    {
        var village = NewVillage();
        var citizen = NewCitizen(village);

        CycleEngine.Start(citizen, Task("read-sky"), WorkContext.For(village), Now);
        var report = Finish(citizen, WorkContext.For(village));

        Assert.Equal(Skill.Weatherlore, report.Skill);
        Assert.True(report.SkillExperience > 0);
    }

    [Fact]
    public void Synoptic_IsAJourneymanRole_UnlockedByWeatherLore()
    {
        var role = GameCatalog.FindRole(WeatherRules.SynopticRole)!;

        Assert.Equal(RoleTier.Journeyman, role.Tier);
        Assert.Equal(TechnologyCatalog.WeatherLore, role.Technology);
        Assert.False(WeatherRules.IsSynoptic(NewVillage(), NewCitizen(NewVillage(), (Stat.Intelligence, 4), (Stat.Endurance, 2))));
        var village = SynopticVillage();
        Assert.True(WeatherRules.IsSynoptic(village, Synoptic(village)));
    }

    [Theory]
    [InlineData("fish", WeatherRules.FishingYield)]
    [InlineData("tend-fire", WeatherRules.FireMorale)]
    [InlineData("hunt", WeatherRules.HuntingYield)]
    [InlineData("roam", WeatherRules.HuntingYield)]
    [InlineData("build", WeatherRules.MudYield)]
    [InlineData("deliver-wood", WeatherRules.MudYield)]
    [InlineData("read-books", WeatherRules.IndoorYield)]
    [InlineData("chat-tavern", WeatherRules.IndoorYield)]
    [InlineData("chop-wood", 1)]
    public void Rain_ChangesTheYield(string taskId, double multiplier)
    {
        var village = NewVillage();
        Assert.Equal(1, WeatherRules.YieldMultiplier(village, Task(taskId), Now));

        SetSky(village, rain: true);
        Assert.Equal(multiplier, WeatherRules.YieldMultiplier(village, Task(taskId), Now));
    }

    [Fact]
    public void Rain_MakesOutdoorWorkTiring()
    {
        var village = NewVillage();
        SetSky(village, rain: true);
        var citizen = NewCitizen(village);
        var before = citizen.CurrentEnergy(Now, village);

        CycleEngine.Start(citizen, Task("chop-wood"), WorkContext.For(village), Now);

        Assert.Equal(GameRules.EnergyCostPerCycle * WeatherRules.OutdoorEnergyCost, before - citizen.StoredEnergy, 6);
    }

    [Fact]
    public void Rain_ChillsThoseSleepingUnderTheTrees()
    {
        var sunny = NewVillage();
        var rainy = NewVillage();
        SetSky(rainy, rain: true);
        var dry = NewCitizen(sunny);
        var wet = NewCitizen(rainy);
        foreach (var citizen in new[] { dry, wet })
        {
            citizen.StoredEnergy = 0;
            citizen.EnergyUpdatedAt = Now;
        }

        var later = Now.AddHours(1);

        Assert.True(wet.SleepsUnderTrees);
        Assert.Equal(dry.CurrentEnergy(later, sunny) * WeatherRules.UnderTreesRegeneration, wet.CurrentEnergy(later, rainy), 6);
    }

    [Fact]
    public void Rain_WatersTheBeds()
    {
        var village = NewVillage();
        village.Structures.Add(new Structure { Kind = BuildingKind.Farm, PlotId = "21,12", BuiltAt = Now });
        village.EnsureCamp(Now);
        SetSky(village, rain: true);
        var bed = village.Fields[0];
        bed.Crop = CropKind.Turnip;
        bed.UpdatedAt = Now;
        var later = Now.AddHours(FarmCatalog.Get(CropKind.Turnip).GrowHours / 2);

        Assert.Equal(0.5, FarmRules.GrowthOf(village, bed, later), 6);
        Assert.False(FarmRules.IsThirsty(village, bed, later));
        var error = Assert.Throws<GameRuleException>(() => CycleEngine.Start(NewCitizen(village), Task("water-fields"), WorkContext.For(village), later));
        Assert.Equal("farm.raining", error.Code);
    }

    [Fact]
    public void Rain_RegrowsTheForestFaster()
    {
        var sunny = NewVillage();
        var rainy = NewVillage();
        SetSky(rainy, rain: true);
        var halfway = Now + Forest.RegrowTime / WeatherRules.ForestRegrowth;
        foreach (var village in new[] { sunny, rainy })
        {
            village.Trees.Add(new Tree { X = 2, Y = 2, FelledAt = Now });
        }

        Assert.False(Forest.Stands(sunny, sunny.Trees[0], halfway));
        Assert.True(Forest.Stands(rainy, rainy.Trees[0], halfway));
    }
}
