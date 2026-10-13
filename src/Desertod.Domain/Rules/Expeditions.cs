using Desertod.Domain.Catalog;

namespace Desertod.Domain.Rules;

public sealed record LootEntry(ResourceKind Resource, int Amount, int Weight);

public static class Loot
{
    /// <summary>Picks one entry, each with a chance proportional to its weight.</summary>
    public static LootEntry Roll(IReadOnlyList<LootEntry> table, Random random)
    {
        var roll = random.Next(table.Sum(e => e.Weight));
        foreach (var entry in table)
        {
            if (roll < entry.Weight)
            {
                return entry;
            }

            roll -= entry.Weight;
        }

        return table[^1];
    }
}

public static class Expeditions
{
    public static IReadOnlyList<LootEntry> Table { get; } =
    [
        new(ResourceKind.Wood, 3, 30),
        new(ResourceKind.Stone, 2, 25),
        new(ResourceKind.Food, 2, 25),
        new(ResourceKind.IronOre, 1, 12),
        new(ResourceKind.Coal, 1, 8),
    ];

    public static readonly ResourceAmount RareFind = new(ResourceKind.Gold, 1);

    /// <summary>Basic expedition: 1% + 0.5% per Dexterity. Scouts on a long expedition: 5% + 1% per Dexterity.</summary>
    public static double RareChance(int dexterity, bool scout) =>
        scout ? 0.05 + 0.01 * dexterity : 0.01 + 0.005 * dexterity;

    public static LootEntry Roll(Random random) => Loot.Roll(Table, random);
}
