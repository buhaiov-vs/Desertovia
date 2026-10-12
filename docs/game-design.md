# Desertod — Game Design: Stats, Roles, Tasks, Progression

Status: draft v0.1. All numbers are tuning parameters, collected in [Tuning parameters](#10-tuning-parameters).

## 1. Concept recap

- A **streamer** hosts a **Village**. Any streamer on the platform can host one.
- **Citizens** are the streamer's subscribers. Each person has one **Account** and one **Citizen** per Village; stats are per Citizen.
- Citizens work in a browser page (clicker style): pick a task, it runs for **90 seconds**, then must be renewed or changed.
- The village is rendered as a 2D overlay on stream.

## 2. Stats

Five stats, range **1–10**.

| Stat | Meaning | Main uses |
|---|---|---|
| Strength | Raw physical power | Mining, smithing, hauling, guarding |
| Dexterity | Precision, speed | Crafting, hunting, scouting, fine smithing |
| Intelligence | Knowledge, planning | Engineering, research, alchemy, supervising projects |
| Charisma | Social influence | Trading, leading groups, morale, diplomacy |
| Endurance | Stamina | Size of the energy pool (how long a Citizen works at full efficiency), farming (better seeds) |

### 2.1 Character creation

- Every stat starts at **1**.
- The player distributes **8 points**.
- No stat may exceed **4** at creation.
- Total stat levels at creation: 5 + 8 = **13**.

This allows starting directly in a journeyman role (requirement 4) in one focus, Fallout Shelter style, while master roles must be earned.

### 2.2 Talent cap

- The sum of all five stats cannot exceed **30** (out of a theoretical 50).
- A Citizen can be great at two things, decent at one, or average at everything, but never great at everything.
- This makes Citizens depend on each other, which is the whole point of a community game.
- When a stat's experience would push the total over the cap, that experience is converted into **Renown** (prestige currency for cosmetics, titles, house decorations). Effort is never wasted, but power does not grow.

## 3. Roles

Roles are **not** a class choice. A role is a set of tasks, unlocked automatically when stat requirements are met. A Citizen may perform any task of any role they qualify for and can switch every cycle. The highest role held is shown as the Citizen's title.

Three tiers, aligned with the progression math:

| Tier | Primary stat requirement | Typical time to reach (regular player) |
|---|---|---|
| Basic | 1 (no gate) | immediately |
| Journeyman | 4 | at creation, or ~1 week |
| Master | 7 | ~5–6 weeks |

### 3.1 Basic (everyone)

| Role | Tasks |
|---|---|
| Laborer | Chop wood, quarry stone, forage, tend fields, read old books, chat at the tavern, haul (generic project labor) |

### 3.2 Journeyman roles (primary stat 4)

| Role | Requirements | Tasks |
|---|---|---|
| Miner | Strength 4 | Mine coal, mine iron ore |
| Smith | Strength 4, Dexterity 3 | Smelt iron ingots, forge tools |
| Carpenter | Dexterity 4, Strength 2 | Saw planks, make beams, build carts |
| Hunter | Dexterity 4 | Title only: hunting is open to everyone, each prey needs a Dexterity level (see [cooperative-building.md](cooperative-building.md), Wildlife) |
| Baker | Dexterity 4, Endurance 2, Intelligence 2 | Bake bread (grain into food) at the Bakery; needs the Baking discovery |
| Farmer | Endurance 4 | Title only: farming is open to everyone, each seed needs an Endurance level (see [cooperative-building.md](cooperative-building.md), Farming) |
| Scholar | Intelligence 4 | Research (research points) |
| Engineer | Intelligence 4, Strength 2 | Survey project, supervise construction |
| Merchant | Charisma 4 | Trade with caravans (convert surplus to needed resources) |
| Bard | Charisma 4, Dexterity 2 | Perform (village morale) |
| Guard | Endurance 4, Strength 3 | Patrol (raid defense during streamer events) |

### 3.3 Master roles (primary stat 7)

| Role | Requirements | Tasks |
|---|---|---|
| Deep Miner | Strength 7, Endurance 4 | Mine gold, mine gems, open new shaft |
| Master Smith | Strength 7, Dexterity 5 | Forge chains, forge advanced tools |
| Artisan | Dexterity 7, Intelligence 4 | Build mechanisms (gates, cranes, mill parts) |
| Scout | Dexterity 7, Endurance 4 | Explore (discover resource nodes and events) |
| Architect | Intelligence 7, Charisma 3 | Design major buildings (town hall, monuments) |
| Alchemist | Intelligence 7, Dexterity 4 | Brew remedies (cure injuries), dyes |
| Foreman | Charisma 7, Intelligence 3 | Lead crew (group bonus on a project) |
| Envoy | Charisma 7, Intelligence 4 | Diplomacy with other streamers' villages |
| Captain | Endurance 7, Strength 5 | Command guards (raid defense multiplier) |

Every stat has a task at tier requirement 1, 4 and 7. That matters for the challenge rule in section 5.4.

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
| Role | Miner |
| Tier requirement | 4 |
| Primary stat | Strength |
| Secondary stat | Endurance |
| Base yield per cycle | 2 coal |
| Inputs consumed | none |
| Energy cost | 1 |

### 4.3 Task catalog (first version)

| Task | Role | Tier req. | Primary / Secondary | Base yield | Inputs |
|---|---|---|---|---|---|
| Chop wood | Laborer | 1 | Strength / Endurance | 3 wood | — |
| Quarry stone | Laborer | 1 | Strength / Endurance | 2 stone | — |
| Forage | Laborer | 1 | Dexterity / Intelligence | 2 food | — |
| Tend fields | Laborer | 1 | Endurance / Dexterity | 2 food | — |
| Read old books | Laborer | 1 | Intelligence / Charisma | 0.5 research point | — |
| Chat at the tavern | Laborer | 1 | Charisma / Intelligence | +morale (half of Perform) | — |
| Haul | Laborer | 1 | Endurance / Strength | +1 project progress | — |
| Mine coal | Miner | 4 | Strength / Endurance | 2 coal | — |
| Mine iron ore | Miner | 4 | Strength / Endurance | 1 iron ore | — |
| Smelt ingot | Smith | 4 | Strength / Dexterity | 1 iron ingot | 1 iron ore, 1 coal |
| Saw planks | Carpenter | 4 | Dexterity / Strength | 2 planks | 2 wood |
| Hunt (pick the prey) | Laborer | Dexterity 0 / 2 / 3 for rabbits / deer / birds | Dexterity / Endurance (birds: Intelligence) | see [cooperative-building.md](cooperative-building.md), Wildlife | — |
| Research | Scholar | 4 | Intelligence / Charisma | 1 research point | — |
| Survey | Engineer | 4 | Intelligence / Dexterity | +1 survey progress | — |
| Supervise | Engineer | 4 | Intelligence / Charisma | enables construction (see 6.2) | — |
| Trade | Merchant | 4 | Charisma / Intelligence | exchange at better rate | resources |
| Perform | Bard | 4 | Charisma / Dexterity | +morale | — |
| Patrol | Guard | 4 | Endurance / Strength | +defense (events only) | — |
| Mine gold | Deep Miner | 7 | Strength / Endurance | 0.5 gold | — |
| Forge chain | Master Smith | 7 | Strength / Dexterity | 1 chain | 2 iron ingot, 1 coal |
| Build mechanism | Artisan | 7 | Dexterity / Intelligence | 1 mechanism | 2 planks, 1 iron ingot |
| Explore | Scout | 7 | Dexterity / Endurance | discovery roll | — |
| Design | Architect | 7 | Intelligence / Charisma | +1 blueprint progress | — |
| Brew remedy | Alchemist | 7 | Intelligence / Dexterity | 1 remedy | 2 food |
| Lead crew | Foreman | 7 | Charisma / Intelligence | group bonus (see 6.3) | — |
| Negotiate | Envoy | 7 | Charisma / Intelligence | +relation with another village | — |
| Command | Captain | 7 | Endurance / Strength | defense multiplier (events only) | — |

## 5. Progression math

### 5.1 Experience gain per cycle

```text
XP_primary   = 10 × Challenge × Fatigue × Overflow × LiveBonus
XP_secondary = 0.3 × XP_primary
```

- **Every task gives the same base experience (10).** Higher-tier tasks give better *yield*, not more experience. This removes the "find the single best experience task and spam it" exploit.
- `LiveBonus` = **1.25** while the streamer is live, otherwise 1.0. Rewards watching the stream.
- The other multipliers are anti-abuse rules (sections 5.4–5.6).

### 5.2 Experience curve

```text
XP_to_next(0) = 50
XP_to_next(L) = round(60 × L^1.5)         for L = 1..9
```

Experience is tracked per stat. At level 10 the stat is maxed and further experience becomes Renown.

**Pacing rule:** every technology (a role tier, a story project) should be reachable in a few hours of play, about two in four hours. Citizens start with every stat at 0.

### 5.3 Pacing (calculated, primary stat only, without live bonus)

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

A Journeyman role (stat 4) takes about 1.5 hours of its main-stat task; a Master role (stat 7) about 6.6 hours. The talent cap (30 stat points in total) still keeps a citizen from mastering everything, and the challenge rule (section 5.4) keeps them from farming trivial tasks. The energy-capped minimum times of the old, slower curve no longer apply and have not been recalculated.

### 5.4 Challenge: no farming trivial tasks

```text
Challenge = 1.0   if Stat ≤ TierRequirement + 3
Challenge = 0.25  otherwise
```

A Strength 6 Citizen chopping wood (requirement 1) gets a quarter of the experience. To keep growing they must move up to mining (requirement 4), and at 8+ to deep mining (requirement 7). Because every stat has tasks at requirements 1, 4 and 7, a full-experience task always exists.

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

Rolled on the server only. The rare find is announced on the stream overlay.

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
| New subscriber | Citizen creation unlocked; arrival shown on overlay |
| Subscription lapses | Citizen becomes **Dormant**: no tasks, stats and items frozen (no decay) |
| Resubscribe | Citizen wakes up, keeps everything |
| Tier upgrades | Cosmetics, house upgrades, extra title slots. **Never stats, experience or energy.** |

## 10. Tuning parameters

| Parameter | Value |
|---|---|
| Stat range | 1–10 |
| Creation points / max per stat at creation | 8 / 4 |
| Talent cap (sum of stats) | 30 |
| Cycle length / grace window | 90 s / 60 s |
| Base experience per cycle | 10 |
| Secondary experience share | 0.3 |
| Experience curve | 50 for the first level, then 60 × L^1.5 |
| Challenge threshold / penalty | requirement + 3 / × 0.25 |
| Energy max | 60 + 8 × Endurance |
| Energy full regeneration time | 20 hours |
| Tired experience / yield | × 0 / × 0.25 |
| Overflow experience | × 0.5 |
| Live bonus (experience) | × 1.25 |
| Stat factor | 0.4 + 0.12 × EffectiveStat |
| Streak | +1% per renewal, max +20% |
| Morale range | 0.8–1.2 |
| Tool bonus | +10% per tier, max +30% |
| Rare find chance | 1% + 0.5% × Dexterity |
| Engineer supervision | 3 + Intelligence laborers |
| Foreman crew bonus | +2% per Charisma |
| Project contribution cap | 20% per requirement |

## 11. Open questions

- **Endurance as a fifth stat** (original idea was four: Strength, Dexterity, Intelligence, Charisma). Without it, energy would be a flat number. Keep or drop?
- **Respec:** should Citizens be able to lower one stat to raise another (for example, a costly "retraining" project)? Without it, the talent cap is permanent.
- **Seasons:** do villages reset periodically (fresh competition, new streamers can catch up) or persist forever?
- **Failure/risk:** should tasks like deep mining have injury chance (needs Alchemist remedies)? It adds interdependence but also frustration.
