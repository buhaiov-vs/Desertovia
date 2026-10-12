using Desertod.Domain.Catalog;

namespace Desertod.Domain.Projects;

public enum ProjectStatus
{
    Active,
    Completed,
}

public enum ProjectPhase
{
    Survey,
    Construction,
    Completed,
}

public sealed class VillageProject
{
    public const string SurveyKey = "Survey";
    public const string BuildKey = "Build";

    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid VillageId { get; set; }
    public required string DefinitionId { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public double SurveyProgress { get; set; }
    public double BuildProgress { get; set; }
    public List<ProjectMaterial> Materials { get; set; } = [];
    public List<ProjectContribution> Contributions { get; set; } = [];

    public ProjectDefinition Definition =>
        ProjectCatalog.Find(DefinitionId) ?? throw new InvalidOperationException($"Unknown project '{DefinitionId}'.");

    public static VillageProject Start(Guid villageId, ProjectDefinition definition, DateTimeOffset now)
    {
        var project = new VillageProject { VillageId = villageId, DefinitionId = definition.Id, StartedAt = now };
        foreach (var material in definition.Materials)
        {
            project.Materials.Add(new ProjectMaterial { VillageProjectId = project.Id, Kind = material.Resource });
        }

        return project;
    }

    public bool SurveyDone => SurveyProgress >= Definition.SurveyCycles - ProjectRules.Epsilon;

    public bool MaterialsDone => MaterialsFraction >= 1 - ProjectRules.Epsilon;

    public bool BuildDone => BuildProgress >= Definition.BuildCycles - ProjectRules.Epsilon;

    public bool IsComplete => SurveyDone && MaterialsDone && BuildDone;

    public ProjectPhase Phase => Status == ProjectStatus.Completed ? ProjectPhase.Completed
        : SurveyDone ? ProjectPhase.Construction
        : ProjectPhase.Survey;

    public double Delivered(ResourceKind kind) => Materials.SingleOrDefault(m => m.Kind == kind)?.Delivered ?? 0;

    public double MaterialsFraction => Definition.Materials.Count == 0
        ? 1
        : Definition.Materials.Average(m => Math.Min(1, Delivered(m.Resource) / m.Amount));

    /// <summary>Construction cannot run ahead of the delivered materials.</summary>
    public double BuildLimit => MaterialsFraction * Definition.BuildCycles;

    public double Progress =>
        ProjectRules.SurveyWeight * (Definition.SurveyCycles == 0 ? 1 : Math.Min(1, SurveyProgress / Definition.SurveyCycles))
        + ProjectRules.MaterialsWeight * MaterialsFraction
        + ProjectRules.BuildWeight * Math.Min(1, BuildProgress / Definition.BuildCycles);

    public double Required(string key) => key switch
    {
        SurveyKey => Definition.SurveyCycles,
        BuildKey => Definition.BuildCycles,
        _ => Definition.Materials.SingleOrDefault(m => m.Resource.ToString() == key)?.Amount ?? 0,
    };

    public double Done(string key) => key switch
    {
        SurveyKey => SurveyProgress,
        BuildKey => BuildProgress,
        _ => Enum.TryParse<ResourceKind>(key, out var kind) ? Delivered(kind) : 0,
    };

    public double Remaining(string key) => Math.Max(0, Required(key) - Done(key));

    public double Contributed(Guid citizenId, string key) =>
        Contributions.SingleOrDefault(c => c.CitizenId == citizenId && c.Key == key)?.Amount ?? 0;

    public double ContributionRoom(Guid citizenId, string key, int activeCitizens) =>
        Math.Max(0, ProjectRules.ContributionCap(Required(key), activeCitizens) - Contributed(citizenId, key));

    public void Record(Guid citizenId, string key, double amount)
    {
        switch (key)
        {
            case SurveyKey: SurveyProgress += amount; break;
            case BuildKey: BuildProgress += amount; break;
            default:
                Materials.Single(m => m.Kind.ToString() == key).Delivered += amount;
                break;
        }

        var contribution = Contributions.SingleOrDefault(c => c.CitizenId == citizenId && c.Key == key);
        if (contribution is null)
        {
            contribution = new ProjectContribution { VillageProjectId = Id, CitizenId = citizenId, Key = key };
            Contributions.Add(contribution);
        }

        contribution.Amount += amount;
    }

    /// <summary>Share of the whole project done by a citizen: average of their share over every requirement.</summary>
    public double ShareOf(Guid citizenId)
    {
        var keys = Definition.Materials.Select(m => m.Resource.ToString()).Append(SurveyKey).Append(BuildKey).ToList();
        return keys.Average(key => Required(key) <= 0 ? 0 : Contributed(citizenId, key) / Required(key));
    }
}

public sealed class ProjectMaterial
{
    public Guid VillageProjectId { get; set; }
    public ResourceKind Kind { get; set; }
    public double Delivered { get; set; }
}

public sealed class ProjectContribution
{
    public Guid VillageProjectId { get; set; }
    public Guid CitizenId { get; set; }
    public required string Key { get; set; }
    public double Amount { get; set; }
}
