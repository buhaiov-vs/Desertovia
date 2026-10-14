using Desertod.Domain.Catalog;

namespace Desertod.Domain.Combat;

public enum EnemyKind
{
    Wolf,
    Boar,
    Bear,
    Zombie,
    Skeleton,
}

/// <param name="Power">Fighting strength of one enemy; compare <see cref="CombatRules.Power"/> of a citizen.</param>
/// <param name="AmbushWeight">How often it is the one attacking, among the enemies a task lists.</param>
/// <param name="Loot">Goods one slain enemy leaves.</param>
/// <param name="Steals">Goods one raider gets away with when the village loses.</param>
/// <param name="Damage">Damage one raider does to a building when the village loses.</param>
/// <param name="InjuryMinutes">How long a citizen it wounds cannot work.</param>
/// <param name="Undead">Called by the Tsar; beasts come by themselves.</param>
public sealed record EnemyDefinition(
    EnemyKind Kind,
    string Name,
    double Power,
    int AmbushWeight,
    IReadOnlyList<ResourceAmount> Loot,
    IReadOnlyList<ResourceAmount> Steals,
    int Damage,
    int InjuryMinutes,
    bool Undead = false);

public static class EnemyCatalog
{
    public static IReadOnlyList<EnemyDefinition> Enemies { get; } =
    [
        new(EnemyKind.Wolf, "wolf", 3, 5,
            [new(ResourceKind.Leather, 1)],
            [new(ResourceKind.Food, 3)],
            Damage: 0, InjuryMinutes: 30),
        new(EnemyKind.Boar, "boar", 5, 3,
            [new(ResourceKind.Food, 6), new(ResourceKind.Leather, 1)],
            [new(ResourceKind.Food, 4), new(ResourceKind.Grain, 2)],
            Damage: 10, InjuryMinutes: 40),
        new(EnemyKind.Bear, "bear", 12, 1,
            [new(ResourceKind.Food, 15), new(ResourceKind.Leather, 3)],
            [new(ResourceKind.Food, 12)],
            Damage: 25, InjuryMinutes: 60),
        new(EnemyKind.Zombie, "zombie", 4, 0,
            [],
            [],
            Damage: 15, InjuryMinutes: 30, Undead: true),
        new(EnemyKind.Skeleton, "skeleton", 6, 0,
            [new(ResourceKind.IronOre, 1)],
            [new(ResourceKind.Gold, 1)],
            Damage: 20, InjuryMinutes: 40, Undead: true),
    ];

    private static readonly Dictionary<EnemyKind, EnemyDefinition> ByKind = Enemies.ToDictionary(e => e.Kind);

    public static EnemyDefinition Get(EnemyKind kind) => ByKind[kind];
}
