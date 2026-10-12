using Desertod.Domain.Buildings;
using Desertod.Domain.Farming;
using Desertod.Domain.Stats;
using Desertod.Domain.World;

namespace Desertod.Domain.Catalog;

public enum RoleTier
{
    Basic = 1,
    Journeyman = 2,
    Master = 3,
}

public enum TaskEffect
{
    /// <summary>Produces resources into village storage.</summary>
    Resources,

    /// <summary>Raises village morale.</summary>
    Morale,

    /// <summary>Moves the resource in <see cref="TaskDefinition.Outputs"/> from storage into the active project.</summary>
    Deliver,

    /// <summary>Surveys the active project (first phase).</summary>
    Survey,

    /// <summary>Construction labor on the active project; needs a supervising Engineer.</summary>
    Build,

    /// <summary>Lets laborers build while the cycle runs.</summary>
    Supervise,

    /// <summary>Random loot from outside the village.</summary>
    Expedition,

    /// <summary>Walks around the land; common goods are found on the way by chance.</summary>
    Roam,

    /// <summary>Carries the citizen's load to storage, as far as there is room.</summary>
    Unload,

    /// <summary>Sows <see cref="TaskDefinition.Crop"/> in an empty farm bed.</summary>
    Sow,

    /// <summary>Carries water from the river to the thirstiest farm beds; they grow faster while wet.</summary>
    Water,

    /// <summary>Gathers a ripe farm bed into storage.</summary>
    Harvest,

    /// <summary>Needs streamer events or other villages, which are not implemented yet.</summary>
    NotAvailableYet,
}

public sealed record StatRequirement(Stat Stat, int Minimum);

/// <param name="Technology">The village must have discovered it before anyone can work in this role.</param>
public sealed record RoleDefinition(
    string Id,
    string Name,
    RoleTier Tier,
    IReadOnlyList<StatRequirement> Requirements,
    string? Technology = null)
{
    public bool IsQualified(StatBlock stats) => Requirements.All(r => stats.Get(r.Stat) >= r.Minimum);
}

public sealed record ResourceAmount(ResourceKind Resource, double Amount);

/// <param name="Animals">Animals caught per cycle at a stat factor of 1 while the herd is abundant; the task's outputs are for that catch.</param>
/// <param name="Dexterity">Dexterity level needed to hunt this prey; Dexterity is the hunting skill.</param>
public sealed record HuntDefinition(AnimalKind Prey, double Animals, int Dexterity = 0);

/// <param name="RequiredBuilding">The task can be done only once the village has this building.</param>
/// <param name="WorkSeconds">Time spent working; walking time is added on top.</param>
/// <param name="Hunt">Set for hunting: the catch comes out of the village's herd of this animal.</param>
/// <param name="RequiredProject">The task can be done only once the village has finished this project.</param>
/// <param name="RequiredTechnology">The task can be done only once the village has discovered this technology.</param>
/// <param name="Crop">Set for sowing: the crop put in the bed. Its Endurance level is the farming skill needed.</param>
public sealed record TaskDefinition(
    string Id,
    string Name,
    string RoleId,
    int TierRequirement,
    Stat Primary,
    Stat Secondary,
    TaskEffect Effect,
    double BaseYield,
    IReadOnlyList<ResourceAmount> Outputs,
    IReadOnlyList<ResourceAmount> Inputs,
    string Workplace,
    BuildingKind? RequiredBuilding = null,
    int WorkSeconds = 90,
    HuntDefinition? Hunt = null,
    string? RequiredProject = null,
    string? RequiredTechnology = null,
    CropKind? Crop = null);
