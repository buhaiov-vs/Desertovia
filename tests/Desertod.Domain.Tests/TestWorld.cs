using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Rules;
using Desertod.Domain.Stats;
using Desertod.Domain.Storage;
using Desertod.Domain.Weather;

namespace Desertod.Domain.Tests;

internal static class TestWorld
{
    public static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A camp with extra general storage and a granary of food, so tests are not about hunger or space.
    /// Its land has no trees, so blueprints never get a felling job.
    /// </summary>
    public static Village NewVillage(bool fed = true)
    {
        var village = Village.Create("v", "V", Guid.NewGuid(), Now);
        village.Trees.Clear();
        village.Storages.Add(StorageBuilding.Create(StorageKind.TownHall, "13,5", Now));
        village.Storages.Add(StorageBuilding.Create(StorageKind.Granary, "17,5", Now));
        if (fed)
        {
            village.Store(ResourceKind.Food, 200);
        }

        SetSky(village, rain: false);
        return village;
    }

    /// <summary>Forecasts the same weather for every day within two weeks of <see cref="Now"/>, so the sky's own draw never decides a test.</summary>
    public static void SetSky(Village village, bool rain)
    {
        village.Forecasts.Clear();
        var today = WeatherRules.DayOf(Now);
        var days = (long)(TimeSpan.FromDays(14) / WeatherRules.DayLength);
        for (var day = today - days; day <= today + days; day++)
        {
            village.Forecasts.Add(new WeatherForecast { Day = day, Rain = rain, SaysRain = !rain, MadeAt = Now });
        }
    }

    public static Citizen NewCitizen(Village village, params (Stat Stat, int Level)[] stats) =>
        NewCitizen(village, "Bob", Now, stats);

    public static Citizen NewCitizen(Village village, string name, DateTimeOffset joinedAt, params (Stat Stat, int Level)[] stats)
    {
        var citizen = CharacterCreation.Create(village.Id, Guid.NewGuid(), name, joinedAt);
        foreach (var (stat, level) in stats)
        {
            citizen.Stats.Set(stat, level);
        }

        citizen.StoredEnergy = citizen.MaximumEnergy;
        return citizen;
    }

    public static TaskDefinition Task(string id) => GameCatalog.FindTask(id)!;

    /// <summary>Settles the running cycle at the moment it ends.</summary>
    public static CycleReport Finish(Citizen citizen, WorkContext context) =>
        CycleEngine.Settle(citizen, context, citizen.CycleEndsAt!.Value)!;
}
