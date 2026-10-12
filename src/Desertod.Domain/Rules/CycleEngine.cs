using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Entities;
using Desertod.Domain.Farming;
using Desertod.Domain.Projects;
using Desertod.Domain.Stats;
using Desertod.Domain.Storage;
using Desertod.Domain.Technologies;
using Desertod.Domain.World;

namespace Desertod.Domain.Rules;

/// <param name="ProjectContribution">Material carried to, or work done on, a project or construction site.</param>
/// <param name="SiteId">Plot of the construction site for construction jobs.</param>
/// <param name="BuildingCompleted">Set when this cycle finished the last stage of a building.</param>
/// <param name="Hungry">No food was left in storage for this cycle.</param>
/// <param name="TechnologyId">Technology the cycle researched.</param>
/// <param name="TechnologyDiscovered">Set when this cycle finished the research of <paramref name="TechnologyId"/>.</param>
public sealed record CycleReport(
    Guid CitizenId,
    string CitizenName,
    string TaskId,
    IReadOnlyList<ResourceAmount> Produced,
    IReadOnlyList<ResourceAmount> Carried,
    double MoraleGained,
    double ProjectContribution,
    bool RareFind,
    Stat Primary,
    double PrimaryExperience,
    Stat Secondary,
    double SecondaryExperience,
    int LevelsGained,
    double RenownGained,
    bool Tired,
    bool Overflowed,
    string? SiteId = null,
    BuildingKind? BuildingCompleted = null,
    bool Hungry = false,
    string? TechnologyId = null,
    bool TechnologyDiscovered = false);

public static class CycleEngine
{
    private const double UnloadExperienceMultiplier = 0.5;

    /// <summary>
    /// Starts a new task. A previous cycle must already be settled (<see cref="Settle"/>).
    /// Renewing inside the grace window keeps the streak; inputs are consumed up front.
    /// </summary>
    /// <param name="spot">Where on the map the player wants the goods gathered; see <see cref="WorkSpots"/>.</param>
    public static void Start(Citizen citizen, TaskDefinition task, WorkContext context, DateTimeOffset now, TilePoint? spot = null)
    {
        var village = context.Village;
        CheckReady(citizen, now);
        if (spot is { } picked)
        {
            WorkSpots.Check(village, task, picked, now);
        }

        if (task.Effect == TaskEffect.NotAvailableYet)
        {
            throw new GameRuleException("task.notAvailable", $"\"{task.Name}\" is not available yet.", new Dictionary<string, object> { ["taskId"] = task.Id });
        }

        if (task.RequiredBuilding is { } building && !village.Has(building))
        {
            throw new GameRuleException("task.needsBuilding", $"\"{task.Name}\" needs a {BuildingCatalog.Get(building).Name}.",
                new Dictionary<string, object> { ["building"] = building.ToString() });
        }

        if (task.RequiredProject is { } projectId && !context.CompletedProjects.Contains(projectId))
        {
            throw new GameRuleException("task.needsProject", $"\"{task.Name}\" needs the {ProjectCatalog.Find(projectId)?.Name}.",
                new Dictionary<string, object> { ["taskId"] = task.Id, ["project"] = projectId });
        }

        var role = GameCatalog.RoleOf(task);
        foreach (var technology in new[] { task.RequiredTechnology, role.Technology })
        {
            if (!TechnologyRules.Knows(village, technology))
            {
                throw new GameRuleException("task.needsTechnology",
                    $"\"{task.Name}\" needs {TechnologyCatalog.Get(technology!).Name}. The village must discover it first.",
                    new Dictionary<string, object> { ["taskId"] = task.Id, ["technology"] = technology! });
            }
        }

        if (!GameCatalog.HasHuntingSkill(citizen.Stats, task))
        {
            throw new GameRuleException("hunt.skillRequired", $"Hunting {WildlifeCatalog.Get(task.Hunt!.Prey).Name} needs Dexterity {task.Hunt.Dexterity}.",
                new Dictionary<string, object> { ["animal"] = task.Hunt.Prey.ToString(), ["level"] = task.Hunt.Dexterity });
        }

        if (!GameCatalog.HasFarmingSkill(citizen.Stats, task))
        {
            var crop = FarmCatalog.Get(task.Crop!.Value);
            throw new GameRuleException("farm.skillRequired", $"Growing {crop.Name} needs Endurance {crop.Endurance}.",
                new Dictionary<string, object> { ["crop"] = crop.Kind.ToString(), ["level"] = crop.Endurance });
        }

        if (!GameCatalog.CanPerform(citizen.Stats, task))
        {
            throw new GameRuleException("task.roleRequired", $"You lack the skills for the {role.Name} role.", new Dictionary<string, object> { ["roleId"] = role.Id });
        }

        if (task.Effect is TaskEffect.Sow or TaskEffect.Water or TaskEffect.Harvest)
        {
            FarmRules.Check(village, task, now);
        }

        CheckHands(citizen, village, task, now);
        CheckProjectTask(citizen, task, context);
        CheckPrey(village, task, now);
        if (GoldVein.IsDug(task) && GoldVein.IsEmpty(village))
        {
            throw new GameRuleException("task.veinEmpty", "The gold vein is empty.", new Dictionary<string, object> { ["taskId"] = task.Id });
        }

        foreach (var input in task.Inputs)
        {
            if (Available(citizen, village, input.Resource) < input.Amount)
            {
                throw new GameRuleException("storage.missingInput", $"Not enough {ResourceNames.Of(input.Resource)} in storage.", Resource(input.Resource));
            }
        }

        foreach (var input in task.Inputs)
        {
            TakeFromHandsThenStorage(citizen, village, input.Resource, input.Amount);
        }

        Begin(citizen, task.Id, TimeSpan.FromSeconds(task.WorkSeconds) + Walk(citizen, village, task, spot, now), now);
        citizen.CurrentSiteId = null;
        citizen.CurrentSiteStage = null;
        citizen.Spot = spot;
    }

    /// <summary>Starts a job on a construction site: hauling material there, or working on the current stage.</summary>
    public static void StartJob(Citizen citizen, string plotId, int stage, string jobId, WorkContext context, DateTimeOffset now)
    {
        var village = context.Village;
        CheckReady(citizen, now);
        var job = ConstructionRules.CheckJob(village, citizen, plotId, stage, jobId, context.RecentCitizenCount);

        var yard = WorldMap.Yard(plotId);
        var duration = job.Kind == JobKind.Haul
            ? BuildingCatalog.HaulBaseTime + WorldMap.Walk(
                citizen.Carried(job.Resource!.Value) > 0 ? WorldMap.Places[WorldMap.Camp] : village.PickupPoint(job.Resource.Value, yard),
                yard)
            : GameRules.CycleLength + WorldMap.Walk(WorldMap.Places[WorldMap.Camp], yard);

        Begin(citizen, job.Id, duration, now);
        citizen.CurrentSiteId = plotId;
        citizen.CurrentSiteStage = stage;
    }

    /// <summary>Starts researching the technology the citizens voted for, at its building.</summary>
    public static void StartResearch(Citizen citizen, WorkContext context, DateTimeOffset now)
    {
        CheckReady(citizen, now);
        var (technology, plotId) = TechnologyRules.CheckResearch(context.Village, citizen);

        Begin(citizen, TechnologyCatalog.ResearchTaskId,
            GameRules.CycleLength + WorldMap.Walk(WorldMap.Places[WorldMap.Camp], WorldMap.Yard(plotId)), now);
        citizen.CurrentTechnologyId = technology.Id;
        citizen.CurrentSiteId = plotId;
        citizen.CurrentSiteStage = null;
    }

    /// <summary>
    /// Stops the running task, job or research. Nothing is given back: the inputs and energy spent at the start
    /// are lost and the cycle earns nothing. The streak stays where it was before this cycle started.
    /// </summary>
    public static void Cancel(Citizen citizen, DateTimeOffset now)
    {
        if (citizen.WorkState(now) != WorkState.Working)
        {
            throw new GameRuleException("task.notRunning", "There is no task running.");
        }

        // A streak above zero means this cycle renewed the last one: keep the grace window open so the next task renews it again.
        if (citizen.Streak > 0)
        {
            citizen.Streak--;
            citizen.CycleEndsAt = now;
        }
        else
        {
            citizen.CycleEndsAt = null;
        }

        citizen.CycleSettled = true;
        citizen.CurrentTaskId = null;
        citizen.CurrentTechnologyId = null;
        citizen.CurrentSiteId = null;
        citizen.CurrentSiteStage = null;
        citizen.CycleStartedAt = null;
        citizen.Spot = null;
    }

    /// <summary>Banks the result of a completed cycle. Returns null if there is nothing to settle.</summary>
    public static CycleReport? Settle(Citizen citizen, WorkContext context, DateTimeOffset now)
    {
        if (citizen.CycleSettled || citizen.CurrentTaskId is null || citizen.CycleEndsAt is null
            || now < citizen.CycleEndsAt)
        {
            return null;
        }

        citizen.CycleSettled = true;
        if (citizen.CurrentTechnologyId is not null)
        {
            return SettleResearch(citizen, context, now);
        }

        return citizen.CurrentSiteId is not null
            ? SettleJob(citizen, context, now)
            : SettleTask(citizen, context, now);
    }

    private static CycleReport? SettleResearch(Citizen citizen, WorkContext context, DateTimeOffset now)
    {
        var technology = TechnologyCatalog.Find(citizen.CurrentTechnologyId!);
        if (technology is null)
        {
            return null;
        }

        var village = context.Village;
        var factor = YieldFactor(citizen, village, technology.Primary, technology.Secondary) / GameRules.StatFactorBase;
        var (amount, discovered) = TechnologyRules.Research(village, citizen, technology, factor, context.RecentCitizenCount, now);
        var overflowed = amount <= ProjectRules.Epsilon;
        var hungry = Eat(citizen, village, now);

        var report = Finish(citizen, village, TechnologyCatalog.ResearchTaskId, technology.Primary, technology.Secondary,
            technology.Researcher.Max(r => r.Minimum), overflowed ? GameRules.OverflowExperienceMultiplier : 1, hungry,
            [], [], 0, 0, false, overflowed);
        return report with { TechnologyId = technology.Id, TechnologyDiscovered = discovered };
    }

    private static CycleReport? SettleTask(Citizen citizen, WorkContext context, DateTimeOffset now)
    {
        var task = GameCatalog.FindTask(citizen.CurrentTaskId!);
        if (task is null)
        {
            return null;
        }

        var village = context.Village;
        var project = context.Project is { Status: ProjectStatus.Active } active ? active : null;
        var factor = YieldFactor(citizen, village, task.Primary, task.Secondary) * TechnologyRules.YieldMultiplier(village, task.Id);

        var produced = new List<ResourceAmount>();
        var carried = new List<ResourceAmount>();
        var overflowed = false;
        var rareFind = false;
        double moraleGained = 0;
        double contributed = 0;
        double experienceShare = 1;

        void Produce(ResourceKind kind, double amount)
        {
            if (amount <= ProjectRules.Epsilon)
            {
                return;
            }

            produced.Add(new ResourceAmount(kind, amount));
            var stored = village.Store(kind, amount);
            if (amount - stored > ProjectRules.Epsilon)
            {
                citizen.Carry(kind, amount - stored);
                carried.Add(new ResourceAmount(kind, amount - stored));
            }
        }

        switch (task.Effect)
        {
            case TaskEffect.Resources:
            {
                var share = factor;
                if (task.Hunt is { } hunt)
                {
                    var herd = village.HerdOf(hunt.Prey);
                    share = herd.Take(hunt.Animals * factor * herd.Abundance(now), now) / hunt.Animals;
                    overflowed = share <= ProjectRules.Epsilon;
                }

                if (GoldVein.IsDug(task))
                {
                    var perShare = task.Outputs.Sum(o => o.Amount);
                    share = GoldVein.Take(village, perShare * share) / perShare;
                    overflowed = share <= ProjectRules.Epsilon;
                }

                foreach (var output in task.Outputs)
                {
                    Produce(output.Resource, output.Amount * share);
                }

                if (task.Workplace == Forest.Workplace)
                {
                    Forest.Chop(village, task.Outputs.Where(o => o.Resource == ResourceKind.Wood).Sum(o => o.Amount * share), now, citizen.Spot);
                }

                break;
            }

            case TaskEffect.Unload:
                produced.AddRange(StorageRules.Unload(citizen, village));
                overflowed = citizen.Load.Count > 0;
                experienceShare = UnloadExperienceMultiplier;
                break;

            case TaskEffect.Sow:
                overflowed = !FarmRules.Sow(village, task.Crop!.Value, now);
                break;

            case TaskEffect.Water:
                overflowed = FarmRules.Water(village, now) == 0;
                break;

            case TaskEffect.Harvest:
            {
                var crop = FarmRules.Harvest(village, now);
                overflowed = crop is null;
                foreach (var output in crop?.Harvest ?? [])
                {
                    Produce(output.Resource, output.Amount * factor);
                }

                break;
            }

            case TaskEffect.Morale:
                if (village.Morale >= GameRules.MoraleMaximum)
                {
                    overflowed = true;
                }
                else
                {
                    var before = village.Morale;
                    village.AddMorale(GameRules.MoralePerPerformYield * task.BaseYield * factor);
                    moraleGained = village.Morale - before;
                }

                break;

            case TaskEffect.Deliver:
            {
                var output = task.Outputs[0];
                var key = output.Resource.ToString();
                var amount = project is null
                    ? 0
                    : Min(output.Amount * factor, Available(citizen, village, output.Resource), project.Remaining(key),
                        project.ContributionRoom(citizen.Id, key, context.ActiveCitizenCount));

                if (amount <= ProjectRules.Epsilon)
                {
                    overflowed = true;
                    break;
                }

                TakeFromHandsThenStorage(citizen, village, output.Resource, amount);
                project!.Record(citizen.Id, key, amount);
                contributed = amount;
                break;
            }

            case TaskEffect.Survey:
            case TaskEffect.Build:
            {
                var key = task.Effect == TaskEffect.Survey ? VillageProject.SurveyKey : VillageProject.BuildKey;
                var amount = 0.0;
                if (project is not null)
                {
                    var limit = task.Effect == TaskEffect.Survey
                        ? project.Remaining(key)
                        : Math.Max(0, project.BuildLimit - project.BuildProgress);
                    amount = Min(task.BaseYield * factor, limit,
                        project.ContributionRoom(citizen.Id, key, context.ActiveCitizenCount));
                }

                if (amount <= ProjectRules.Epsilon)
                {
                    overflowed = true;
                    break;
                }

                project!.Record(citizen.Id, key, amount);
                contributed = amount;
                break;
            }

            case TaskEffect.Supervise:
                overflowed = project is null || project.Phase != ProjectPhase.Construction || project.BuildDone;
                break;

            case TaskEffect.Expedition:
            {
                var scout = task.RoleId == "scout";
                for (var roll = 0; roll < task.BaseYield; roll++)
                {
                    var loot = Expeditions.Roll(context.Random);
                    Produce(loot.Resource, loot.Amount * factor);
                }

                if (!citizen.CycleTired
                    && context.Random.NextDouble() < Expeditions.RareChance(citizen.Stats.Dexterity, scout))
                {
                    Produce(Expeditions.RareFind.Resource, Expeditions.RareFind.Amount);
                    rareFind = true;
                }

                break;
            }

            case TaskEffect.Roam:
            {
                var found = new Dictionary<ResourceKind, double>();
                var chance = Roaming.FindChance(citizen.Stats.Dexterity);
                for (var glance = 0; glance < Roaming.Glances; glance++)
                {
                    if (context.Random.NextDouble() < chance)
                    {
                        var loot = Loot.Roll(Roaming.Table, context.Random);
                        found[loot.Resource] = found.GetValueOrDefault(loot.Resource) + loot.Amount;
                    }
                }

                foreach (var (kind, amount) in found)
                {
                    Produce(kind, amount * factor);
                }

                break;
            }
        }

        if (task.Effect != TaskEffect.Unload)
        {
            experienceShare = overflowed ? GameRules.OverflowExperienceMultiplier : 1;
            if (carried.Count > 0)
            {
                overflowed = true;
                var storedShare = 1 - carried.Sum(c => c.Amount) / produced.Sum(p => p.Amount);
                experienceShare = GameRules.OverflowExperienceMultiplier + (1 - GameRules.OverflowExperienceMultiplier) * storedShare;
            }
        }

        // Prey differ in hunting time; experience follows the time, so every prey gives the same experience per minute.
        if (task.Hunt is not null)
        {
            experienceShare *= task.WorkSeconds / GameRules.CycleLength.TotalSeconds;
        }

        var hungry = task.Effect != TaskEffect.Unload && Eat(citizen, village, now);
        return Finish(citizen, village, task.Id, task.Primary, task.Secondary, task.TierRequirement, experienceShare, hungry,
            produced, carried, moraleGained, contributed, rareFind, overflowed);
    }

    private static CycleReport? SettleJob(Citizen citizen, WorkContext context, DateTimeOffset now)
    {
        var job = BuildingCatalog.FindJob(citizen.CurrentTaskId!);
        if (job is null)
        {
            return null;
        }

        var village = context.Village;
        var plotId = citizen.CurrentSiteId!;
        var stage = citizen.CurrentSiteStage ?? 0;
        var site = village.Sites.SingleOrDefault(s => s.PlotId == plotId);
        var factor = YieldFactor(citizen, village, job.Primary, job.Secondary) / GameRules.StatFactorBase;
        var amount = 0.0;
        var produced = new List<ResourceAmount>();
        var carried = new List<ResourceAmount>();

        if (site is not null && stage >= site.Stage && !site.IsFinished)
        {
            if (job.Id == BuildingCatalog.LendAHand)
            {
                var needed = ConstructionSite.CrewNeeded(site.Definition.Stages[stage], context.RecentCitizenCount);
                if (stage == site.Stage && site.HandCount(stage) < needed && !site.HasHand(stage, citizen.Id))
                {
                    site.AddHand(stage, citizen.Id);
                    amount = 1;
                }
            }
            else if (job.Kind == JobKind.Haul)
            {
                var resource = job.Resource!.Value;
                amount = Min(job.PerCycle * factor, Available(citizen, village, resource), site.Remaining(stage, job.Id));
                if (amount > ProjectRules.Epsilon)
                {
                    TakeFromHandsThenStorage(citizen, village, resource, amount);
                    site.Record(stage, job.Id, amount, citizen.Id);
                }
            }
            else if (stage == site.Stage)
            {
                amount = Min(job.PerCycle * factor, site.LaborRoom(stage, job.Id));
                if (amount > ProjectRules.Epsilon)
                {
                    site.Record(stage, job.Id, amount, citizen.Id);
                    if (job.Id == BuildingCatalog.FellTrees)
                    {
                        var wood = Forest.Fell(village, WorldMap.Footprint(plotId)!.Value,
                            amount * Forest.WoodPerTree / BuildingCatalog.FellWorkPerTree, now);
                        if (wood > ProjectRules.Epsilon)
                        {
                            produced.Add(new ResourceAmount(ResourceKind.Wood, wood));
                            var stored = village.Store(ResourceKind.Wood, wood);
                            if (wood - stored > ProjectRules.Epsilon)
                            {
                                citizen.Carry(ResourceKind.Wood, wood - stored);
                                carried.Add(new ResourceAmount(ResourceKind.Wood, wood - stored));
                            }
                        }
                    }
                }
            }
        }

        var overflowed = amount <= ProjectRules.Epsilon;
        var completed = site is null ? null : ConstructionRules.Advance(village, site, context.RecentCitizenCount, now);
        var hungry = Eat(citizen, village, now);

        var report = Finish(citizen, village, job.Id, job.Primary, job.Secondary, 1,
            overflowed ? GameRules.OverflowExperienceMultiplier : 1, hungry,
            produced, carried, 0, overflowed ? 0 : amount, false, overflowed);
        return report with { SiteId = plotId, BuildingCompleted = completed };
    }

    private static CycleReport Finish(
        Citizen citizen,
        Village village,
        string taskId,
        Stat primary,
        Stat secondary,
        int tierRequirement,
        double experienceShare,
        bool hungry,
        IReadOnlyList<ResourceAmount> produced,
        IReadOnlyList<ResourceAmount> carried,
        double moraleGained,
        double contributed,
        bool rareFind,
        bool overflowed)
    {
        var experience = GameRules.BaseExperiencePerCycle
                         * Progression.ChallengeMultiplier(citizen.Stats.Get(primary), tierRequirement)
                         * (citizen.CycleTired ? GameRules.TiredExperienceMultiplier : 1)
                         * experienceShare
                         * (hungry ? GameRules.HungryExperienceMultiplier : 1)
                         * (village.IsLive ? GameRules.LiveExperienceMultiplier : 1);
        var secondaryExperience = experience * GameRules.SecondaryExperienceShare;

        var primaryResult = Progression.AddExperience(citizen.Stats, primary, experience);
        var secondaryResult = Progression.AddExperience(citizen.Stats, secondary, secondaryExperience);
        var renown = primaryResult.RenownGained + secondaryResult.RenownGained;
        citizen.Renown += renown;

        return new CycleReport(
            citizen.Id,
            citizen.Name,
            taskId,
            produced,
            carried,
            moraleGained,
            contributed,
            rareFind,
            primary,
            experience,
            secondary,
            secondaryExperience,
            primaryResult.LevelsGained + secondaryResult.LevelsGained,
            renown,
            citizen.CycleTired,
            overflowed,
            Hungry: hungry);
    }

    /// <summary>
    /// Every finished cycle eats food from the citizen's hands first, then from storage.
    /// Returns true when there was not enough food.
    /// </summary>
    private static bool Eat(Citizen citizen, Village village, DateTimeOffset now)
    {
        var eaten = citizen.TakeFromLoad(ResourceKind.Food, GameRules.FoodPerCycle);
        eaten += village.Take(ResourceKind.Food, GameRules.FoodPerCycle - eaten);
        var hungry = eaten < GameRules.FoodPerCycle - ProjectRules.Epsilon;
        if (hungry != citizen.IsHungry)
        {
            citizen.FlushEnergy(now);
            citizen.IsHungry = hungry;
        }

        return hungry;
    }

    /// <summary>Stat factor with tiredness, morale and streak.</summary>
    private static double YieldFactor(Citizen citizen, Village village, Stat primary, Stat secondary) =>
        YieldMath.StatFactor(YieldMath.EffectiveStat(citizen.Stats.Get(primary), citizen.Stats.Get(secondary)))
        * (citizen.CycleTired ? GameRules.TiredYieldMultiplier : 1)
        * village.Morale
        * YieldMath.StreakFactor(citizen.Streak);

    private static void CheckReady(Citizen citizen, DateTimeOffset now)
    {
        if (citizen.Status != CitizenStatus.Active)
        {
            throw new GameRuleException("citizen.dormant", "Dormant citizens cannot work.");
        }

        if (citizen.CycleEndsAt is not null && now < citizen.CycleEndsAt)
        {
            throw new GameRuleException("task.cycleRunning", "The current task is not finished yet.");
        }

        if (!citizen.CycleSettled)
        {
            throw new InvalidOperationException("The previous cycle must be settled before starting a new one.");
        }
    }

    private static void Begin(Citizen citizen, string taskId, TimeSpan duration, DateTimeOffset now)
    {
        var renewing = citizen.GraceEndsAt is not null && now <= citizen.GraceEndsAt;
        citizen.Streak = renewing ? citizen.Streak + 1 : 0;

        var energy = citizen.CurrentEnergy(now);
        citizen.CycleTired = energy < GameRules.EnergyCostPerCycle;
        citizen.StoredEnergy = citizen.CycleTired ? energy : energy - GameRules.EnergyCostPerCycle;
        citizen.EnergyUpdatedAt = now;

        citizen.CurrentTaskId = taskId;
        citizen.CurrentTechnologyId = null;
        citizen.Spot = null;
        citizen.IdleSpot = null;
        citizen.CycleStartedAt = now;
        citizen.CycleEndsAt = now + duration;
        citizen.CycleSettled = false;
    }

    /// <summary>Walking time of a task: from where the work happens to where its result is dropped.</summary>
    public static TimeSpan Walk(Citizen citizen, Village village, TaskDefinition task) =>
        Walk(citizen, village, task, Workplace(village, task));

    /// <summary>Where the task is done: the yard of the building it needs once one stands, else its place on the map.</summary>
    public static TilePoint Workplace(Village village, TaskDefinition task) =>
        task.RequiredBuilding is { } kind && village.Structures.FirstOrDefault(s => s.Kind == kind) is { } building
            ? WorldMap.Yard(building.PlotId)
            : WorldMap.Place(task.Workplace);

    /// <summary>
    /// A camp tree the player picked is walked to and from as it is. Other spots cannot be checked,
    /// so they may make the walk longer than from the workplace, never shorter.
    /// </summary>
    private static TimeSpan Walk(Citizen citizen, Village village, TaskDefinition task, TilePoint? spot, DateTimeOffset now)
    {
        if (FarmRules.Walk(village, task, now) is { } farmWalk)
        {
            return farmWalk;
        }

        var standard = Walk(citizen, village, task);
        if (spot is not { } picked)
        {
            return standard;
        }

        var fromSpot = Walk(citizen, village, task, picked);
        return WorkSpots.TreeAt(village, task, picked, now) is not null || fromSpot > standard ? fromSpot : standard;
    }

    private static TimeSpan Walk(Citizen citizen, Village village, TaskDefinition task, TilePoint place)
    {
        var camp = WorldMap.Places[WorldMap.Camp];
        return task.Effect switch
        {
            TaskEffect.Resources when task.Outputs.Count > 0 =>
                WorldMap.Walk(place, village.DropPoint(task.Outputs[0].Resource, place)),
            TaskEffect.Expedition => WorldMap.Walk(place, camp),
            TaskEffect.Unload when citizen.Load.Count > 0 => WorldMap.Walk(camp, village.DropPoint(citizen.Load[0].Kind, camp)),
            _ => TimeSpan.Zero,
        };
    }

    private static void CheckProjectTask(Citizen citizen, TaskDefinition task, WorkContext context)
    {
        if (task.Effect is not (TaskEffect.Deliver or TaskEffect.Survey or TaskEffect.Build or TaskEffect.Supervise))
        {
            return;
        }

        var project = context.Project is { Status: ProjectStatus.Active } active
            ? active
            : throw new GameRuleException("project.none", "There is no construction project right now.");

        switch (task.Effect)
        {
            case TaskEffect.Deliver:
            {
                var resource = task.Outputs[0].Resource;
                var key = resource.ToString();
                if (project.Required(key) <= 0)
                {
                    throw new GameRuleException("project.materialNotNeeded", $"The project does not need {ResourceNames.Of(resource)}.", Resource(resource));
                }

                if (project.Remaining(key) <= ProjectRules.Epsilon)
                {
                    throw new GameRuleException("project.materialComplete", $"Enough {ResourceNames.Of(resource)} has already been delivered.", Resource(resource));
                }

                RequireRoom(project, citizen, key, context);
                if (Available(citizen, context.Village, resource) < 1)
                {
                    throw new GameRuleException("project.storageEmpty", $"There is no {ResourceNames.Of(resource)} in storage. Gather some first.", Resource(resource));
                }

                break;
            }

            case TaskEffect.Survey:
                if (project.SurveyDone)
                {
                    throw new GameRuleException("project.surveyDone", "The survey is already finished.");
                }

                RequireRoom(project, citizen, VillageProject.SurveyKey, context);
                break;

            case TaskEffect.Build:
                if (!project.SurveyDone)
                {
                    throw new GameRuleException("project.surveyFirst", "An engineer must survey the site first.");
                }

                if (project.BuildDone)
                {
                    throw new GameRuleException("project.buildDone", "Construction is already finished.");
                }

                if (project.BuildProgress >= project.BuildLimit - ProjectRules.Epsilon)
                {
                    throw new GameRuleException("project.needMaterials", "Not enough materials: deliver them to the site.");
                }

                if (project.Definition.NeedsEngineer && context.BuildersWorking >= context.SupervisionCapacity)
                {
                    throw context.SupervisionCapacity == 0
                        ? new GameRuleException("project.needSupervisor", "An engineer must supervise the construction.")
                        : new GameRuleException("project.supervisionFull", "All supervised builder places are taken. More engineers are needed.");
                }

                RequireRoom(project, citizen, VillageProject.BuildKey, context);
                break;

            case TaskEffect.Supervise:
                if (!project.Definition.NeedsEngineer)
                {
                    throw new GameRuleException("project.noSupervision", "This work is small enough to build without an engineer.");
                }

                if (!project.SurveyDone)
                {
                    throw new GameRuleException("project.surveyFirst", "An engineer must survey the site first.");
                }

                if (project.BuildDone)
                {
                    throw new GameRuleException("project.buildDone", "Construction is already finished.");
                }

                break;
        }
    }

    private static void RequireRoom(VillageProject project, Citizen citizen, string key, WorkContext context)
    {
        if (project.ContributionRoom(citizen.Id, key, context.ActiveCitizenCount) <= ProjectRules.Epsilon)
        {
            throw new GameRuleException("project.shareTaken", "You already did your share of this part of the project. Let others work.");
        }
    }

    private static Dictionary<string, object> Resource(ResourceKind kind) => new() { ["resource"] = kind.ToString() };

    private static void CheckPrey(Village village, TaskDefinition task, DateTimeOffset now)
    {
        if (task.Hunt is not { } hunt || village.HerdOf(hunt.Prey).Current(now) >= WildlifeCatalog.MinimumToHunt)
        {
            return;
        }

        throw new GameRuleException("hunt.noAnimals",
            $"No {WildlifeCatalog.Get(hunt.Prey).Name} are left near the camp. Give them time to come back.",
            new Dictionary<string, object> { ["animal"] = hunt.Prey.ToString() });
    }

    /// <summary>
    /// A citizen holding goods that did not fit in storage cannot produce more of the same good.
    /// Expedition and roaming loot is random, so they need empty hands. Carrying to storage needs a load and room.
    /// </summary>
    private static void CheckHands(Citizen citizen, Village village, TaskDefinition task, DateTimeOffset now)
    {
        if (task.Effect == TaskEffect.Unload)
        {
            if (citizen.Load.Count == 0)
            {
                throw new GameRuleException("hands.empty", "Your hands are empty.");
            }

            if (citizen.Load.All(g => village.FreeSpace(g.Kind) <= ProjectRules.Epsilon))
            {
                throw new GameRuleException("hands.noRoom", "Storage has no room for what you carry. The Governor must build more storage.");
            }

            return;
        }

        if (task.Effect == TaskEffect.Expedition && citizen.Load.Count > 0)
        {
            throw new GameRuleException("hands.fullExpedition", "Your hands are full. Unload or drop your load before an expedition.");
        }

        if (task.Effect == TaskEffect.Roam && citizen.Load.Count > 0)
        {
            throw new GameRuleException("hands.fullRoam", "Your hands are full. Unload or drop your load before roaming.");
        }

        var outputs = task.Effect switch
        {
            TaskEffect.Resources => task.Outputs,
            TaskEffect.Harvest when FarmRules.RipeBed(village, now) is { Crop: { } crop } => FarmCatalog.Get(crop).Harvest,
            _ => [],
        };
        var blocked = outputs.FirstOrDefault(o => citizen.Carried(o.Resource) > 0);
        if (blocked is not null)
        {
            throw new GameRuleException("hands.full",
                $"Your hands are full of {ResourceNames.Of(blocked.Resource)}: storage has no room for it. Carry it to storage or pick another job.",
                Resource(blocked.Resource));
        }
    }

    private static double Available(Citizen citizen, Village village, ResourceKind kind) =>
        citizen.Carried(kind) + village.Stock(kind);

    private static void TakeFromHandsThenStorage(Citizen citizen, Village village, ResourceKind kind, double amount)
    {
        var fromHands = citizen.TakeFromLoad(kind, amount);
        village.Take(kind, amount - fromHands);
    }

    private static double Min(params double[] values) => values.Min();
}
