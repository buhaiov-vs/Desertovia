using Desertod.Domain.Buildings;
using Desertod.Domain.Farming;
using Desertod.Domain.Projects;
using Desertod.Domain.Rules;
using Desertod.Domain.Skills;
using Desertod.Domain.Stats;
using Desertod.Domain.Technologies;
using Desertod.Domain.Weather;
using Desertod.Domain.World;

namespace Desertod.Domain.Catalog;

/// <summary>Roles and tasks. Names are English; clients translate them by identifier.</summary>
public static class GameCatalog
{
    private static StatRequirement Requires(Stat stat, int minimum) => new(stat, minimum);

    private static ResourceAmount Amount(ResourceKind resource, int amount) => new(resource, amount);

    private static readonly ResourceAmount[] None = [];

    /// <summary>Master work needs this level in its craft: the citizen must have done the journeyman work before.</summary>
    private const int MasterSkill = 3;

    public static IReadOnlyList<RoleDefinition> Roles { get; } =
    [
        new("laborer", "Laborer", RoleTier.Basic, []),

        new("miner", "Miner", RoleTier.Journeyman, [Requires(Stat.Strength, 4)], TechnologyCatalog.Mining),
        new("smith", "Smith", RoleTier.Journeyman, [Requires(Stat.Strength, 4), Requires(Stat.Dexterity, 3)], TechnologyCatalog.Smelting),
        new("carpenter", "Carpenter", RoleTier.Journeyman, [Requires(Stat.Dexterity, 4), Requires(Stat.Strength, 2)], TechnologyCatalog.Carpentry),
        new("baker", "Baker", RoleTier.Journeyman,
            [Requires(Stat.Dexterity, 4), Requires(Stat.Endurance, 2), Requires(Stat.Intelligence, 2)], TechnologyCatalog.Baking),
        new("hunter", "Hunter", RoleTier.Journeyman, [Requires(Stat.Dexterity, 4)]),
        new("farmer", "Farmer", RoleTier.Journeyman, [Requires(Stat.Endurance, 4)]),
        new("scholar", "Scholar", RoleTier.Journeyman, [Requires(Stat.Intelligence, 4)], TechnologyCatalog.Writing),
        new("engineer", "Engineer", RoleTier.Journeyman, [Requires(Stat.Intelligence, 4), Requires(Stat.Strength, 2)]),
        new("merchant", "Merchant", RoleTier.Journeyman, [Requires(Stat.Charisma, 4)]),
        new("bard", "Bard", RoleTier.Journeyman, [Requires(Stat.Charisma, 4), Requires(Stat.Dexterity, 2)], TechnologyCatalog.Music),
        new("guard", "Guard", RoleTier.Journeyman, [Requires(Stat.Endurance, 4), Requires(Stat.Strength, 3)]),
        new(WeatherRules.SynopticRole, "Synoptic", RoleTier.Journeyman, [Requires(Stat.Intelligence, 4), Requires(Stat.Endurance, 2)],
            TechnologyCatalog.WeatherLore),

        new("deep-miner", "Deep miner", RoleTier.Master, [Requires(Stat.Strength, 7), Requires(Stat.Endurance, 4)], TechnologyCatalog.Mining),
        new("master-smith", "Master smith", RoleTier.Master, [Requires(Stat.Strength, 7), Requires(Stat.Dexterity, 5)], TechnologyCatalog.Forging),
        new("artisan", "Artisan", RoleTier.Master, [Requires(Stat.Dexterity, 7), Requires(Stat.Intelligence, 4)], TechnologyCatalog.Mechanics),
        new("scout", "Scout", RoleTier.Master, [Requires(Stat.Dexterity, 7), Requires(Stat.Endurance, 4)]),
        new("architect", "Architect", RoleTier.Master, [Requires(Stat.Intelligence, 7), Requires(Stat.Charisma, 3)]),
        new("alchemist", "Alchemist", RoleTier.Master, [Requires(Stat.Intelligence, 7), Requires(Stat.Dexterity, 4)], TechnologyCatalog.Alchemy),
        new("foreman", "Foreman", RoleTier.Master, [Requires(Stat.Charisma, 7), Requires(Stat.Intelligence, 3)]),
        new("envoy", "Envoy", RoleTier.Master, [Requires(Stat.Charisma, 7), Requires(Stat.Intelligence, 4)]),
        new("captain", "Captain", RoleTier.Master, [Requires(Stat.Endurance, 7), Requires(Stat.Strength, 5)]),
    ];

    public const string CarryToStorage = "carry-to-storage";

    /// <summary>Workplace of farm work: the beds of the village farms.</summary>
    public const string Fields = "fields";

    /// <summary>Sowing is open to everyone; each crop needs its Endurance level, which also sets the task's challenge.</summary>
    private static TaskDefinition Sow(string id, string name, CropKind crop) =>
        new(id, name, "laborer", Math.Max(1, FarmCatalog.Get(crop).Endurance), Stat.Endurance, Stat.Intelligence, Skill.Farming, TaskEffect.Sow, 0,
            None, None, Fields, BuildingKind.Farm, Crop: crop);

    public static IReadOnlyList<TaskDefinition> Tasks { get; } =
    [
        new("chop-wood", "Gather wood", "laborer", 1, Stat.Strength, Stat.Endurance, Skill.Woodcutting, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Wood, 5)], None, "forest"),
        new("quarry-stone", "Gather stones", "laborer", 1, Stat.Strength, Stat.Endurance, Skill.Quarrying, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Stone, 4)], None, "stones"),
        new("forage", "Forage berries", "laborer", 1, Stat.Dexterity, Stat.Intelligence, Skill.Gathering, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 4)], None, "berries"),
        new("cut-reeds", "Cut reeds", "laborer", 1, Stat.Dexterity, Stat.Endurance, Skill.Gathering, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Reeds, 6)], None, "reeds"),
        new("hunt", "Hunt rabbits", "laborer", 1, Stat.Dexterity, Stat.Endurance, Skill.Hunting, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 6), Amount(ResourceKind.Leather, 1)], None, "hunting",
            Hunt: new(AnimalKind.Rabbit, 3)),
        new("track-game", "Hunt deer", "laborer", 2, Stat.Dexterity, Stat.Endurance, Skill.Hunting, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 12), Amount(ResourceKind.Leather, 4)], None, "deer-woods",
            WorkSeconds: 180, Hunt: new(AnimalKind.Deer, 1, Dexterity: 2)),
        new("hunt-birds", "Hunt birds", "laborer", 3, Stat.Dexterity, Stat.Intelligence, Skill.Hunting, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 1), Amount(ResourceKind.Feathers, 4)], None, "bird-grove",
            WorkSeconds: 45, Hunt: new(AnimalKind.Bird, 3, Dexterity: 3)),
        new("tend-fire", "Tend the fire", "laborer", 1, Stat.Charisma, Stat.Endurance, Skill.Entertaining, TaskEffect.Morale, 0.5,
            None, None, WorldMap.Camp),
        new("fish", "Fish from the pier", "laborer", 1, Stat.Dexterity, Stat.Endurance, Skill.Fishing, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 3)], None, WorldMap.Pier, RequiredProject: ProjectCatalog.FishingPier),
        new(GoldVein.DigTask, "Dig for gold", "laborer", 1, Stat.Strength, Stat.Endurance, Skill.Mining, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Gold, 1)], None, WorldMap.GoldVein, RequiredProject: ProjectCatalog.TsarBridge),
        new("measure-river", "Measure the river", "laborer", 1, Stat.Intelligence, Stat.Strength, Skill.Scholarship, TaskEffect.Resources, 0,
            [Amount(ResourceKind.ResearchPoints, 1)], None, WorldMap.Ford),
        new(CarryToStorage, "Carry to storage", "laborer", 1, Stat.Endurance, Stat.Strength, Skill.Hauling, TaskEffect.Unload, 0,
            None, None, WorldMap.Camp, WorkSeconds: 15),
        new("tend-fields", "Tend the fields", "laborer", 1, Stat.Endurance, Stat.Dexterity, Skill.Farming, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 2)], None, Fields, BuildingKind.Farm),
        Sow("sow-turnips", "Sow turnips", CropKind.Turnip),
        Sow("sow-carrots", "Sow carrots", CropKind.Carrot),
        Sow("sow-cabbage", "Sow cabbage", CropKind.Cabbage),
        Sow("sow-flax", "Sow flax", CropKind.Flax),
        Sow("sow-rye", "Sow rye", CropKind.Rye),
        new("water-fields", "Water the fields", "laborer", 1, Stat.Endurance, Stat.Strength, Skill.Farming, TaskEffect.Water, 0,
            None, None, Fields, BuildingKind.Farm),
        new("harvest", "Harvest", "laborer", 1, Stat.Endurance, Stat.Dexterity, Skill.Farming, TaskEffect.Harvest, 0,
            None, None, Fields, BuildingKind.Farm),
        new("read-books", "Read the chronicles", "laborer", 1, Stat.Intelligence, Stat.Charisma, Skill.Scholarship, TaskEffect.Resources, 0,
            [Amount(ResourceKind.ResearchPoints, 2)], None, "library", BuildingKind.Library),
        new("chat-tavern", "Chat at the tavern", "laborer", 1, Stat.Charisma, Stat.Intelligence, Skill.Entertaining, TaskEffect.Morale, 0.5,
            None, None, "tavern", BuildingKind.Tavern),
        new("brew-vigor", "Brew vigor drinks", "laborer", 1, Stat.Intelligence, Stat.Charisma, Skill.Scholarship, TaskEffect.Resources, 0,
            [Amount(ResourceKind.VigorDrink, 1)], [Amount(ResourceKind.Grain, 2), Amount(ResourceKind.Food, 1)], "tavern", BuildingKind.Tavern,
            RequiredTechnology: TechnologyCatalog.VigorDrinks),
        new("deliver-wood", "Deliver wood", "laborer", 1, Stat.Endurance, Stat.Strength, Skill.Hauling, TaskEffect.Deliver, 0,
            [Amount(ResourceKind.Wood, 6)], None, "project"),
        new("deliver-stone", "Deliver stone", "laborer", 1, Stat.Strength, Stat.Endurance, Skill.Hauling, TaskEffect.Deliver, 0,
            [Amount(ResourceKind.Stone, 4)], None, "project"),
        new("deliver-reeds", "Deliver reeds", "laborer", 1, Stat.Endurance, Stat.Dexterity, Skill.Hauling, TaskEffect.Deliver, 0,
            [Amount(ResourceKind.Reeds, 8)], None, "project"),
        new("deliver-gold", "Deliver gold", "laborer", 1, Stat.Endurance, Stat.Strength, Skill.Hauling, TaskEffect.Deliver, 0,
            [Amount(ResourceKind.Gold, 2)], None, "project"),
        new("deliver-planks", "Deliver planks", "laborer", 1, Stat.Endurance, Stat.Strength, Skill.Hauling, TaskEffect.Deliver, 0,
            [Amount(ResourceKind.Planks, 4)], None, "project"),
        new("deliver-iron", "Deliver iron", "laborer", 1, Stat.Strength, Stat.Endurance, Skill.Hauling, TaskEffect.Deliver, 0,
            [Amount(ResourceKind.IronIngot, 2)], None, "project"),
        new("build", "Build", "laborer", 1, Stat.Strength, Stat.Endurance, Skill.Building, TaskEffect.Build, 1,
            None, None, "project"),
        new("expedition", "Go on an expedition", "laborer", 1, Stat.Dexterity, Stat.Endurance, Skill.Exploring, TaskEffect.Expedition, 1,
            None, None, "gate"),
        new("roam", "Roam the land", "laborer", 1, Stat.Dexterity, Stat.Endurance, Skill.Exploring, TaskEffect.Roam, 0,
            None, None, WorldMap.Wilds),
        new("tell-stories", "Tell stories by the fire", "laborer", 1, Stat.Intelligence, Stat.Charisma, Skill.Entertaining, TaskEffect.Morale, 0.25,
            None, None, WorldMap.Camp),
        new("study-plants", "Study plants", "laborer", 1, Stat.Intelligence, Stat.Dexterity, Skill.Gathering, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 2)], None, "berries"),
        new("explore-land", "Explore the land", "laborer", 1, Stat.Intelligence, Stat.Endurance, Skill.Exploring, TaskEffect.Roam, 0,
            None, None, WorldMap.Wilds),
        new("read-sky", "Read the sky", "laborer", 1, Stat.Intelligence, Stat.Endurance, Skill.Weatherlore, TaskEffect.Resources, 0,
            [Amount(ResourceKind.ResearchPoints, 1)], None, WorldMap.Camp),

        new("mine-coal", "Mine coal", "miner", 4, Stat.Strength, Stat.Endurance, Skill.Mining, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Coal, 2)], None, "mine", BuildingKind.Mine),
        new("mine-iron-ore", "Mine iron ore", "miner", 4, Stat.Strength, Stat.Endurance, Skill.Mining, TaskEffect.Resources, 0,
            [Amount(ResourceKind.IronOre, 1)], None, "mine", BuildingKind.Mine),
        new("smelt-ingot", "Smelt iron", "smith", 4, Stat.Strength, Stat.Dexterity, Skill.Smithing, TaskEffect.Resources, 0,
            [Amount(ResourceKind.IronIngot, 1)], [Amount(ResourceKind.IronOre, 1), Amount(ResourceKind.Coal, 1)], "smithy", BuildingKind.Smithy),
        new("saw-planks", "Saw planks", "carpenter", 4, Stat.Dexterity, Stat.Strength, Skill.Carpentry, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Planks, 2)], [Amount(ResourceKind.Wood, 2)], "workshop", BuildingKind.Workshop),
        new("bake-bread", "Bake bread", "baker", 4, Stat.Dexterity, Stat.Endurance, Skill.Baking, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Food, 8)], [Amount(ResourceKind.Grain, 2)], "bakery", BuildingKind.Bakery),
        new("research", "Research", "scholar", 4, Stat.Intelligence, Stat.Charisma, Skill.Scholarship, TaskEffect.Resources, 0,
            [Amount(ResourceKind.ResearchPoints, 4)], None, "library", BuildingKind.Library),
        new("survey", "Survey the site", "engineer", 4, Stat.Intelligence, Stat.Dexterity, Skill.Building, TaskEffect.Survey, 1,
            None, None, "project"),
        new("supervise", "Supervise construction", "engineer", 4, Stat.Intelligence, Stat.Charisma, Skill.Building, TaskEffect.Supervise, 0,
            None, None, "project"),
        new("trade", "Trade", "merchant", 4, Stat.Charisma, Stat.Intelligence, Skill.Trading, TaskEffect.NotAvailableYet, 0,
            None, None, "market", BuildingKind.Market),
        new("perform", "Play the gusli", "bard", 4, Stat.Charisma, Stat.Dexterity, Skill.Entertaining, TaskEffect.Morale, 1,
            None, None, "stage", BuildingKind.Stage),
        new("patrol", "Patrol", "guard", 4, Stat.Endurance, Stat.Strength, Skill.Guarding, TaskEffect.NotAvailableYet, 0,
            None, None, "barracks", BuildingKind.Barracks),

        new("mine-gold", "Mine gold", "deep-miner", 7, Stat.Strength, Stat.Endurance, Skill.Mining, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Gold, 1)], None, "mine", BuildingKind.Mine, SkillLevel: MasterSkill),
        new("forge-chain", "Forge chains", "master-smith", 7, Stat.Strength, Stat.Dexterity, Skill.Smithing, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Chain, 1)], [Amount(ResourceKind.IronIngot, 2), Amount(ResourceKind.Coal, 1)], "smithy", BuildingKind.Smithy,
            SkillLevel: MasterSkill),
        new("build-mechanism", "Build mechanisms", "artisan", 7, Stat.Dexterity, Stat.Intelligence, Skill.Carpentry, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Mechanism, 1)], [Amount(ResourceKind.Planks, 2), Amount(ResourceKind.IronIngot, 1)], "workshop", BuildingKind.Workshop,
            SkillLevel: MasterSkill),
        new("explore", "Long expedition", "scout", 7, Stat.Dexterity, Stat.Endurance, Skill.Exploring, TaskEffect.Expedition, 2,
            None, None, "gate", SkillLevel: MasterSkill),
        new("design", "Draft plans", "architect", 7, Stat.Intelligence, Stat.Charisma, Skill.Building, TaskEffect.NotAvailableYet, 1,
            None, None, "library", BuildingKind.Library, SkillLevel: MasterSkill),
        new("brew-remedy", "Brew remedies", "alchemist", 7, Stat.Intelligence, Stat.Dexterity, Skill.Scholarship, TaskEffect.Resources, 0,
            [Amount(ResourceKind.Remedy, 1)], [Amount(ResourceKind.Food, 2)], "library", BuildingKind.Library, SkillLevel: MasterSkill),
        new("lead-crew", "Lead a crew", "foreman", 7, Stat.Charisma, Stat.Intelligence, Skill.Building, TaskEffect.NotAvailableYet, 0,
            None, None, "project", SkillLevel: MasterSkill),
        new("negotiate", "Negotiate", "envoy", 7, Stat.Charisma, Stat.Intelligence, Skill.Trading, TaskEffect.NotAvailableYet, 0,
            None, None, "market", BuildingKind.Market, SkillLevel: MasterSkill),
        new("command", "Command", "captain", 7, Stat.Endurance, Stat.Strength, Skill.Guarding, TaskEffect.NotAvailableYet, 0,
            None, None, "barracks", BuildingKind.Barracks, SkillLevel: MasterSkill),
    ];

    private static readonly Dictionary<string, RoleDefinition> RolesById = Roles.ToDictionary(r => r.Id);
    private static readonly Dictionary<string, TaskDefinition> TasksById = Tasks.ToDictionary(t => t.Id);

    public static RoleDefinition? FindRole(string id) => RolesById.GetValueOrDefault(id);

    public static TaskDefinition? FindTask(string id) => TasksById.GetValueOrDefault(id);

    public static RoleDefinition RoleOf(TaskDefinition task) => RolesById[task.RoleId];

    public static bool CanPerform(StatBlock stats, TaskDefinition task) =>
        RoleOf(task).IsQualified(stats) && HasHuntingSkill(stats, task) && HasFarmingSkill(stats, task);

    public static bool HasHuntingSkill(StatBlock stats, TaskDefinition task) =>
        task.Hunt is null || stats.Get(Stat.Dexterity) >= task.Hunt.Dexterity;

    public static bool HasFarmingSkill(StatBlock stats, TaskDefinition task) =>
        task.Crop is not { } crop || stats.Get(Stat.Endurance) >= FarmCatalog.Get(crop).Endurance;

    /// <summary>Highest-tier qualified role; ties broken by the strongest primary requirement stat.</summary>
    public static RoleDefinition TitleFor(StatBlock stats) =>
        Roles.Where(r => r.IsQualified(stats))
            .OrderByDescending(r => r.Tier)
            .ThenByDescending(r => r.Requirements.Count == 0 ? 0 : stats.Get(r.Requirements[0].Stat))
            .First();
}
