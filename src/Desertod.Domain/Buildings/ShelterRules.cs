using Desertod.Domain.Entities;
using Desertod.Domain.Rules;

namespace Desertod.Domain.Buildings;

public static class ShelterRules
{
    /// <summary>
    /// Gives the best beds to the citizens who joined first; everyone else sleeps under the trees.
    /// Energy is banked first so the new regeneration rate does not apply retroactively.
    /// </summary>
    public static void Assign(Village village, IEnumerable<Citizen> citizens, DateTimeOffset now)
    {
        var beds = village.Structures
            .Select(s => BuildingCatalog.Get(s.Kind))
            .Where(d => d.Shelter > 0)
            .SelectMany(d => Enumerable.Repeat(d.ShelterQuality, d.Shelter))
            .OrderByDescending(quality => quality)
            .ToList();

        var index = 0;
        foreach (var citizen in citizens.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id))
        {
            var quality = index < beds.Count ? beds[index] : GameRules.UnderTreesRegeneration;
            index++;
            if (Math.Abs(citizen.ShelterQuality - quality) < 1e-9)
            {
                continue;
            }

            citizen.FlushEnergy(now, village);
            citizen.ShelterQuality = quality;
        }
    }
}
