using Desertod.Domain.Entities;
using Desertod.Domain.Stats;

namespace Desertod.Domain.Rules;

public static class CharacterCreation
{
    public const int NameMaximumLength = 32;

    /// <summary>A new citizen arrives with nothing but a name and a look: every stat is 0.</summary>
    public static Citizen Create(Guid villageId, Guid accountId, string name, DateTimeOffset now, Appearance? appearance = null)
    {
        name = name.Trim();
        if (name.Length is 0 or > NameMaximumLength)
        {
            throw new GameRuleException(
                "citizen.nameLength",
                $"The name must be 1 to {NameMaximumLength} characters long.",
                new Dictionary<string, object> { ["maximum"] = NameMaximumLength });
        }

        if (appearance is not null && !appearance.IsValid())
        {
            throw new GameRuleException("citizen.appearance", "This look is not available.");
        }

        var stats = new StatBlock();
        return new Citizen
        {
            VillageId = villageId,
            AccountId = accountId,
            Name = name,
            Appearance = appearance,
            Stats = stats,
            CreatedAt = now,
            StoredEnergy = Energy.Maximum(stats.Endurance),
            EnergyUpdatedAt = now,
        };
    }
}
