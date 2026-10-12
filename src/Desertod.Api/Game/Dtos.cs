using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Farming;
using Desertod.Domain.Projects;
using Desertod.Domain.Quests;
using Desertod.Domain.Rules;
using Desertod.Domain.Stats;
using Desertod.Domain.Storage;
using Desertod.Domain.Technologies;
using Desertod.Domain.World;

namespace Desertod.Api.Game;

/// <param name="Capacity">Amount plus free room in all storage that accepts the good; null when unlimited.</param>
public sealed record ResourceDto(ResourceKind Kind, double Amount, double? Capacity);

/// <param name="Fill">0 to 1; drawn as stacks on the building, the only storage hint the Governor gets.</param>
public sealed record StorageSlotDto(ResourceKind Kind, double Fill);

public sealed record StorageDto(StorageKind Kind, string PlotId, IReadOnlyList<ResourceAmount> Goods, IReadOnlyList<StorageSlotDto> Slots);

public sealed record StructureDto(BuildingKind Kind, string PlotId);

/// <param name="Room">Work a labor job can take right now; it cannot run ahead of the delivered material.</param>
public sealed record SiteJobDto(string JobId, double Done, double Required, double Room);

/// <param name="CrewNeeded">Different citizens the stage needs, scaled down for small streams.</param>
/// <param name="Hands">Different citizens who already worked on the stage.</param>
public sealed record SiteStageDto(string Id, int CrewNeeded, int Hands, IReadOnlyList<SiteJobDto> Jobs);

public sealed record SiteDto(BuildingKind Kind, string PlotId, int Stage, double Progress, IReadOnlyList<SiteStageDto> Stages);

/// <param name="Count">Animals now; the map shows a share of them, the Governor never sees the number.</param>
public sealed record HerdDto(AnimalKind Kind, double Count, double Capacity, string Habitat);

public sealed record TreeDto(int X, int Y);

/// <param name="Growth">0 when sown, 1 when ripe; 0 for an empty bed.</param>
/// <param name="WateredUntil">The bed is wet until then.</param>
/// <param name="Thirsty">Growing and drying out soon: watering reaches it.</param>
public sealed record FieldBedDto(string PlotId, int Index, CropKind? Crop, double Growth, DateTimeOffset? WateredUntil, bool Thirsty);

/// <param name="Available">Not discovered and every technology it builds on is; the Governor can put it to the vote.</param>
/// <param name="CrewNeeded">Different citizens the research needs, scaled down for small streams.</param>
public sealed record TechnologyStateDto(
    string Id,
    bool Discovered,
    bool Available,
    double Done,
    double Required,
    int Hands,
    int CrewNeeded);

public sealed record PollOptionDto(string TechnologyId, int Votes);

public sealed record PollBallotDto(Guid CitizenId, string TechnologyId);

/// <param name="Electorate">Active citizens now, or when the poll closed.</param>
/// <param name="VotesNeeded">Votes that close the poll: more than half of <paramref name="Electorate"/>.</param>
/// <param name="Winner">Null while open, or when the Governor cancelled the poll.</param>
public sealed record TechnologyPollDto(
    Guid Id,
    IReadOnlyList<PollOptionDto> Options,
    IReadOnlyList<PollBallotDto> Ballots,
    int Electorate,
    int VotesNeeded,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    string? Winner);

/// <param name="Trees">
/// Standing trees on the camp land, those waiting to be felled on construction sites included;
/// the client draws the wild beyond it by itself.
/// </param>
/// <param name="CurrentResearch">The technology the last vote chose; null until a vote chooses an undiscovered one.</param>
/// <param name="Poll">The open vote on the next discovery, or the last closed one.</param>
/// <param name="Fields">Beds of every farm.</param>
public sealed record VillageStateDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    bool IsPrivate,
    bool IsLive,
    double Morale,
    IReadOnlyList<ResourceDto> Resources,
    IReadOnlyList<StorageDto> Storages,
    IReadOnlyList<StructureDto> Structures,
    IReadOnlyList<SiteDto> Sites,
    ProjectDto? Project,
    IReadOnlyList<string> CompletedProjects,
    IReadOnlyList<QuestDto> Quests,
    IReadOnlyList<HerdDto> Wildlife,
    IReadOnlyList<TreeDto> Trees,
    Era Era,
    string? CurrentResearch,
    IReadOnlyList<TechnologyStateDto> Technologies,
    TechnologyPollDto? Poll,
    double GoldVeinRemaining,
    IReadOnlyList<FieldBedDto> Fields);

/// <param name="Objectives">Governor objectives carry no numbers; the client hides citizen numbers from the Governor.</param>
public sealed record QuestDto(Guid Id, QuestKind Kind, ResourceKind? Resource, DateTimeOffset StartedAt, IReadOnlyList<QuestObjective> Objectives);

public sealed record ProjectMaterialDto(ResourceKind Kind, double Delivered, double Required);

public sealed record ProjectDto(
    Guid Id,
    string DefinitionId,
    string Name,
    string Description,
    ProjectPhase Phase,
    double Progress,
    double SurveyDone,
    double SurveyRequired,
    double BuildDone,
    double BuildLimit,
    double BuildRequired,
    IReadOnlyList<ProjectMaterialDto> Materials,
    int ActiveCitizens,
    int SupervisionCapacity,
    int BuildersWorking,
    bool NeedsEngineer);

/// <param name="Subtitle">Written by the Governor, shown under the name on the map.</param>
/// <param name="SiteId">Plot the citizen works on: a construction site, or the building where they research.</param>
/// <param name="Spot">Spot on the map the citizen gathers at, picked by the player.</param>
/// <param name="IdleSpot">Where the player sent the citizen to stand while not working.</param>
public sealed record CitizenSummaryDto(
    Guid Id,
    string Name,
    string? Subtitle,
    Appearance? Appearance,
    string TitleRoleId,
    string? TaskId,
    string? Workplace,
    string? SiteId,
    TilePoint? Spot,
    TilePoint? IdleSpot,
    WorkState State,
    DateTimeOffset? CycleStartedAt,
    DateTimeOffset? CycleEndsAt,
    IReadOnlyList<ResourceKind> Carrying);

/// <param name="InviteCode">Only sent to the owner of a private village.</param>
public sealed record VillageDto(VillageStateDto State, bool IsOwner, string? InviteCode, IReadOnlyList<CitizenSummaryDto> Citizens);

public sealed record VillageListItemDto(string Slug, string Name, bool IsOwner, bool IsLive);

public sealed record PublicVillageDto(string Slug, string Name, string? Description, bool IsLive, int Citizens);

public sealed record InviteDto(string InviteCode);

public sealed record StatDto(Stat Stat, int Level, double Experience, double ExperienceToNext);

/// <param name="SiteId">Plot of the construction site the citizen works on.</param>
/// <param name="Spot">Spot on the map the last task gathered at, picked by the player.</param>
/// <param name="TechnologyId">Technology the current cycle researches.</param>
public sealed record CitizenDto(
    Guid Id,
    string Name,
    Appearance? Appearance,
    string TitleRoleId,
    CitizenStatus Status,
    IReadOnlyList<StatDto> Stats,
    int StatTotal,
    double Renown,
    double Energy,
    double MaximumEnergy,
    int Streak,
    string? TaskId,
    string? SiteId,
    int? SiteStage,
    TilePoint? Spot,
    WorkState State,
    DateTimeOffset? CycleStartedAt,
    DateTimeOffset? CycleEndsAt,
    DateTimeOffset? GraceEndsAt,
    bool CycleTired,
    IReadOnlyList<ResourceAmount> Load,
    double ShelterQuality,
    bool IsHungry,
    DateTimeOffset ServerNow,
    string? TechnologyId);

public sealed record RulesDto(
    int CycleSeconds,
    int GraceSeconds,
    int StatMaximum,
    int TalentCap,
    double SecondsPerTile,
    int MaximumOpenSites,
    double FoodPerCycle,
    double TakeLimit);

public sealed record CatalogDto(
    IReadOnlyList<RoleDefinition> Roles,
    IReadOnlyList<TaskDefinition> Tasks,
    IReadOnlyList<ProjectDefinition> Projects,
    IReadOnlyList<StorageDefinition> Storages,
    IReadOnlyList<BuildingDefinition> Buildings,
    IReadOnlyList<JobDefinition> Jobs,
    IReadOnlyDictionary<string, TilePoint> Plots,
    TileArea BuildArea,
    IReadOnlyList<TileArea> ReservedAreas,
    IReadOnlyDictionary<string, TilePoint> Places,
    IReadOnlyDictionary<QuestKind, QuestReward> QuestRewards,
    RulesDto Rules,
    IReadOnlyList<TechnologyDefinition> Technologies,
    IReadOnlyList<CropDefinition> Crops);

public sealed record CreateVillageRequest(string Name, string? Description = null, bool IsPrivate = false);

public sealed record SetLiveRequest(bool IsLive);

public sealed record CreateCitizenRequest(string Name, Appearance? Appearance, string? InviteCode = null);

/// <param name="Spot">Where on the map to gather, for tasks that allow it; null works at the task's workplace.</param>
public sealed record StartTaskRequest(string TaskId, TilePoint? Spot = null);

/// <param name="Spot">Where on the land the idle citizen walks to and waits.</param>
public sealed record GoToRequest(TilePoint Spot);

/// <param name="PlotId">The storage building to take from.</param>
public sealed record TakeRequest(string PlotId, ResourceKind Resource);

public sealed record StartJobRequest(string PlotId, int Stage, string JobId);

/// <param name="X">Top-left tile of the building.</param>
public sealed record PlaceBlueprintRequest(BuildingKind Kind, int X, int Y);

/// <param name="TechnologyIds">Available technologies the Governor puts to the vote.</param>
public sealed record OpenPollRequest(IReadOnlyList<string> TechnologyIds);

public sealed record VoteRequest(string TechnologyId);

/// <param name="Subtitle">Null or blank removes the subtitle.</param>
public sealed record SetSubtitleRequest(string? Subtitle);

public static class DtoMapper
{
    public static VillageStateDto ToStateDto(
        this Village village,
        WorkContext context,
        IReadOnlyList<string> completedProjects,
        IReadOnlyList<VillageQuest> quests,
        DateTimeOffset now) => new(
        village.Id,
        village.Slug,
        village.Name,
        village.Description,
        village.IsPrivate,
        village.IsLive,
        village.Morale,
        Enum.GetValues<ResourceKind>()
            .Select(kind => new ResourceDto(
                kind,
                village.Stock(kind),
                StorageCatalog.IsGood(kind) ? village.Stock(kind) + village.FreeSpace(kind) : null))
            .ToList(),
        village.Storages
            .Select(s => new StorageDto(
                s.Kind,
                s.PlotId,
                s.Goods.OrderBy(g => g.Kind).Select(g => new ResourceAmount(g.Kind, g.Amount)).ToList(),
                s.Slots().Select(slot => new StorageSlotDto(slot.Kind, slot.Fill)).ToList()))
            .ToList(),
        village.Structures.Select(s => new StructureDto(s.Kind, s.PlotId)).ToList(),
        village.Sites.OrderBy(s => s.PlacedAt).Select(s => s.ToDto(context.RecentCitizenCount)).ToList(),
        context.Project is { } project ? project.ToDto(context) : null,
        completedProjects,
        quests
            .Select(q => new QuestDto(q.Id, q.Kind, q.Resource, q.StartedAt, QuestRules.Objectives(q, village, context.ActiveCitizenCount, completedProjects)))
            .ToList(),
        village.Wildlife
            .OrderBy(h => h.Kind)
            .Select(h => new HerdDto(h.Kind, h.Current(now), h.Definition.Capacity, h.Definition.Habitat))
            .ToList(),
        Forest.Drawn(village, now).Select(t => new TreeDto(t.X, t.Y)).ToList(),
        TechnologyRules.EraOf(village),
        TechnologyRules.Current(village)?.Id,
        TechnologyCatalog.Technologies
            .Select(technology =>
            {
                var progress = TechnologyRules.ProgressOf(village, technology.Id);
                return new TechnologyStateDto(
                    technology.Id,
                    TechnologyRules.Knows(village, technology.Id),
                    TechnologyRules.IsAvailable(village, technology),
                    progress?.Done ?? 0,
                    technology.Cycles,
                    progress?.Hands.Count ?? 0,
                    TechnologyRules.CrewNeeded(technology, context.RecentCitizenCount));
            })
            .ToList(),
        village.Poll?.ToDto(context.ActiveCitizenCount),
        GoldVein.Remaining(village),
        village.Fields
            .OrderBy(b => b.PlotId)
            .ThenBy(b => b.Index)
            .Select(b => new FieldBedDto(b.PlotId, b.Index, b.Crop, FarmRules.GrowthOf(b, now), b.WateredUntil, FarmRules.IsThirsty(b, now)))
            .ToList());

    public static TechnologyPollDto ToDto(this TechnologyPoll poll, int activeCitizens)
    {
        var electorate = poll.IsOpen ? activeCitizens : poll.Electorate;
        return new TechnologyPollDto(
            poll.Id,
            poll.Options.Select(id => new PollOptionDto(id, poll.VotesFor(id))).ToList(),
            poll.Ballots.Select(b => new PollBallotDto(b.CitizenId, b.TechnologyId)).ToList(),
            electorate,
            TechnologyPoll.VotesNeeded(electorate),
            poll.OpenedAt,
            poll.ClosedAt,
            poll.Winner);
    }

    public static SiteDto ToDto(this ConstructionSite site, int recentCitizens) => new(
        site.Kind,
        site.PlotId,
        site.Stage,
        site.Fraction(),
        site.Definition.Stages
            .Select((stage, index) => new SiteStageDto(
                stage.Id,
                ConstructionSite.CrewNeeded(stage, recentCitizens),
                site.HandCount(index),
                site.Jobs(index)
                    .Select(job => new SiteJobDto(
                        job.JobId,
                        site.Done(index, job.JobId),
                        site.Required(index, job.JobId),
                        BuildingCatalog.FindJob(job.JobId)?.Kind == JobKind.Labor
                            ? site.LaborRoom(index, job.JobId)
                            : site.Remaining(index, job.JobId)))
                    .ToList()))
            .ToList());

    public static ProjectDto ToDto(this VillageProject project, WorkContext context)
    {
        var definition = project.Definition;
        return new ProjectDto(
            project.Id,
            project.DefinitionId,
            definition.Name,
            definition.Description,
            project.Phase,
            project.Progress,
            project.SurveyProgress,
            definition.SurveyCycles,
            project.BuildProgress,
            project.BuildLimit,
            definition.BuildCycles,
            definition.Materials
                .Select(m => new ProjectMaterialDto(m.Resource, project.Delivered(m.Resource), m.Amount))
                .ToList(),
            context.ActiveCitizenCount,
            context.SupervisionCapacity,
            context.BuildersWorking,
            definition.NeedsEngineer);
    }

    public static CitizenSummaryDto ToSummaryDto(this Citizen citizen, DateTimeOffset now)
    {
        var state = citizen.WorkState(now);
        var active = state != WorkState.Idle && citizen.CurrentTaskId is not null;

        return new CitizenSummaryDto(
            citizen.Id,
            citizen.Name,
            citizen.Subtitle,
            citizen.Appearance,
            GameCatalog.TitleFor(citizen.Stats).Id,
            active ? citizen.CurrentTaskId : null,
            active && citizen.CurrentSiteId is null ? GameCatalog.FindTask(citizen.CurrentTaskId!)?.Workplace : null,
            active ? citizen.CurrentSiteId : null,
            active ? citizen.Spot : null,
            state == WorkState.Working ? null : citizen.IdleSpot,
            state,
            citizen.CycleStartedAt,
            citizen.CycleEndsAt,
            citizen.Load.Select(g => g.Kind).ToList());
    }

    public static CitizenDto ToDto(this Citizen citizen, DateTimeOffset now) => new(
        citizen.Id,
        citizen.Name,
        citizen.Appearance,
        GameCatalog.TitleFor(citizen.Stats).Id,
        citizen.Status,
        Enum.GetValues<Stat>()
            .Select(stat => new StatDto(
                stat,
                citizen.Stats.Get(stat),
                citizen.Stats.GetExperience(stat),
                Progression.ExperienceToNext(citizen.Stats.Get(stat))))
            .ToList(),
        citizen.Stats.Total,
        citizen.Renown,
        citizen.CurrentEnergy(now),
        citizen.MaximumEnergy,
        citizen.Streak,
        citizen.CurrentTaskId,
        citizen.CurrentTechnologyId is null ? citizen.CurrentSiteId : null,
        citizen.CurrentSiteStage,
        citizen.Spot,
        citizen.WorkState(now),
        citizen.CycleStartedAt,
        citizen.CycleEndsAt,
        citizen.GraceEndsAt,
        citizen.CycleTired,
        citizen.Load.Select(g => new ResourceAmount(g.Kind, g.Amount)).ToList(),
        citizen.ShelterQuality,
        citizen.IsHungry,
        now,
        citizen.CurrentTechnologyId);

    public static CatalogDto Catalog { get; } = new(
        GameCatalog.Roles,
        GameCatalog.Tasks,
        ProjectCatalog.Projects,
        StorageCatalog.Storages,
        BuildingCatalog.Buildings,
        BuildingCatalog.Jobs,
        WorldMap.Plots,
        WorldMap.BuildArea,
        WorldMap.ReservedAreas,
        WorldMap.Places,
        QuestRules.Rewards,
        new RulesDto(
            (int)GameRules.CycleLength.TotalSeconds,
            (int)GameRules.GraceWindow.TotalSeconds,
            GameRules.StatMaximum,
            GameRules.TalentCap,
            WorldMap.SecondsPerTile,
            BuildingCatalog.MaximumOpenSites,
            GameRules.FoodPerCycle,
            StorageRules.TakeLimit),
        TechnologyCatalog.Technologies,
        FarmCatalog.Crops);
}
