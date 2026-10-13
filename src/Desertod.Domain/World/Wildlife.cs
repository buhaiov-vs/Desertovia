namespace Desertod.Domain.World;

public enum AnimalKind
{
    Rabbit,
    Deer,
    Bird,
}

/// <param name="Capacity">Most animals of this kind the land around the village feeds.</param>
/// <param name="RecoveryHalfLifeHours">Half of the missing animals come back within this time.</param>
/// <param name="Habitat">Place on <see cref="WorldMap"/> where they live and are hunted.</param>
public sealed record AnimalDefinition(AnimalKind Kind, string Name, int Capacity, double RecoveryHalfLifeHours, string Habitat);

public static class WildlifeCatalog
{
    /// <summary>Below this share of its capacity a herd is scarce and hunters catch less.</summary>
    public const double AbundantShare = 0.5;

    /// <summary>Hunting needs at least one whole animal; below that the land is hunted out.</summary>
    public const int MinimumToHunt = 1;

    public static IReadOnlyList<AnimalDefinition> Animals { get; } =
    [
        new(AnimalKind.Rabbit, "rabbits", 72, 0.75, "hunting"),
        new(AnimalKind.Deer, "deer", 40, 1.5, "deer-woods"),
        new(AnimalKind.Bird, "birds", 60, 0.5, "bird-grove"),
    ];

    private static readonly Dictionary<AnimalKind, AnimalDefinition> ByKind = Animals.ToDictionary(a => a.Kind);

    public static AnimalDefinition Get(AnimalKind kind) => ByKind[kind];
}

/// <summary>
/// Animals of one kind on the village land. <see cref="Count"/> is stored at <see cref="UpdatedAt"/>;
/// animals that came back since then are calculated, the same way energy regenerates.
/// </summary>
public sealed class Herd
{
    public AnimalKind Kind { get; set; }
    public int Count { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public AnimalDefinition Definition => WildlifeCatalog.Get(Kind);

    public static Herd Full(AnimalKind kind, DateTimeOffset now) =>
        new() { Kind = kind, Count = WildlifeCatalog.Get(kind).Capacity, UpdatedAt = now };

    /// <summary>Animals now, rounded to whole ones.</summary>
    public int Current(DateTimeOffset now)
    {
        var definition = Definition;
        var hours = Math.Max(0, (now - UpdatedAt).TotalHours);
        var missing = (definition.Capacity - Count) * Math.Pow(0.5, hours / definition.RecoveryHalfLifeHours);
        return definition.Capacity - (int)Math.Round(missing);
    }

    /// <summary>1 while the herd is at least half its capacity, then falling with the herd.</summary>
    public double Abundance(DateTimeOffset now) =>
        Math.Clamp(Current(now) / (Definition.Capacity * WildlifeCatalog.AbundantShare), 0, 1);

    /// <summary>Takes up to <paramref name="amount"/> animals and returns how many were taken.</summary>
    public int Take(int amount, DateTimeOffset now)
    {
        var current = Current(now);
        var taken = Math.Clamp(amount, 0, current);
        Count = current - taken;
        UpdatedAt = now;
        return taken;
    }
}
