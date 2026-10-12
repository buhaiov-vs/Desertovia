using Desertod.Domain.Catalog;
using Desertod.Domain.Storage;

namespace Desertod.Domain.Projects;

/// <param name="Storage">Storage building the village gets on the project site when it is finished.</param>
public sealed record ProjectReward(double RenownPool, double Morale, StorageKind? Storage);

/// <param name="SurveyCycles">Zero for small works: building starts right away.</param>
/// <param name="NeedsEngineer">Great works need an Engineer to supervise the builders; small works do not.</param>
public sealed record ProjectDefinition(
    string Id,
    string Name,
    string Description,
    int SurveyCycles,
    IReadOnlyList<ResourceAmount> Materials,
    int BuildCycles,
    ProjectReward Reward,
    bool NeedsEngineer = true);

/// <summary>Projects are started by story quests (<see cref="Quests.StoryQuests"/>), one at a time.</summary>
public static class ProjectCatalog
{
    public const string TsarBridge = "tsar-bridge";
    public const string TsarMonument = "tsar-monument";
    public const string FishingPier = "fishing-pier";
    public const string Palisade = "palisade";
    public const string BellTower = "bell-tower";
    public const string GreatGranary = "granary";

    public static IReadOnlyList<ProjectDefinition> Projects { get; } =
    [
        new(TsarMonument, "Tsar's monument",
            "A stone Tsar by the campfire, so that every passer-by knows whose land this is.",
            0,
            [new(ResourceKind.Stone, 60), new(ResourceKind.Wood, 30)],
            12,
            new ProjectReward(60, 0.03, null),
            NeedsEngineer: false),
        new(FishingPier, "Fishing pier",
            "Planks on poles reaching into the river: fish can be caught from it all year.",
            0,
            [new(ResourceKind.Wood, 40), new(ResourceKind.Reeds, 20)],
            8,
            new ProjectReward(40, 0.02, null),
            NeedsEngineer: false),
        new(Palisade, "Palisade",
            "A fence of sharpened logs around the camp: wolves stay outside.",
            0,
            [new(ResourceKind.Wood, 200)],
            25,
            new ProjectReward(60, 0.03, null),
            NeedsEngineer: false),
        new(TsarBridge, "Tsar Bridge",
            "Connects our town with the monastery across the river. A matter for everyone.",
            10,
            [new(ResourceKind.Wood, 240), new(ResourceKind.Stone, 100), new(ResourceKind.Reeds, 60)],
            60,
            new ProjectReward(500, 0.1, null)),
        new(GreatGranary, "Great Granary",
            "More storage room for grain, wood and stone.",
            5,
            [new(ResourceKind.Wood, 200), new(ResourceKind.Stone, 150), new(ResourceKind.Reeds, 40)],
            40,
            new ProjectReward(300, 0.05, StorageKind.GreatGranary)),
        new(BellTower, "Bell Tower",
            "A bell for the monastery: its ringing carries over both banks.",
            8,
            [new(ResourceKind.Stone, 150), new(ResourceKind.Wood, 80), new(ResourceKind.Gold, 20)],
            40,
            new ProjectReward(400, 0.05, null)),
    ];

    private static readonly Dictionary<string, ProjectDefinition> ById = Projects.ToDictionary(p => p.Id);

    public static ProjectDefinition? Find(string id) => ById.GetValueOrDefault(id);
}
