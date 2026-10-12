using Desertod.Domain.Entities;
using Desertod.Domain.Storage;

namespace Desertod.Domain.Projects;

public sealed record ProjectCompletion(string ProjectId, string ProjectName, IReadOnlyDictionary<Guid, double> RenownByCitizen);

public static class ProjectRules
{
    public const double Epsilon = 1e-6;

    public const double SurveyWeight = 0.1;
    public const double MaterialsWeight = 0.5;
    public const double BuildWeight = 0.4;

    /// <summary>No citizen may provide more than this share of one requirement...</summary>
    public const double ContributionCapShare = 0.2;

    /// <summary>...except in small villages, where the cap is relaxed to an equal split.</summary>
    public static double ContributionCap(double required, int activeCitizens) =>
        required * Math.Max(ContributionCapShare, 1.0 / Math.Max(1, activeCitizens));

    public const int SupervisionBase = 3;

    public static int SupervisionCapacity(int engineerIntelligence) => SupervisionBase + engineerIntelligence;

    /// <summary>Marks the project complete, rewards the village and splits Renown by contribution share.</summary>
    public static ProjectCompletion Complete(
        VillageProject project,
        Village village,
        IReadOnlyDictionary<Guid, Citizen> contributors,
        DateTimeOffset now)
    {
        var reward = project.Definition.Reward;
        project.Status = ProjectStatus.Completed;
        project.CompletedAt = now;

        village.AddMorale(reward.Morale);
        if (reward.Storage is { } storage && village.Storages.All(s => s.PlotId != project.DefinitionId))
        {
            village.Storages.Add(StorageBuilding.Create(storage, project.DefinitionId, now));
        }

        var shares = contributors.Keys.ToDictionary(id => id, project.ShareOf);
        var total = shares.Values.Sum();
        var renown = new Dictionary<Guid, double>();
        foreach (var (citizenId, share) in shares)
        {
            var amount = total <= 0 ? 0 : Math.Round(reward.RenownPool * share / total, 1);
            contributors[citizenId].Renown += amount;
            renown[citizenId] = amount;
        }

        return new ProjectCompletion(project.DefinitionId, project.Definition.Name, renown);
    }
}
