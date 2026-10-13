using Desertod.Domain.Catalog;

namespace Desertod.Domain.Rules;

/// <summary>Wandering the land around the camp and picking up whatever common goods lie on the way.</summary>
public static class Roaming
{
    /// <summary>Chances per cycle to spot something on the way.</summary>
    public const int Glances = 6;

    public static IReadOnlyList<LootEntry> Table { get; } =
    [
        new(ResourceKind.Wood, 2, 30),
        new(ResourceKind.Stone, 1, 25),
        new(ResourceKind.Food, 2, 25),
        new(ResourceKind.Reeds, 2, 20),
    ];

    /// <summary>25% + 2.5% per Dexterity: a keen eye finds twice as much at the stat maximum.</summary>
    public static double FindChance(int dexterity) => 0.25 + 0.025 * dexterity;
}
