#:project ../../src/Desertod.Domain/Desertod.Domain.csproj
// Prints pacing tables from the live catalogs: dotnet run tools/Balance/balance.cs
using System.Globalization;
using Desertod.Domain.Buildings;
using Desertod.Domain.Catalog;
using Desertod.Domain.Projects;
using Desertod.Domain.Rules;
using Desertod.Domain.Stats;
using Desertod.Domain.Technologies;
using Desertod.Domain.World;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
const double Reaction = 5;
const double SiteWalk = 15;
var camp = WorldMap.Places[WorldMap.Camp];

double WalkSeconds(TaskDefinition t) => t.Effect switch
{
    TaskEffect.Resources when t.Outputs.Count > 0 => WorldMap.Walk(WorldMap.Place(t.Workplace), camp).TotalSeconds,
    TaskEffect.Expedition => WorldMap.Walk(WorldMap.Place(t.Workplace), camp).TotalSeconds,
    _ => 0,
};
double CycleSeconds(TaskDefinition t) => t.WorkSeconds + WalkSeconds(t) + Reaction;
TaskDefinition Task(string id) => GameCatalog.FindTask(id)!;

int Cumulative(int level) => Enumerable.Range(0, level).Sum(Progression.Curve);
double Factor(double stat) => YieldMath.StatFactor(stat);

Console.WriteLine("## A. Cycle length (empty village, goods dropped at the camp, 5 s to renew)\n");
Console.WriteLine("| Task | Work s | Walk s | Cycle s | Cycles/hour |");
Console.WriteLine("|---|---|---|---|---|");
foreach (var id in new[] { "chop-wood", "quarry-stone", "forage", "cut-reeds", "hunt", "track-game", "hunt-birds", "measure-river", "dig-cave", "tend-fire", "read-sky", "keep-watch", "carry-to-storage", "deliver-wood", "build" })
{
    var t = Task(id);
    Console.WriteLine($"| {t.Name} | {t.WorkSeconds} | {WalkSeconds(t)} | {CycleSeconds(t)} | {3600 / CycleSeconds(t):0.0} |");
}

var gatherCycle = new[] { "chop-wood", "quarry-stone", "forage", "cut-reeds" }.Average(id => CycleSeconds(Task(id)));
Console.WriteLine($"\nTypical gathering cycle: {gatherCycle:0} s = {3600 / gatherCycle:0.0} cycles per hour.\n");

Console.WriteLine("## B. Stat or skill level from zero (primary experience, 10 per full cycle)\n");
Console.WriteLine("| Level | Cumulative XP | Full cycles | Play hours |");
Console.WriteLine("|---|---|---|---|");
for (var level = 1; level <= 10; level++)
{
    var cycles = Math.Ceiling(Cumulative(level) / 10.0);
    Console.WriteLine($"| {level} | {Cumulative(level)} | {cycles} | {cycles * gatherCycle / 3600:0.0} |");
}

Console.WriteLine("\n## C. Energy: full-experience cycles per day\n");
Console.WriteLine("| Endurance | Pool | Under trees (×0.5) | Lean-to (×0.75) | Hut (×1) |");
Console.WriteLine("|---|---|---|---|---|");
foreach (var endurance in new[] { 0, 2, 4, 6, 8, 10 })
{
    var perDay = Energy.RegenerationPerHour(endurance) * 24;
    string Cell(double q) => $"{perDay * q:0} ({perDay * q * gatherCycle / 3600:0.0} h)";
    Console.WriteLine($"| {endurance} | {Energy.Maximum(endurance):0} | {Cell(0.5)} | {Cell(0.75)} | {Cell(1)} |");
}

// D. Calendar simulation: a citizen gathering with Endurance as the secondary stat.
var milestones = new (string Name, int Cycles)[]
{
    ("Stat 2 (Camp discoveries)", 11), ("Stat 3 / journeyman", 28), ("Stat 4", 60), ("Stat 5", 108),
    ("Stat 6", 175), ("Master craft 7", 263), ("Stat 10", 672),
};
var profiles = new (string Name, Func<int, bool> PlaysOn, double Hours)[]
{
    ("Weekly stream 3 h", d => d % 7 == 0, 3),
    ("3 streams a week, 3 h", d => d % 7 is 0 or 2 or 4, 3),
    ("Daily 1 h", _ => true, 1),
    ("Daily 3 h", _ => true, 3),
    ("Daily 8 h", _ => true, 8),
    ("Bot 24/7", _ => true, 24),
};

Dictionary<int, (double Day, double Hours)> Simulate(Func<int, bool> playsOn, double hours, double shelter, int days = 120)
{
    var result = new Dictionary<int, (double, double)>();
    var enduranceXp = 0;
    int Endurance() { var l = 0; while (l < 10 && enduranceXp >= Cumulative(l + 1)) l++; return l; }
    var energy = Energy.Maximum(0);
    var full = 0;
    var played = 0.0;
    var step = gatherCycle / 3600;
    for (var day = 0; day < days; day++)
    {
        var start = day * 24.0 + (hours >= 24 ? 0 : 19 - Math.Min(hours, 19));
        var time = day * 24.0;
        void Regen(double until)
        {
            var e = Endurance();
            energy = Math.Min(Energy.Maximum(e), energy + (until - time) * Energy.RegenerationPerHour(e) * shelter);
            time = until;
        }

        if (playsOn(day))
        {
            Regen(start);
            for (var t = 0.0; t + step <= hours + 1e-9; t += step)
            {
                if (energy >= 1)
                {
                    energy -= 1;
                    full++;
                    enduranceXp += 3;
                    foreach (var m in milestones.Where(m => m.Cycles == full))
                    {
                        result[m.Cycles] = (day + 1, played + step);
                    }
                }

                played += step;
                Regen(time + step);
            }
        }

        Regen((day + 1) * 24.0);
    }

    return result;
}

foreach (var (label, shelter) in new[] { ("under the trees", 0.5), ("in a hut", 1.0) })
{
    Console.WriteLine($"\n## D. Calendar day / hours played to reach a level, sleeping {label}\n");
    Console.WriteLine("| Profile | " + string.Join(" | ", milestones.Select(m => m.Name)) + " |");
    Console.WriteLine("|---|" + string.Concat(milestones.Select(_ => "---|")));
    foreach (var p in profiles)
    {
        var r = Simulate(p.PlaysOn, p.Hours, shelter);
        Console.WriteLine($"| {p.Name} | " + string.Join(" | ", milestones.Select(m =>
            r.TryGetValue(m.Cycles, out var v) ? $"day {v.Day:0} ({v.Hours:0.#} h)" : "> 120 days")) + " |");
    }
}

Console.WriteLine("\n## E. Discoveries: full cycles one fresh citizen needs to qualify as a researcher\n");
Console.WriteLine("| Discovery | Era | Needs | Cycles | Play hours | Research cycles × crew |");
Console.WriteLine("|---|---|---|---|---|---|");
foreach (var tech in TechnologyCatalog.Technologies)
{
    var reqs = tech.Researcher.OrderByDescending(r => r.Minimum).ToList();
    var first = Math.Ceiling(Cumulative(reqs[0].Minimum) / 10.0);
    var extra = reqs.Count > 1 ? Math.Max(0, Math.Ceiling((Cumulative(reqs[1].Minimum) - 3 * first) / 10.0)) : 0;
    var cycles = first + extra;
    Console.WriteLine($"| {tech.Name} | {tech.Era} | {string.Join(", ", reqs.Select(r => $"{r.Stat} {r.Minimum}"))} | {cycles} | {cycles * gatherCycle / 3600:0.0} | {tech.Cycles} × {tech.Crew} |");
}

// F. Buildings: person-cycles of gathering, hauling and labor.
var gatherTask = new Dictionary<ResourceKind, string>
{
    [ResourceKind.Wood] = "chop-wood", [ResourceKind.Stone] = "quarry-stone", [ResourceKind.Reeds] = "cut-reeds",
    [ResourceKind.Leather] = "hunt", [ResourceKind.Food] = "forage", [ResourceKind.Gold] = GoldVein.DigTask,
};

double GatherPerCycle(ResourceKind kind, double stat)
{
    if (kind == ResourceKind.Leather) return 1;
    if (kind == ResourceKind.Planks) return YieldMath.Whole(2 * Factor(stat));
    var t = Task(gatherTask[kind]);
    return YieldMath.Whole(t.Outputs.First(o => o.Resource == kind).Amount * Factor(stat));
}

double GatherSeconds(ResourceKind kind) =>
    kind == ResourceKind.Planks ? 90 + Reaction : CycleSeconds(Task(gatherTask[kind]));

(double Cycles, double Hours) GatherCost(ResourceKind kind, int amount, double stat)
{
    var cycles = Math.Ceiling(amount / GatherPerCycle(kind, stat));
    var seconds = cycles * GatherSeconds(kind);
    if (kind == ResourceKind.Planks)
    {
        var wood = GatherCost(ResourceKind.Wood, amount, stat);
        cycles += wood.Cycles;
        seconds += wood.Hours * 3600;
    }

    return (cycles, seconds / 3600);
}

(double Cycles, double Hours, int Crew) BuildingCost(BuildingDefinition b, double stat)
{
    double cycles = 0, seconds = 0;
    var siteFactor = Factor(stat) / GameRules.StatFactorBase;
    foreach (var stage in b.Stages)
    {
        foreach (var job in stage.Jobs)
        {
            var def = BuildingCatalog.FindJob(job.JobId)!;
            var per = YieldMath.Whole(def.PerCycle * siteFactor);
            var n = Math.Ceiling(job.Amount / (double)per);
            cycles += n;
            seconds += n * ((def.Kind == JobKind.Haul ? BuildingCatalog.HaulBaseTime.TotalSeconds : 90) + SiteWalk + Reaction);
            if (def.Kind == JobKind.Haul)
            {
                var g = GatherCost(def.Resource!.Value, job.Amount, stat);
                cycles += g.Cycles;
                seconds += g.Hours * 3600;
            }
        }
    }

    return (cycles, seconds / 3600, b.Stages.Max(s => s.Crew));
}

Console.WriteLine("\n## F. Buildings: person-cycles and person-hours including gathering the material (first copy)\n");
Console.WriteLine("| Building | Crew | Stats 0: cycles | Stats 0: hours | Stats 3: cycles | Stats 3: hours |");
Console.WriteLine("|---|---|---|---|---|---|");
foreach (var b in BuildingCatalog.Buildings.Where(b => b.Buildable))
{
    var low = BuildingCost(b, 0);
    var mid = BuildingCost(b, 2.4);
    Console.WriteLine($"| {b.Name} | {low.Crew} | {low.Cycles} | {low.Hours:0.0} | {mid.Cycles} | {mid.Hours:0.0} |");
}

var deliverTask = new Dictionary<ResourceKind, string>
{
    [ResourceKind.Wood] = "deliver-wood", [ResourceKind.Stone] = "deliver-stone", [ResourceKind.Reeds] = "deliver-reeds",
    [ResourceKind.Gold] = "deliver-gold",
};

(double Cycles, double Hours) ProjectCost(ProjectDefinition p, double stat)
{
    double cycles = 0, seconds = 0;
    foreach (var m in p.Materials)
    {
        var g = GatherCost(m.Resource, m.Amount, stat);
        var per = YieldMath.Whole(Task(deliverTask[m.Resource]).Outputs[0].Amount * Factor(stat));
        var n = Math.Ceiling(m.Amount / (double)per);
        cycles += g.Cycles + n;
        seconds += g.Hours * 3600 + n * (90 + Reaction);
    }

    var work = p.SurveyCycles + p.BuildCycles;
    cycles += work;
    seconds += work * (90 + Reaction);
    return (cycles, seconds / 3600);
}

Console.WriteLine("\n## G. Story chain: person-cycles and person-hours\n");
Console.WriteLine("| Step | Stats 0: cycles | Stats 0: hours | Stats 3: cycles | Stats 3: hours |");
Console.WriteLine("|---|---|---|---|---|");
double total0 = 0, total3 = 0, totalH0 = 0, totalH3 = 0;
void Row(string name, (double C, double H) a, (double C, double H) b)
{
    total0 += a.C; total3 += b.C; totalH0 += a.H; totalH3 += b.H;
    Console.WriteLine($"| {name} | {a.C} | {a.H:0.0} | {b.C} | {b.H:0.0} |");
}

var fire = Task("tend-fire");
Row("Fire (5 × tend the fire)", (5, 5 * CycleSeconds(fire) / 3600), (5, 5 * CycleSeconds(fire) / 3600));
Row("Shelter (lean-to)", BuildingCost(BuildingCatalog.Get(BuildingKind.LeanTo), 0) is var l0 ? (l0.Cycles, l0.Hours) : default,
    BuildingCost(BuildingCatalog.Get(BuildingKind.LeanTo), 2.4) is var l3 ? (l3.Cycles, l3.Hours) : default);
Row("Stores (20 food)", GatherCost(ResourceKind.Food, 20, 0), GatherCost(ResourceKind.Food, 20, 2.4));
Row("First house (hut)", BuildingCost(BuildingCatalog.Get(BuildingKind.Hut), 0) is var h0 ? (h0.Cycles, h0.Hours) : default,
    BuildingCost(BuildingCatalog.Get(BuildingKind.Hut), 2.4) is var h3 ? (h3.Cycles, h3.Hours) : default);
foreach (var id in new[] { ProjectCatalog.TsarMonument, ProjectCatalog.FishingPier, ProjectCatalog.Palisade, ProjectCatalog.TsarBridge })
{
    var p = ProjectCatalog.Find(id)!;
    Row(p.Name, ProjectCost(p, 0), ProjectCost(p, 2.4));
}

var dig = Task(GoldVein.DigTask);
Row("Monks' gold (20 digs)", (20, 20 * CycleSeconds(dig) / 3600), (20, 20 * CycleSeconds(dig) / 3600));
foreach (var id in new[] { ProjectCatalog.BellTower, ProjectCatalog.GreatGranary })
{
    var p = ProjectCatalog.Find(id)!;
    Row(p.Name, ProjectCost(p, 0), ProjectCost(p, 2.4));
}

Console.WriteLine($"| **Total** | {total0} | {totalH0:0} | {total3} | {totalH3:0} |");

// H. Buildings needed to research the whole tree.
var needed = new HashSet<BuildingKind>();
void Need(BuildingKind k) { if (needed.Add(k)) foreach (var r in BuildingCatalog.Get(k).Requires) Need(r); }
foreach (var t in TechnologyCatalog.Technologies) Need(t.ResearchedAt);
var treeCycles0 = needed.Sum(k => BuildingCost(BuildingCatalog.Get(k), 0).Hours);
var treeCycles3 = needed.Sum(k => BuildingCost(BuildingCatalog.Get(k), 2.4).Hours);
var research = TechnologyCatalog.Technologies.Sum(t => t.Cycles) * (90 + SiteWalk + Reaction) / 3600;
Console.WriteLine($"\n## H. Whole discovery tree\n");
Console.WriteLine($"Buildings needed: {string.Join(", ", needed)}");
Console.WriteLine($"Person-hours of those buildings: {treeCycles0:0} at stats 0, {treeCycles3:0} at stats 3. Research work itself: {research:0.0} person-hours.");
