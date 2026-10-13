using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.World;

namespace Desertod.Domain.Rules;

/// <summary>The monks' gold vein: one deposit for the whole village that does not grow back.</summary>
public static class GoldVein
{
    public const string DigTask = "dig-gold";
    public const int Size = 150;

    public static bool IsDug(TaskDefinition task) => task.Workplace == WorldMap.GoldVein;

    public static int Remaining(Village village) => Math.Max(0, Size - village.GoldVeinMined);

    public static bool IsEmpty(Village village) => Remaining(village) == 0;

    /// <summary>Takes up to <paramref name="amount"/> gold out of the vein and returns what was taken.</summary>
    public static int Take(Village village, int amount)
    {
        var taken = Math.Min(Math.Max(0, amount), Remaining(village));
        village.GoldVeinMined += taken;
        return taken;
    }
}
