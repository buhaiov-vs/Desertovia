using System.Security.Cryptography;
using System.Text;
using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Farming;
using Desertod.Domain.Rules;
using Desertod.Domain.Storage;
using Desertod.Domain.Technologies;
using Desertod.Domain.World;

namespace Desertod.Domain.Entities;

public sealed class Village
{
    public const int NameMinimumLength = 3;
    public const int NameMaximumLength = 64;
    public const int DescriptionMaximumLength = 280;
    public const int InviteCodeLength = 8;

    /// <summary>No 0/O or 1/I/L, so a code read aloud on stream is typed correctly.</summary>
    private const string InviteAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    /// <summary>A private village is hidden from the public list and needs <see cref="InviteCode"/> to join. Anyone can still watch it.</summary>
    public bool IsPrivate { get; set; }

    public string? InviteCode { get; set; }
    public Guid OwnerAccountId { get; set; }
    public bool IsLive { get; set; }
    public double Morale { get; set; } = 1.0;
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Research points. Not a physical good, so it has no storage limit.</summary>
    public double Knowledge { get; set; }

    /// <summary>Gold dug from the vein on the far bank; the vein is empty at <see cref="Rules.GoldVein.Size"/>.</summary>
    public double GoldVeinMined { get; set; }

    public List<StorageBuilding> Storages { get; set; } = [];

    /// <summary>Finished buildings that are not storage: shelters and workplaces.</summary>
    public List<Structure> Structures { get; set; } = [];

    public List<ConstructionSite> Sites { get; set; } = [];

    /// <summary>Game animals on the village land, one herd per kind.</summary>
    public List<Herd> Wildlife { get; set; } = [];

    /// <summary>Trees on the camp land, felled ones included: their stumps grow back.</summary>
    public List<Tree> Trees { get; set; } = [];

    /// <summary>Beds of every farm, each sown with one crop or empty.</summary>
    public List<FieldBed> Fields { get; set; } = [];

    /// <summary>Technologies being researched or discovered.</summary>
    public List<TechnologyProgress> Research { get; set; } = [];

    /// <summary>The open vote on the next discovery, or the last closed one.</summary>
    public TechnologyPoll? Poll { get; set; }

    /// <summary>Technology the last vote chose; researched until it is discovered.</summary>
    public string? ChosenTechnology { get; set; }

    /// <summary>A new tribe has a campfire, a heap of goods next to it, woods and wild animals around. Everything else is built by hand.</summary>
    public static Village Create(string slug, string name, Guid ownerAccountId, DateTimeOffset now)
    {
        var village = new Village { Slug = slug, Name = name, OwnerAccountId = ownerAccountId, CreatedAt = now };
        village.EnsureCamp(now);
        return village;
    }

    public static Village Create(
        string slug,
        string name,
        string? description,
        bool isPrivate,
        Guid ownerAccountId,
        DateTimeOffset now)
    {
        name = name.Trim();
        if (name.Length is < NameMinimumLength or > NameMaximumLength)
        {
            throw new GameRuleException("village.nameLength", "The village name must be 3 to 64 characters long.");
        }

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > DescriptionMaximumLength)
        {
            throw new GameRuleException(
                "village.descriptionLength",
                $"The description must be at most {DescriptionMaximumLength} characters long.",
                new Dictionary<string, object> { ["maximum"] = DescriptionMaximumLength });
        }

        var village = Create(slug, name, ownerAccountId, now);
        village.Description = description;
        village.IsPrivate = isPrivate;
        if (isPrivate)
        {
            village.NewInviteCode();
        }

        return village;
    }

    /// <summary>Replaces the invite code, so links shared before stop working.</summary>
    public string NewInviteCode()
    {
        InviteCode = RandomNumberGenerator.GetString(InviteAlphabet, InviteCodeLength);
        return InviteCode;
    }

    public bool Admits(string? inviteCode)
    {
        if (!IsPrivate)
        {
            return true;
        }

        if (InviteCode is null || string.IsNullOrWhiteSpace(inviteCode))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(inviteCode.Trim().ToUpperInvariant()),
            Encoding.ASCII.GetBytes(InviteCode));
    }

    /// <summary>
    /// Gives a village what a new tribe starts with, also villages created before it existed, and beds to farms built
    /// before beds existed. Returns true if anything was added.
    /// </summary>
    public bool EnsureCamp(DateTimeOffset now)
    {
        var added = false;
        if (Storages.Count == 0)
        {
            Storages.Add(StorageBuilding.Create(StorageKind.Heap, WorldMap.HeapPlot, now));
            added = true;
        }

        foreach (var animal in WildlifeCatalog.Animals.Where(a => Wildlife.All(h => h.Kind != a.Kind)))
        {
            Wildlife.Add(Herd.Full(animal.Kind, now));
            added = true;
        }

        if (Trees.Count == 0)
        {
            Trees.AddRange(Forest.Plant(new Random(Id.GetHashCode())));
            added = true;
        }

        return FarmRules.EnsureBeds(this) || added;
    }

    public Herd HerdOf(AnimalKind kind) => Wildlife.Single(h => h.Kind == kind);

    public bool Has(BuildingKind kind) =>
        Structures.Any(s => s.Kind == kind)
        || Storages.Any(s => BuildingCatalog.ForStorage(s.Kind) == kind);

    /// <summary>Finished buildings and open sites of this kind; repeats need more material.</summary>
    public int CountOf(BuildingKind kind) =>
        Structures.Count(s => s.Kind == kind)
        + Storages.Count(s => BuildingCatalog.ForStorage(s.Kind) == kind)
        + Sites.Count(s => s.Kind == kind);

    /// <summary>What is built or planned on a plot.</summary>
    public BuildingKind? KindAt(string plotId) =>
        Sites.FirstOrDefault(s => s.PlotId == plotId)?.Kind
        ?? Structures.FirstOrDefault(s => s.PlotId == plotId)?.Kind
        ?? Storages.Where(s => s.PlotId == plotId).Select(s => BuildingCatalog.ForStorage(s.Kind)).FirstOrDefault();

    /// <summary>True when a building or an open site covers any tile of the area.</summary>
    public bool AreaTaken(TileArea area) => AreaBuilt(area) || Covers(Sites.Select(s => s.PlotId), area);

    /// <summary>True when a finished building covers any tile of the area.</summary>
    public bool AreaBuilt(TileArea area) => Covers(Storages.Select(s => s.PlotId).Concat(Structures.Select(s => s.PlotId)), area);

    private static bool Covers(IEnumerable<string> plotIds, TileArea area) =>
        plotIds.Any(plotId => WorldMap.Footprint(plotId) is { } taken && taken.Overlaps(area));

    public double Stock(ResourceKind kind) =>
        StorageCatalog.IsGood(kind) ? Storages.Sum(s => s.Amount(kind)) : Knowledge;

    public double FreeSpace(ResourceKind kind) =>
        StorageCatalog.IsGood(kind) ? Storages.Sum(s => s.FreeSpace(kind)) : double.PositiveInfinity;

    /// <summary>Stores as much as fits, specialised buildings first, and returns the stored amount.</summary>
    public double Store(ResourceKind kind, double amount)
    {
        if (!StorageCatalog.IsGood(kind))
        {
            Knowledge += amount;
            return amount;
        }

        var stored = 0.0;
        var order = Storages
            .OrderBy(s => s.Definition.IsGeneral)
            .ThenByDescending(s => s.Amount(kind) > 0);
        foreach (var building in order)
        {
            if (amount - stored <= 0)
            {
                break;
            }

            stored += building.Put(kind, amount - stored);
        }

        return stored;
    }

    /// <summary>Takes up to <paramref name="amount"/>, general storage first to keep its slots free.</summary>
    public double Take(ResourceKind kind, double amount)
    {
        if (!StorageCatalog.IsGood(kind))
        {
            var fromKnowledge = Math.Min(amount, Knowledge);
            Knowledge -= fromKnowledge;
            return fromKnowledge;
        }

        var taken = 0.0;
        foreach (var building in Storages.OrderBy(s => !s.Definition.IsGeneral))
        {
            if (amount - taken <= 0)
            {
                break;
            }

            taken += building.Take(kind, amount - taken);
        }

        return taken;
    }

    /// <summary>Where a citizen walks to drop this good: the nearest building with room, else the nearest that accepts it.</summary>
    public TilePoint DropPoint(ResourceKind kind, TilePoint from)
    {
        var accepting = Storages.Where(s => s.Definition.AcceptsGood(kind)).ToList();
        var target = accepting.Where(s => s.FreeSpace(kind) > 0).MinBy(s => WorldMap.Yard(s.PlotId).DistanceTo(from))
                     ?? accepting.MinBy(s => WorldMap.Yard(s.PlotId).DistanceTo(from));
        return target is null ? WorldMap.Places[WorldMap.Camp] : WorldMap.Yard(target.PlotId);
    }

    /// <summary>Where a citizen picks up this good: the nearest building holding it.</summary>
    public TilePoint PickupPoint(ResourceKind kind, TilePoint to)
    {
        var target = Storages.Where(s => s.Amount(kind) > 0).MinBy(s => WorldMap.Yard(s.PlotId).DistanceTo(to));
        return target is null ? WorldMap.Places[WorldMap.Camp] : WorldMap.Yard(target.PlotId);
    }

    public void AddMorale(double delta) =>
        Morale = Math.Clamp(Morale + delta, GameRules.MoraleMinimum, GameRules.MoraleMaximum);
}
