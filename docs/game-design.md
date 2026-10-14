# Desertod — Game Design: Stats, Skills, Tasks, Progression

Status: draft v0.1. All numbers are tuning parameters, collected in [Tuning parameters](#10-tuning-parameters).

## 1. Concept recap

- A **streamer** hosts a **Village**. Any streamer on the platform can host one.
- **Citizens** are the streamer's subscribers. Each person has one **Account** with one **Character** (name and look), created right after signing up. Opening a Village settles the Character there as a **Citizen**; stats, skills and renown are per Citizen, so every Village starts from scratch.
- Citizens work in a browser page (clicker style): pick a task, it runs for **90 seconds**, then must be renewed or changed.
- The village is rendered as a 2D map in the browser.

## 2. Stats

Five stats, range **0–10**. Stats are generic: they are what a Citizen *is*. What a Citizen *can do* is set by skills (section 3).

| Stat | Meaning | Main uses |
|---|---|---|
| Strength | Raw physical power | Yield of heavy work; Woodcutting, Quarrying, Mining, Smithing; fight power |
| Dexterity | Precision, speed | Gathering, Hunting, Fishing, Exploring, Baking, Carpentry, Weaponcraft; rare finds; fight power |
| Intelligence | Knowledge, planning | Building, Scholarship, Weatherlore; engineer supervision |
| Charisma | Social influence | Entertaining, Trading |
| Endurance | Stamina | Size of the energy pool (how long a Citizen works at full efficiency); Farming, Hauling, Guarding; captain's command |

### 2.1 Character creation

- Everyone starts wild: every stat at **0**, grown only by working. Creation picks only the name and the look.

### 2.2 Talent cap

- The sum of all five stats cannot exceed **30** (out of a theoretical 50).
- A Citizen can be great at two things, decent at one, or average at everything, but never great at everything.
- Because every skill level needs stats (section 3.1), the cap also decides which crafts a Citizen can master. This makes Citizens depend on each other, which is the whole point of a community game.
- When a stat's experience would push the total over the cap, that experience is converted into **Renown** (prestige currency for cosmetics, titles, house decorations). Effort is never wasted, but power does not grow.

## 3. Skills and professions

Professions are **not** a class choice. Every task trains one **skill** (a craft) and needs its own level of that skill; a profession is the title a Citizen earns at a level of the craft. A Citizen may perform any task they have the skill level for and can switch every cycle. The highest profession held is shown as the Citizen's title.

### 3.1 Skills rest on stats

Every skill has a primary and a secondary stat. Level L of a skill needs:

```text
Primary   ≥ L − 1
Secondary ≥ ⌊(L − 1) / 2⌋
```

| Skill | Primary / Secondary | Skill | Primary / Secondary |
|---|---|---|---|
| Woodcutting | Strength / Endurance | Carpentry | Dexterity / Strength |
| Quarrying | Strength / Endurance | Weaponcraft | Dexterity / Strength |
| Mining | Strength / Endurance | Farming | Endurance / Dexterity |
| Smithing | Strength / Dexterity | Hauling | Endurance / Strength |
| Gathering | Dexterity / Intelligence | Guarding | Endurance / Strength |
| Hunting | Dexterity / Endurance | Building | Intelligence / Strength |
| Fishing | Dexterity / Endurance | Scholarship | Intelligence / Charisma |
| Exploring | Dexterity / Endurance | Weatherlore | Intelligence / Endurance |
| Baking | Dexterity / Endurance | Entertaining | Charisma / Dexterity |
| | | Trading | Charisma / Intelligence |

Examples: Smithing 2 needs Strength 1; Hunting 4 needs Dexterity 3 and Endurance 1; a master craft (level 7) needs 6 and 3.

When a skill has the experience for its next level but not the stats, the level **waits**: experience up to that one level is kept, the rest is lost, and the level comes as soon as the stats catch up. Since a stat gets the same experience as the skill trained with it, a specialist rarely waits; the gate bites when the talent cap stops a stat from growing. A Citizen with Strength, Dexterity and Endurance at 10 cannot get past Scholarship 1.

### 3.2 Professions

Three tiers. A task's skill level is its only gate besides the village's buildings and discoveries; the profession is a title and the group its tasks are listed under.

| Tier | Craft level | Typical time to reach (primary experience) |
|---|---|---|
| Basic (Laborer) | — | immediately |
| Journeyman | 3 | ~0.7 active hours |
| Master | 7 | ~6.6 active hours |

| Craft | Journeyman (3) | Master (7) | Work by required level |
|---|---|---|---|
| Mining | Miner | Deep miner | Dig in the cave, Mine coal 0 · Mine iron ore 3 · Mine gold 7 |
| Smithing | Smith | Master smith | Smelt iron 0 · Forge swords 3 · Forge chains 7 |
| Carpentry | Carpenter | Artisan | Saw planks 0 · Make shields 3 · Build mechanisms 7 |
| Scholarship | Scholar | Alchemist | Measure the river, Read the chronicles, Brew vigor drinks 0 · Research 3 · Brew remedies 7 |
| Building | Engineer | Architect, Foreman | Build, Repair 0 · Survey, Supervise 3 · Draft plans, Lead a crew 7 (not available yet) |
| Guarding | Guard | Captain | Keep watch 0 · Patrol 3 · Command 7 |
| Exploring | — | Scout | Roam, Explore the land 0 · Go on an expedition 3 · Long expedition 7 |
| Trading | Merchant | Envoy | Trade 0 · Negotiate 7 (both not available yet) |
| Hunting | Hunter | — | Rabbits 0 · Deer 2 · Birds 3 |
| Farming | Farmer | — | Tend, water, harvest, turnips 0 · carrots 2 · cabbage 3 · flax 4 · rye 5 |
| Baking | Baker | — | Bake bread 0 |
| Entertaining | Bard | — | Tend the fire, Chat at the tavern, Tell stories 0 · Play the gusli 3 |
| Weatherlore | Synoptic | — | Read the sky 0 |
| Weaponcraft | — | — | Spears, leather armor, arrows 0 · Bows 2 |
| Woodcutting, Quarrying, Gathering, Fishing, Hauling | — | — | All work at 0 |

Every craft has work at level 0, so anyone can start it. Professions with a discovery (Miner and Deep miner: Mining; Smith: Smelting; Master smith: Forging; Carpenter: Carpentry; Artisan: Mechanics; Baker: Baking; Scholar: Writing; Alchemist: Alchemy; Bard: Music; Guard and Captain: Warfare; Synoptic: Weather lore) open their work only once the village has made it.

Crafts with work at 0, 3 and 7 always have a task that teaches in full (section 5.4). Building has no working master task yet, so Building above 6 learns at a quarter; the same goes for Trading and Entertaining above 6, and for single-task crafts above 3.

## 4. Tasks

### 4.1 Task cycle

- A task runs for **90 seconds** (one *cycle*). The duration is measured on the server; the client only displays it.
- When a cycle completes, its result is **banked automatically**. Work already done is never lost.
- After completion the player has a **60 second grace window** to renew or change the task.
- After the grace window the Citizen goes **idle** until the player picks a task again.
- Only **one active task** per Citizen. A new cycle can only start after the previous one completes. Clicking faster gives nothing: the server allows at most **40 cycles per hour**.

### 4.2 Task definition

Each task has:

| Field | Example (Mine coal) |
|---|---|
| Profession (where it is listed) | Miner |
| Skill and required level | Mining 0 |
| Primary stat | Strength |
| Secondary stat | Endurance |
| Base yield per cycle | 2 coal |
| Inputs consumed | none |
| Energy cost | 1 |

### 4.3 Task catalog (first version)

| Task | Profession | Skill level | Primary / Secondary | Base yield | Inputs |
|---|---|---|---|---|---|
| Chop wood | Laborer | Woodcutting 0 | Strength / Endurance | 3 wood | — |
| Quarry stone | Laborer | Quarrying 0 | Strength / Endurance | 2 stone | — |
| Forage | Laborer | Gathering 0 | Dexterity / Intelligence | 2 food | — |
| Tend fields | Laborer | Farming 0 | Endurance / Dexterity | 2 food | — |
| Read old books | Laborer | Scholarship 0 | Intelligence / Charisma | 0.5 research point | — |
| Chat at the tavern | Laborer | Entertaining 0 | Charisma / Intelligence | +morale (half of Perform) | — |
| Haul | Laborer | Hauling 0 | Endurance / Strength | +1 project progress | — |
| Mine coal | Miner | Mining 0 | Strength / Endurance | 2 coal | — |
| Mine iron ore | Miner | Mining 3 | Strength / Endurance | 1 iron ore | — |
| Smelt ingot | Smith | Smithing 0 | Strength / Dexterity | 1 iron ingot | 1 iron ore, 1 coal |
| Saw planks | Carpenter | Carpentry 0 | Dexterity / Strength | 2 planks | 2 wood |
| Hunt (pick the prey) | Laborer | Hunting 0 / 2 / 3 for rabbits / deer / birds | Dexterity / Endurance (birds: Intelligence) | see [cooperative-building.md](cooperative-building.md), Wildlife | — |
| Research | Scholar | Scholarship 3 | Intelligence / Charisma | 1 research point | — |
| Survey | Engineer | Building 3 | Intelligence / Dexterity | +1 survey progress | — |
| Supervise | Engineer | Building 3 | Intelligence / Charisma | enables construction (see 6.2) | — |
| Trade | Merchant | Trading 0 | Charisma / Intelligence | exchange at better rate | resources |
| Perform | Bard | Entertaining 3 | Charisma / Dexterity | +morale | — |
| Keep watch | Laborer | Guarding 0 | Endurance / Strength | defends the camp during a raid (section 12) | — |
| Repair buildings | Laborer | Building 0 | Strength / Dexterity | mends 25 damage of the most damaged building | 2 wood |
| Patrol | Guard | Guarding 3 | Endurance / Strength | defends the camp, ×1.25 (section 12) | — |
| Mine gold | Deep Miner | Mining 7 | Strength / Endurance | 0.5 gold | — |
| Forge chain | Master Smith | Smithing 7 | Strength / Dexterity | 1 chain | 2 iron ingot, 1 coal |
| Build mechanism | Artisan | Carpentry 7 | Dexterity / Intelligence | 1 mechanism | 2 planks, 1 iron ingot |
| Explore | Scout | Exploring 7 | Dexterity / Endurance | discovery roll | — |
| Design | Architect | Building 7 | Intelligence / Charisma | +1 blueprint progress | — |
| Brew remedy | Alchemist | Scholarship 7 | Intelligence / Dexterity | 1 remedy | 2 food |
| Lead crew | Foreman | Building 7 | Charisma / Intelligence | group bonus (see 6.3) | — |
| Negotiate | Envoy | Trading 7 | Charisma / Intelligence | +relation with another village | — |
| Command | Captain | Guarding 7 | Endurance / Strength | defends the camp; +4% defense per Endurance (section 12) | — |

## 5. Progression math

### 5.1 Experience gain per cycle

```text
XP_primary   = 10 × Challenge × Fatigue × Overflow
XP_secondary = 0.3 × XP_primary
```

- **Every task gives the same base experience (10).** Higher-tier tasks give better *yield*, not more experience. This removes the "find the single best experience task and spam it" exploit.
- The multipliers are anti-abuse rules (sections 5.4–5.6).

### 5.2 Experience curve

```text
XP_to_next(0) = 50
XP_to_next(L) = round(60 × L^1.5)         for L = 1..9
```

Experience is tracked per stat. At level 10 the stat is maxed and further experience becomes Renown.

**Pacing rule:** every technology (a profession tier, a story project) should be reachable in a few hours of play, about two in four hours. Citizens start with every stat at 0.

### 5.3 Pacing (calculated, primary stat only)

One active hour = 40 cycles = 400 experience at full efficiency.

| Level | XP to next | Cumulative XP | Active hours | Casual (4 h/week) | Regular (10 h/week) |
|---|---|---|---|---|---|
| 0 | 50 | 0 | 0 | — | — |
| 1 | 60 | 50 | 0.1 | — | — |
| 2 | 170 | 110 | 0.3 | 0.1 wk | — |
| 3 | 312 | 280 | 0.7 | 0.2 wk | 0.1 wk |
| 4 | 480 | 592 | 1.5 | 0.4 wk | 0.15 wk |
| 5 | 671 | 1 072 | 2.7 | 0.7 wk | 0.3 wk |
| 6 | 882 | 1 743 | 4.4 | 1.1 wk | 0.4 wk |
| 7 | 1 111 | 2 625 | 6.6 | 1.6 wk | 0.7 wk |
| 8 | 1 358 | 3 736 | 9.3 | 2.3 wk | 0.9 wk |
| 9 | 1 620 | 5 094 | 12.7 | 3.2 wk | 1.3 wk |
| 10 | — | 6 714 | 16.8 | 4.2 wk | 1.7 wk |

Skills use the same curve. A Journeyman (craft 3) takes about 0.7 hours of its craft; a Master (craft 7) about 6.6 hours, by which time the primary stat is near 7 as well, so the stat gate (section 3.1) only holds back a craft whose stats are spent elsewhere. The talent cap (30 stat points in total) still keeps a citizen from mastering everything, and the challenge rule (section 5.4) keeps them from farming trivial tasks. The energy-capped minimum times of the old, slower curve no longer apply and have not been recalculated.

### 5.4 Challenge: no farming trivial tasks

```text
Challenge = 1.0   if Skill ≤ TaskSkillLevel + 3
Challenge = 0.25  otherwise
```

The challenge compares the Citizen's level in the task's skill with the level the task needs. A Mining 4 Citizen digging coal (Mining 0) gets a quarter of the experience. To keep growing they must move up to iron ore (Mining 3), and at Mining 7 to gold. Crafts with work at 0, 3 and 7 always have a full-experience task; the crafts that lack one are listed at the end of section 3.2. Construction jobs count as skill level 0. Researching a technology still compares the researcher's primary stat with the technology's researcher requirement.

### 5.5 Fatigue: rolling energy pool (main anti-bot and anti-no-life rule)

```text
EnergyMax   = 60 + 8 × Endurance            (68 at Endurance 1, 140 at Endurance 10)
EnergyRegen = EnergyMax / 20 per hour       (empty to full in 20 hours, continuous)
Each cycle costs 1 energy.
```

| State | Condition | Experience | Yield |
|---|---|---|---|
| Fresh | Energy > 0 | × 1.0 | × 1.0 |
| Tired | Energy = 0 | × 0 | × 0.25 |

- Regeneration is **continuous**, not a daily reset, so there is no "reset at midnight" exploit and no timezone advantage.
- Steady state: Endurance 1 ≈ 82 full cycles per day (~2 hours), Endurance 10 ≈ 168 per day (~4.2 hours).
- Tired Citizens can still work and contribute a little (it stays fun during a long stream), but they stop gaining power.
- **Result:** a bot running 24/7 progresses at the same speed as a dedicated human. There is nothing to gain from automation.

### 5.6 Overflow: work must be useful

> Being replaced by storage buildings and carried goods: see [streamer-mode.md](streamer-mode.md), section 3.

Each village resource has a storage cap (raised by building warehouses).

```text
Overflow = 0.5   if the task's output resource is at storage cap
Overflow = 1.0   otherwise
```

When storage is full, yield is wasted and experience is halved. This pushes Citizens toward whatever the village actually needs instead of mindless grinding, and stops resource inflation.

### 5.7 Skills: getting better at a craft

Besides stats, every task, construction job and research trains one **skill** (a craft). Baking bread at the Bakery trains Baking; chopping wood and felling trees on a site train Woodcutting.

| Skill | Trained by |
|---|---|
| Woodcutting | Gather wood, Fell the trees |
| Quarrying | Gather stones |
| Gathering | Forage berries, Cut reeds, Study plants |
| Hunting | Hunt rabbits, deer and birds |
| Fishing | Fish from the pier |
| Farming | Tend, sow, water and harvest the fields, Plough the field |
| Mining | Dig for gold, Mine coal, iron ore and gold, Dig the shaft |
| Smithing | Smelt iron, Forge chains |
| Carpentry | Saw planks, Build mechanisms |
| Baking | Bake bread |
| Scholarship | Measure the river, Read the chronicles, Research, Brew remedies, researching a technology |
| Building | Build, Survey, Supervise, Draft plans, Lead a crew, construction labor |
| Hauling | Carry to storage, Deliver goods, Bring materials to a site |
| Entertaining | Tend the fire, Chat at the tavern, Tell stories, Play the gusli |
| Exploring | Expeditions, Roam the land, Explore the land |
| Trading | Trade, Negotiate (not available yet) |
| Guarding | Keep watch, Patrol, Command, and every fight with a beast or raider |
| Weaponcraft | Make spears, Sew leather armor, Make bows, Fletch arrows |

- A skill gets the same experience as the task's primary stat, on the same curve (section 5.2), up to level 10. Skills do **not** count toward the talent cap; experience past level 10 is lost (no Renown).
- **Stat gate:** each skill level needs its stats (section 3.1). A level the stats do not allow yet waits with at most one level of experience banked, and comes once the stats grow.
- **Task gate:** every task needs its skill level (section 3.2): journeyman work 3, master work 7, a few steps in between (deer 2, bows 2, better seeds 2–5).
- **Bonus:** every skill level above what the task needs gives +5% yield and −4% work time (walking is not shortened), for up to 5 levels: +25% yield and −20% work time at most. Experience per cycle stays the same, so a skilled citizen also levels faster per hour.

## 6. Yield math

### 6.1 Yield per cycle

```text
EffectiveStat = 0.7 × Primary + 0.3 × Secondary
StatFactor    = 0.4 + 0.12 × EffectiveStat      (0.52 at 1, 1.0 at 5, 1.6 at 10)
Yield         = BaseYield × StatFactor × Fatigue × Morale × Streak × ToolBonus
```

- Spread between weakest and strongest Citizen is about **3×**. Strong Citizens matter, but newcomers are never useless. This is the soft gate for basic labor.
- Fractional yield accumulates per Citizen (0.6 + 0.6 → 1 delivered, 0.2 carried over).

### 6.2 Streak

```text
Streak = 1 + 0.01 × ConsecutiveRenewals, capped at 1.20
```

Renewing within the grace window keeps the streak; missing it resets the streak to 0. **The streak affects yield only, never experience**, so it rewards attention without speeding up power progression.

### 6.3 Morale (village-wide)

```text
Morale ∈ [0.8, 1.2], starts at 1.0
```

Raised by Bards, food surplus and finished projects. Lowered by food shortage and lost raids. Slowly drifts back to 1.0.

### 6.4 Tools

Smiths forge tools that are placed in the village tool shed (shared, not personal): +10% yield for the matching task family per tool tier, up to +30%. This gives crafters a reason to exist and makes the economy circular.

### 6.5 Discovery rolls (Scout, Deep Miner)

```text
RareChance = 1% + 0.5% × Dexterity            per cycle
```

Rolled on the server only. The rare find is announced in the village.

## 7. Projects (co-operative goals)

Projects are what make the village a community instead of 50 solo clickers.

### 7.1 Structure

Each project has phases. Each phase has requirements:

- **Resources:** delivered from village storage.
- **Labor:** total cycles of a given task.
- **Role presence:** some phases only progress while a qualified Citizen is actively working on it.

### 7.2 Example: Bridge

| Phase | Requirements |
|---|---|
| 1. Survey | 30 cycles of *Survey* (Engineer) |
| 2. Materials | 400 wood, 200 stone, 120 planks, 40 iron ingots, 10 chains |
| 3. Construction | 300 cycles of *Build*/*Haul*, **only progress while at least one Engineer is on *Supervise*** |

Supervision rule:

```text
LaborersSupervised = 3 + Intelligence of the Engineer   (per Engineer)
```

A single Intelligence 4 Engineer can supervise 7 laborers; extra laborers' cycles do not count. This turns "you can't build a bridge if you're not smart" into a concrete need: smart Citizens are the bottleneck.

### 7.3 Foreman bonus

```text
CrewBonus = 1 + 0.02 × Charisma of the Foreman   (only the best Foreman counts)
```

Applied to the yield of everyone working on the same project.

### 7.4 Contribution cap

- No single Citizen may provide more than **20%** of any single requirement of a phase (resources or labor).
- This stops one dedicated player (or bot) from soloing projects and keeps the "we need more people" pressure.
- Project completion rewards (Renown, title, name on the bridge) are split by contribution share.

### 7.5 Weather and the Synoptic

- A **day** lasts 4 real hours (configurable with `Game:DayLength`): the first half is daytime, the second half night. Days count from the Unix epoch, so every server agrees on them. The map darkens at night, with a short twilight around dusk and dawn; development and local deployments (`Game:DevTools`) show a toggle that previews daylight or night on the client only.
- Every daytime and every night draws its own sky as it begins: **Clear**, **Rain**, **Wind** or **Thunderstorm**. Rain comes with a chance rolled between **5% and 10%** per period. Wind and thunderstorms are prepared but have a chance of 0 until they get effects; a thunderstorm will count as rain. The draw is derived from the village id and the period index, so every read agrees without a worker.
- **Synoptic** is a Journeyman profession (Weatherlore 3) unlocked by the tech **Weather lore** (Village era, Granary, after Agriculture). Its skill is **Weatherlore**, trained by the laborer task "Read the sky".
- A rested Synoptic (idle past the grace window after sleeping) may forecast the **next** daytime or night: rain or a clear sky. The first forecast of a period wins. The forecast comes out the other way round with a probability of 60% at Weatherlore 0, rising to 95% at the cap; otherwise the sky does what it would have done anyway.
- Forecasts are kept for 7 days, so past weather never changes.
- Rain effects: beds count as watered (the Water task is refused); fishing ×1.25; research and tavern work ×1.15; felled forest regrows twice as fast; outdoor tasks cost ×1.5 energy; sleeping under the trees regenerates at half speed; "Tend the fire" gives half morale; building, hauling and surveying ×0.75 (mud); hunting and roaming ×0.8.
- The weather of a cycle is the weather at the moment the cycle started.

## 8. Anti-abuse summary

| Abuse | Countermeasure |
|---|---|
| Fast clicking / scripted requests | Server-authoritative 90 s cycle, max 40 cycles per hour, results computed from server timestamps |
| Bots renewing 24/7 | Rolling energy pool: experience stops when energy is empty, so bots do not outpace humans |
| Farming the easiest task | Same base experience for all tasks + Challenge multiplier |
| Grinding useless resources | Storage caps + Overflow multiplier |
| One player carrying everything | Talent cap (30 total stat levels), project contribution cap (20%) |
| Rushing levels | Energy pool and challenge multiplier; the curve itself is gentle on purpose (stat 10 in about 17 active hours) so technologies stay reachable in a few hours |
| Daily reset exploits / timezones | Continuous energy regeneration instead of daily reset |
| Multiple accounts | One Citizen per Account per Village; Accounts are tied to a verified identity (platform login or streamer invite) |
| Suspicious automation patterns | Server heuristic: very low variance in renewal delay over hours → flag for the streamer, optional confirmation challenge |
| Client-side cheating | Client only sends "start task X" and "renew"; all randomness, yields and experience are calculated on the server |

## 9. Subscription lifecycle

| Event | Effect |
|---|---|
| New subscriber | Citizen creation unlocked; arrival shown in the village |
| Subscription lapses | Citizen becomes **Dormant**: no tasks, stats and items frozen (no decay) |
| Resubscribe | Citizen wakes up, keeps everything |
| Tier upgrades | Cosmetics, house upgrades, extra title slots. **Never stats, experience or energy.** |

## 10. Tuning parameters

| Parameter | Value |
|---|---|
| Stat range / start | 0–10 / 0 |
| Talent cap (sum of stats) | 30 |
| Skill level L needs | primary stat L − 1, secondary ⌊(L − 1) / 2⌋ |
| Journeyman / master craft level | 3 / 7 |
| Cycle length / grace window | 90 s / 60 s |
| Base experience per cycle | 10 |
| Secondary experience share | 0.3 |
| Experience curve | 50 for the first level, then 60 × L^1.5 |
| Skill maximum level | 10 (same curve as stats, no talent cap) |
| Skill bonus per level above the task's need | +5% yield, −4% work time, up to 5 levels |
| Challenge threshold / penalty | task skill level + 3 / × 0.25 |
| Energy max | 60 + 8 × Endurance |
| Energy full regeneration time | 20 hours |
| Vigor drink (tavern, "Vigor drinks" tech) | +20 energy, once per 2 hours; brewed from 2 grain + 1 food |
| Tired experience / yield | × 0 / × 0.25 |
| Overflow experience | × 0.5 |
| Stat factor | 0.4 + 0.12 × EffectiveStat |
| Streak | +1% per renewal, max +20% |
| Morale range | 0.8–1.2 |
| Tool bonus | +10% per tier, max +30% |
| Rare find chance | 1% + 0.5% × Dexterity |
| Engineer supervision | 3 + Intelligence laborers |
| Foreman crew bonus | +2% per Charisma |
| Project contribution cap | 20% per requirement |

## 11. Open questions

- **Stat balance:** decided to keep all five stats. Endurance (energy) and Charisma (two crafts, both with work not available yet) are weaker than the others; to be revisited in the hardening pass (less experience and energy, higher build requirements).
- **Respec:** should Citizens be able to lower one stat to raise another (for example, a costly "retraining" project)? Without it, the talent cap is permanent.
- **Seasons:** do villages reset periodically (fresh competition, new streamers can catch up) or persist forever?
- **Failure/risk:** decided for the wild: hunts and expeditions can be ambushed and wound the citizen (section 12). Should deep mining get a cave-in chance too?

## 12. Fights, enemies and weapons

### 12.1 Enemies

| Enemy | Power | Ambush weight | Slain leaves | One raider steals | Building damage | Wound |
|---|---|---|---|---|---|---|
| Wolf | 3 | 5 | 1 leather | 3 food | 0 | 30 min |
| Boar | 5 | 3 | 6 food, 1 leather | 4 food, 2 grain | 10 | 40 min |
| Bear | 12 | 1 | 15 food, 3 leather | 12 food | 25 | 60 min |
| Zombie (undead) | 4 | — | — | — | 15 | 30 min |
| Skeleton (undead) | 6 | — | 1 iron ore | 1 gold | 20 | 40 min |

### 12.2 Fight power and the armory

- **Fight power** = 2 (bare hands) + 0.6 × Strength + 0.4 × Dexterity + 0.3 × Guarding skill + the weapon's power.
- Weapons and armor are goods in the shared storage (the Storehouse takes them). A citizen starting a dangerous task (hunt, roam, expedition, exploration) or a guard task takes the best weapon and armor available — from their own hands first, then storage — and puts them back when the task ends. If storage has no room, the gear stays in their hands.

| Gear | Effect | Breaks per fight | Made by | Needs |
|---|---|---|---|---|
| Spear | +3 power | 10% | Make spears: 2 wood + 1 stone | Weaponcraft |
| Bow | +4 power, shoots 2 arrows per fight; useless without arrows | 5% | Make bows: 3 wood + 1 leather | Archery |
| Arrows | 6 per quiver | used up | Fletch arrows: 1 wood + 2 feathers → 6 | Archery |
| Sword | +6 power | 3% | Forge swords (Smith): 2 iron + 1 coal + 1 leather | Warfare |
| Leather armor | stops 30% of wounds | 8% | Sew leather armor: 3 leather | Weaponcraft |
| Shield | stops 40% of wounds | 5% | Make shields (Carpenter): 2 planks + 1 leather | Warfare |

Discoveries: **Weaponcraft** (Camp era, at the stone pile, after Stone tools), **Archery** (Camp era, at the drying rack, after Tracking and Weaponcraft), **Warfare** (Town era, at the smithy, after Smelting and Weaponcraft; opens the Guard and Captain roles and the Barracks).

### 12.3 Ambushes in the wild

- Every dangerous task has a chance per cycle that a beast attacks: hunt rabbits 4% (wolf), hunt deer 8% (wolf, boar or bear), hunt birds 3%, roam and explore the land 4%, expedition and long expedition 6%. At night the chance is ×1.5. The beast is picked by its ambush weight.
- Win chance = power / (power + beast power). A win gives the carcass loot and Guarding experience, with a 10% chance of a wound anyway. A loss wounds the citizen and the cycle yields nothing (overflow experience).
- New villages are left in peace for the first day (`Game:PeacefulTime`, 0 in development).

### 12.4 Night raids and the undead

- Each night there is a 35% chance beasts attack the camp, between 15% and 85% of the night. The tribe sees them coming 15 minutes before they strike. Wolves only in the Camp era; boars join in the Village era; bears in the Town era. The pack grows with the number of active citizens.
- The Tsar can call an **undead event** for the stream: zombies or skeletons in 1, 2 or 3 waves (small, medium, large), 3 minutes after the call and 2 minutes apart. The Tsar can lay them to rest before they strike.
- **Defense** = (Σ power × task factor of everyone on a guard task at the strike + 3 for the campfire + 5 with the palisade) × the best captain's command (1 + 0.04 × Endurance) × 1.25 with the palisade × 1.1 with the Barracks. Task factor: Keep watch 1, Patrol 1.25, Command 1. Luck varies the defense by ±25%.
- **Repelled** (defense ≥ attack): defenders slay most raiders, share 2 renown per kill, and the slain leave loot; each defender has a 15% chance of a wound. Morale +3%.
- **Lost:** a few raiders die, the rest steal goods, damage random buildings and wound citizens resting at the camp (50% each); each defender has a 60% wound chance. Morale −8%.
- Gear wears out in raids as in ambushes.

### 12.5 Wounds, healing and repairs

- A wounded citizen cannot start any work until the wound heals. A **Remedy** (Brew remedies, or Make poultices from 2 food with Herb lore) heals it at once; the citizen's hands first, then storage.
- A damaged building (0 to 100) works slower: yield × (1 − damage / 200). **Repair buildings** mends the most damaged one.
