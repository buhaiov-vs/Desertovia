export type Stat = 'Strength' | 'Dexterity' | 'Intelligence' | 'Charisma' | 'Endurance'

export const STATS: Stat[] = ['Strength', 'Dexterity', 'Intelligence', 'Charisma', 'Endurance']

export type WorkState = 'Idle' | 'Working' | 'AwaitingRenewal'

export type RoleTier = 'Basic' | 'Journeyman' | 'Master'

export type TaskEffect =
  | 'Resources'
  | 'Morale'
  | 'Deliver'
  | 'Survey'
  | 'Build'
  | 'Supervise'
  | 'Expedition'
  | 'Roam'
  | 'Unload'
  | 'Sow'
  | 'Water'
  | 'Harvest'
  | 'NotAvailableYet'

export type ProjectPhase = 'Survey' | 'Construction' | 'Completed'

export interface StatRequirement {
  stat: Stat
  minimum: number
}

export interface Role {
  id: string
  name: string
  tier: RoleTier
  requirements: StatRequirement[]
  /** Technology the village must discover before anyone works in this role. */
  technology: string | null
}

export interface ResourceAmount {
  resource: string
  amount: number
}

export interface Task {
  id: string
  name: string
  roleId: string
  tierRequirement: number
  primary: Stat
  secondary: Stat
  effect: TaskEffect
  baseYield: number
  outputs: ResourceAmount[]
  inputs: ResourceAmount[]
  workplace: string
  /** The task can be done only once the village has this building. */
  requiredBuilding: BuildingKind | null
  /** Project the village must have finished first. */
  requiredProject: string | null
  /** Technology the village must have discovered first. */
  requiredTechnology: string | null
  workSeconds: number
  /** Set for hunting: the catch comes out of the village's herd of this animal. */
  hunt: { prey: AnimalKind; animals: number; dexterity: number } | null
  /** Set for sowing: the crop put in the bed; its Endurance level is the farming skill needed. */
  crop: CropKind | null
}

export type AnimalKind = 'Rabbit' | 'Deer' | 'Bird'

export type CropKind = 'Turnip' | 'Carrot' | 'Cabbage' | 'Flax' | 'Rye'

export interface CropDefinition {
  kind: CropKind
  name: string
  /** Endurance level needed to sow it. */
  endurance: number
  /** Time to ripen in a bed that is kept watered; a dry bed grows slower. */
  growHours: number
  /** Goods of a ripe bed at a stat factor of 1. */
  harvest: ResourceAmount[]
}

/** One bed of a farm. */
export interface FieldBed {
  /** Plot of the farm. */
  plotId: string
  index: number
  /** Null while the bed is empty. */
  crop: CropKind | null
  /** 0 when sown, 1 when ripe. */
  growth: number
  /** The bed is wet until then. */
  wateredUntil: string | null
  /** Growing and drying out soon: watering reaches it. */
  thirsty: boolean
}

/** Mirrors the server's tolerance for a ripe bed. */
const RIPE = 1 - 1e-6

export const isRipe = (bed: FieldBed) => bed.crop !== null && bed.growth >= RIPE

/** Why farm work cannot start now, mirroring the server's checks; null when it can. */
export function fieldsBlock(task: Task, fields: FieldBed[]): 'farm.noEmptyBed' | 'farm.nothingToWater' | 'farm.nothingRipe' | null {
  switch (task.effect) {
    case 'Sow':
      return fields.some((bed) => bed.crop === null) ? null : 'farm.noEmptyBed'
    case 'Water':
      return fields.some((bed) => bed.thirsty) ? null : 'farm.nothingToWater'
    case 'Harvest':
      return fields.some(isRipe) ? null : 'farm.nothingRipe'
    default:
      return null
  }
}

/** Game animals on the village land. The Governor sees them on the map, never the count. */
export interface Herd {
  kind: AnimalKind
  count: number
  capacity: number
  /** Workplace where they live and are hunted. */
  habitat: string
}

export interface Rules {
  cycleSeconds: number
  graceSeconds: number
  statMaximum: number
  talentCap: number
  secondsPerTile: number
  maximumOpenSites: number
  foodPerCycle: number
  /** Most of one good a citizen may hold after taking it out of storage. */
  takeLimit: number
}

export type BuildingKind = string

export interface JobDefinition {
  id: string
  name: string
  kind: 'Haul' | 'Labor'
  resource: string | null
  perCycle: number
  primary: Stat
  secondary: Stat
  needsMaterial: boolean
}

export interface StageDefinition {
  id: string
  name: string
  crew: number
  jobs: Array<{ jobId: string; amount: number }>
}

export interface BuildingDefinition {
  kind: BuildingKind
  name: string
  description: string
  requires: BuildingKind[]
  /** Empty for buildings of later ages. */
  stages: StageDefinition[]
  storage: StorageKind | null
  shelter: number
  shelterQuality: number
  /** Technology the village must discover before the building can be planned. */
  technology: string | null
}

export type Era = 'Wild' | 'Camp' | 'Village' | 'Town'

export const ERAS: Era[] = ['Wild', 'Camp', 'Village', 'Town']

/** A discovery the village researches together; the Governor puts a few to the vote and citizens choose the next one. */
export interface TechnologyDefinition {
  id: string
  name: string
  description: string
  era: Era
  /** Building where citizens research it. */
  researchedAt: BuildingKind
  /** Technologies to discover first. */
  requires: string[]
  /** Skills a citizen needs to take part in the research. */
  researcher: StatRequirement[]
  primary: Stat
  secondary: Stat
  /** Research work, in cycles of a researcher with every stat at 0. */
  cycles: number
  /** Different citizens who must research it. */
  crew: number
  bonuses: Array<{ taskId: string; multiplier: number }>
}

export interface TechnologyState {
  id: string
  discovered: boolean
  /** Not discovered and every technology it builds on is: the Governor can put it to the vote. */
  available: boolean
  done: number
  required: number
  hands: number
  crewNeeded: number
}

/** A vote on the next discovery. It closes once more than half of the active citizens voted. */
export interface TechnologyPoll {
  id: string
  options: Array<{ technologyId: string; votes: number }>
  ballots: Array<{ citizenId: string; technologyId: string }>
  /** Active citizens now, or when the poll closed. */
  electorate: number
  /** Votes that close the poll: more than half of `electorate`. */
  votesNeeded: number
  openedAt: string
  closedAt: string | null
  /** Null while open, or when the Governor cancelled the poll. */
  winner: string | null
}

export interface TilePoint {
  x: number
  y: number
}

/** A rectangle of whole tiles. */
export interface TileArea {
  x: number
  y: number
  width: number
  height: number
}

export interface ProjectDefinition {
  id: string
  name: string
  description: string
  surveyCycles: number
  materials: ResourceAmount[]
  buildCycles: number
}

export type StorageKind = string

export interface StorageDefinition {
  kind: StorageKind
  name: string
  /** Empty means every good. */
  accepts: string[]
  slots: number
  slotSize: number
}

export type StoryQuestKind =
  | 'Fire'
  | 'Shelter'
  | 'Stores'
  | 'FirstHouse'
  | 'Monument'
  | 'FishingSpot'
  | 'Palisade'
  | 'TsarBridge'
  | 'MonksGold'
  | 'MonasteryBell'
  | 'GreatGranary'

export type QuestKind = 'Hunger' | 'Cold' | 'Storage' | StoryQuestKind

const NEED_QUESTS: readonly QuestKind[] = ['Hunger', 'Cold', 'Storage']

/** Story quests are the village's chain of goals; the rest are needs that come and go. */
export const isStoryQuest = (kind: QuestKind): kind is StoryQuestKind => !NEED_QUESTS.includes(kind)

export interface QuestReward {
  /** Split between the helpers by their useful cycles. */
  renownPool: number
  morale: number
}

export interface Catalog {
  roles: Role[]
  tasks: Task[]
  projects: ProjectDefinition[]
  storages: StorageDefinition[]
  buildings: BuildingDefinition[]
  jobs: JobDefinition[]
  /** Top-left tile of plots known by name; other plot ids are the top-left tile written as "x,y". */
  plots: Record<string, TilePoint>
  /** Land where a whole building footprint must fit. */
  buildArea: TileArea
  /** Ground nobody builds on: the campfire and the project sites. */
  reservedAreas: TileArea[]
  places: Record<string, TilePoint>
  questRewards: Partial<Record<QuestKind, QuestReward>>
  rules: Rules
  technologies: TechnologyDefinition[]
  crops: CropDefinition[]
}

/** The Governor plans, citizens do the work. */
export type QuestAudience = 'Governor' | 'Citizens'

export interface QuestObjective {
  id: string
  audience: QuestAudience
  done: boolean
  /** Progress numbers; never shown to the Governor. */
  current: number | null
  required: number | null
}

/** A need of the tribe: opens when the village has a problem, closes when it is solved. */
export interface Quest {
  id: string
  kind: QuestKind
  /** The good a storage quest is about. */
  resource: string | null
  startedAt: string
  objectives: QuestObjective[]
}

export interface QuestCompletion {
  questId: string
  kind: QuestKind
  resource: string | null
  renownByCitizen: Record<string, number>
}

export interface SiteJob {
  jobId: string
  done: number
  required: number
  /** Work a labor job can take right now; labor cannot run ahead of the delivered material. */
  room: number
}

export interface SiteStage {
  id: string
  crewNeeded: number
  hands: number
  jobs: SiteJob[]
}

export interface Site {
  kind: BuildingKind
  plotId: string
  stage: number
  /** 0 to 1. */
  progress: number
  stages: SiteStage[]
}

export interface Structure {
  kind: BuildingKind
  plotId: string
}

export interface StorageSlot {
  kind: string
  /** 0 to 1. */
  fill: number
}

export interface Storage {
  kind: StorageKind
  plotId: string
  goods: ResourceAmount[]
  slots: StorageSlot[]
}

export interface ProjectMaterial {
  kind: string
  delivered: number
  required: number
}

export interface Project {
  id: string
  definitionId: string
  name: string
  description: string
  phase: ProjectPhase
  progress: number
  surveyDone: number
  surveyRequired: number
  buildDone: number
  buildLimit: number
  buildRequired: number
  materials: ProjectMaterial[]
  activeCitizens: number
  supervisionCapacity: number
  buildersWorking: number
  /** Great works need an Engineer to survey and supervise; small works do not. */
  needsEngineer: boolean
}

const PROJECT_EFFECTS: readonly TaskEffect[] = ['Deliver', 'Build', 'Survey', 'Supervise']

export const isProjectTask = (task: Task) => PROJECT_EFFECTS.includes(task.effect)

/** Whether the running project has any use for a project task: its materials, survey and supervision. */
export function projectWants(project: Project | null, task: Task) {
  if (!isProjectTask(task)) return true
  if (!project) return false
  switch (task.effect) {
    case 'Deliver':
      return task.outputs.some((output) => project.materials.some((material) => material.kind === output.resource))
    case 'Survey':
      return project.surveyRequired > 0
    case 'Supervise':
      return project.needsEngineer
    default:
      return true
  }
}

/** Workplace of digging the gold vein on the far bank; mirrors the server's map. */
export const GOLD_VEIN = 'gold-vein'

/** Workplaces whose goods can be gathered at a spot picked on the map; mirrors the server's rules. */
export const SPOT_WORKPLACES: readonly string[] = ['forest', 'stones', 'berries', 'reeds']

export const takesSpot = (task: Task) =>
  task.effect === 'Resources' && task.hunt === null && task.inputs.length === 0 && SPOT_WORKPLACES.includes(task.workplace)

/** Below this much gold the vein counts as empty; mirrors the server's tolerance. */
export const VEIN_EPSILON = 1e-6

/** Whether the task digs the gold vein and the vein has nothing left. */
export const veinEmpty = (village: VillageState, task: Task) =>
  task.workplace === GOLD_VEIN && village.goldVeinRemaining <= VEIN_EPSILON

export interface ProjectCompletion {
  projectId: string
  projectName: string
  renownByCitizen: Record<string, number>
}

export interface Resource {
  kind: string
  amount: number
  /** Null when unlimited (knowledge). */
  capacity: number | null
}

export interface VillageState {
  id: string
  slug: string
  name: string
  description: string | null
  /** Joining needs the invite code; watching stays open to everyone. */
  isPrivate: boolean
  isLive: boolean
  morale: number
  resources: Resource[]
  storages: Storage[]
  structures: Structure[]
  sites: Site[]
  project: Project | null
  completedProjects: string[]
  /** Open needs of the tribe, oldest first. */
  quests: Quest[]
  wildlife: Herd[]
  /** Standing trees on the camp land (`FOREST_AREA`); the wild beyond it is drawn from the village seed. */
  trees: Array<{ x: number; y: number }>
  /** The latest era the village discovered a technology of. */
  era: Era
  /** The technology the last vote chose; null until a vote chooses an undiscovered one. */
  currentResearch: string | null
  technologies: TechnologyState[]
  /** The open vote on the next discovery, or the last closed one. */
  poll: TechnologyPoll | null
  /** Gold still in the vein on the far bank; digging it stops at zero. */
  goldVeinRemaining: number
  /** Beds of every farm. */
  fields: FieldBed[]
}

export type Gender = 'Male' | 'Female'

/** Indexes into the lists in `game/appearance.ts`; the server checks them against the same counts. */
export interface Appearance {
  gender: Gender
  style: number
  skinTone: number
  hairColor: number
  outfitColor: number
}

export const CITIZEN_SUBTITLE_MAXIMUM = 24

export interface CitizenSummary {
  id: string
  name: string
  /** Written by the Governor, shown under the name on the map. */
  subtitle: string | null
  /** Null for citizens who joined before character creation. */
  appearance: Appearance | null
  titleRoleId: string
  taskId: string | null
  workplace: string | null
  /** Plot the citizen works on: a construction site, or the building where they research. */
  siteId: string | null
  /** Spot on the map the citizen gathers at, picked by the player. */
  spot: TilePoint | null
  /** Where the player sent the citizen to stand while not working. */
  idleSpot: TilePoint | null
  state: WorkState
  cycleStartedAt: string | null
  cycleEndsAt: string | null
  /** Goods the citizen holds because storage had no room. */
  carrying: string[]
}

export interface Village {
  state: VillageState
  isOwner: boolean
  /** Only sent to the owner of a private village. */
  inviteCode: string | null
  citizens: CitizenSummary[]
}

export interface VillageListItem {
  slug: string
  name: string
  isOwner: boolean
  isLive: boolean
}

export interface PublicVillage {
  slug: string
  name: string
  description: string | null
  isLive: boolean
  citizens: number
}

export interface CreateVillageRequest {
  name: string
  description: string | null
  isPrivate: boolean
}

export const VILLAGE_NAME_MINIMUM = 3
export const VILLAGE_NAME_MAXIMUM = 64
export const VILLAGE_DESCRIPTION_MAXIMUM = 280

/** Query parameter that carries the invite code of a private village. */
export const INVITE_PARAMETER = 'invite'

export function inviteLink(slug: string, inviteCode: string | null) {
  const link = `${window.location.origin}/v/${slug}`
  return inviteCode ? `${link}?${INVITE_PARAMETER}=${encodeURIComponent(inviteCode)}` : link
}

export interface StatValue {
  stat: Stat
  level: number
  experience: number
  experienceToNext: number
}

export interface Citizen {
  id: string
  name: string
  appearance: Appearance | null
  titleRoleId: string
  status: 'Active' | 'Dormant'
  stats: StatValue[]
  statTotal: number
  renown: number
  energy: number
  maximumEnergy: number
  streak: number
  taskId: string | null
  siteId: string | null
  siteStage: number | null
  /** Spot on the map the last task gathered at, picked by the player. */
  spot: TilePoint | null
  state: WorkState
  cycleStartedAt: string | null
  cycleEndsAt: string | null
  graceEndsAt: string | null
  cycleTired: boolean
  load: ResourceAmount[]
  /** Energy regeneration of where the citizen sleeps: 0.5 under the trees. */
  shelterQuality: number
  isHungry: boolean
  serverNow: string
  /** Technology the current cycle researches. */
  technologyId: string | null
}

export interface CycleReport {
  citizenId: string
  citizenName: string
  taskId: string
  produced: ResourceAmount[]
  /** Part of `produced` that did not fit in storage and stays in the citizen's hands. */
  carried: ResourceAmount[]
  moraleGained: number
  projectContribution: number
  rareFind: boolean
  primary: Stat
  primaryExperience: number
  secondary: Stat
  secondaryExperience: number
  levelsGained: number
  renownGained: number
  tired: boolean
  overflowed: boolean
  siteId: string | null
  buildingCompleted: BuildingKind | null
  hungry: boolean
  /** Technology the cycle researched. */
  technologyId: string | null
  /** This cycle finished the research of `technologyId`. */
  technologyDiscovered: boolean
}

export interface Me {
  id: string
  displayName: string
  providers: string[]
}

export interface LoginProviders {
  twitch: boolean
  google: boolean
  telegramBotUsername: string | null
  developmentLogin: boolean
}

/** `code` and `parameters` come from the server's problem details so the message can be translated. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | null
  readonly parameters: Record<string, string | number>

  constructor(status: number, message: string, code: string | null = null, parameters: Record<string, string | number> = {}) {
    super(message)
    this.status = status
    this.code = code
    this.parameters = parameters
  }
}

async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    credentials: 'same-origin',
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  if (!response.ok) {
    let message = response.statusText
    let code: string | null = null
    let parameters: Record<string, string | number> = {}
    try {
      const problem = await response.json()
      message = problem.detail ?? problem.title ?? message
      code = problem.code ?? null
      parameters = problem.arguments ?? {}
    } catch {
      // Body was not JSON.
    }
    throw new ApiError(response.status, message, code, parameters)
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

async function orNull<T>(promise: Promise<T>): Promise<T | null> {
  try {
    return await promise
  } catch (error) {
    if (error instanceof ApiError && (error.status === 401 || error.status === 404)) {
      return null
    }
    throw error
  }
}

export const api = {
  providers: () => request<LoginProviders>('GET', '/auth/providers'),
  me: () => orNull(request<Me>('GET', '/api/me')),
  developmentLogin: (name: string) => request<void>('POST', '/auth/dev-login', { name }),
  logout: () => request<void>('POST', '/auth/logout'),

  catalog: () => request<Catalog>('GET', '/api/catalog'),
  villages: () => request<VillageListItem[]>('GET', '/api/villages'),
  publicVillages: () => request<PublicVillage[]>('GET', '/api/villages/public'),
  createVillage: (village: CreateVillageRequest) => request<VillageState>('POST', '/api/villages', village),
  village: (slug: string) => request<Village>('GET', `/api/villages/${encodeURIComponent(slug)}`),
  setLive: (slug: string, isLive: boolean) =>
    request<VillageState>('POST', `/api/villages/${encodeURIComponent(slug)}/live`, { isLive }),
  newInviteCode: (slug: string) =>
    request<{ inviteCode: string }>('POST', `/api/villages/${encodeURIComponent(slug)}/invite`),

  myCitizen: (slug: string) => orNull(request<Citizen>('GET', `/api/villages/${encodeURIComponent(slug)}/me`)),
  createCitizen: (slug: string, name: string, appearance: Appearance, inviteCode: string | null) =>
    request<Citizen>('POST', `/api/villages/${encodeURIComponent(slug)}/citizens`, { name, appearance, inviteCode }),
  startTask: (slug: string, taskId: string, spot: TilePoint | null = null) =>
    request<Citizen>('POST', `/api/villages/${encodeURIComponent(slug)}/me/task`, { taskId, spot }),
  cancelTask: (slug: string) => request<Citizen>('DELETE', `/api/villages/${encodeURIComponent(slug)}/me/task`),
  startJob: (slug: string, plotId: string, stage: number, jobId: string) =>
    request<Citizen>('POST', `/api/villages/${encodeURIComponent(slug)}/me/job`, { plotId, stage, jobId }),
  dropLoad: (slug: string) => request<Citizen>('POST', `/api/villages/${encodeURIComponent(slug)}/me/drop`),
  takeFromStorage: (slug: string, plotId: string, resource: string) =>
    request<Citizen>('POST', `/api/villages/${encodeURIComponent(slug)}/me/take`, { plotId, resource }),
  goTo: (slug: string, spot: TilePoint) => request<Citizen>('POST', `/api/villages/${encodeURIComponent(slug)}/me/goto`, { spot }),
  research: (slug: string) => request<Citizen>('POST', `/api/villages/${encodeURIComponent(slug)}/me/research`),
  vote: (slug: string, technologyId: string) =>
    request<VillageState>('POST', `/api/villages/${encodeURIComponent(slug)}/me/vote`, { technologyId }),
  openPoll: (slug: string, technologyIds: string[]) =>
    request<VillageState>('POST', `/api/villages/${encodeURIComponent(slug)}/poll`, { technologyIds }),
  cancelPoll: (slug: string) => request<VillageState>('DELETE', `/api/villages/${encodeURIComponent(slug)}/poll`),
  placeBlueprint: (slug: string, kind: BuildingKind, x: number, y: number) =>
    request<VillageState>('POST', `/api/villages/${encodeURIComponent(slug)}/blueprints`, { kind, x, y }),
  cancelBlueprint: (slug: string, plotId: string) =>
    request<VillageState>('DELETE', `/api/villages/${encodeURIComponent(slug)}/blueprints/${encodeURIComponent(plotId)}`),
  setSubtitle: (slug: string, citizenId: string, subtitle: string | null) =>
    request<CitizenSummary>(
      'PUT',
      `/api/villages/${encodeURIComponent(slug)}/citizens/${encodeURIComponent(citizenId)}/subtitle`,
      { subtitle },
    ),
}

export const LEND_A_HAND = 'lend-a-hand'
export const CARRY_TO_STORAGE = 'carry-to-storage'
/** Task id of a research cycle; the technology is reported separately. */
export const RESEARCH_TASK = 'research-technology'

export function discoveredTechnologies(village: VillageState) {
  return new Set(village.technologies.filter((technology) => technology.discovered).map((technology) => technology.id))
}

/** Buildable buildings that store a good, with specialised ones first. */
export function storageBuildingsFor(catalog: Catalog, resource: string) {
  return catalog.buildings.filter((building) => {
    const storage = catalog.storages.find((s) => s.kind === building.storage)
    return building.stages.length > 0 && storage !== undefined && storage.accepts.includes(resource)
  })
}

export function builtKinds(village: VillageState, catalog: Catalog) {
  const kinds = new Set<BuildingKind>(village.structures.map((s) => s.kind))
  for (const storage of village.storages) {
    const building = catalog.buildings.find((b) => b.storage === storage.kind)
    if (building) kinds.add(building.kind)
  }
  return kinds
}

/** Top-left tile of a plot, or null for an id that is not a plot. Mirrors the server's map. */
export function plotPosition(plots: Record<string, TilePoint>, plotId: string): TilePoint | null {
  const named = plots[plotId]
  if (named) return named
  const match = /^(-?\d+),(-?\d+)$/.exec(plotId)
  return match ? { x: Number(match[1]), y: Number(match[2]) } : null
}

export function siteHasWork(site: Site) {
  return site.stages.some((stage) => stage.hands > 0 || stage.jobs.some((job) => job.done > 0))
}

/** Goods produced by a task that the citizen currently carries, which blocks the task. */
export function blockedByLoad(task: Task, citizen: Citizen) {
  const carried = new Set(citizen.load.map((good) => good.resource))
  if (task.effect === 'Expedition' || task.effect === 'Roam') return citizen.load.length > 0 ? citizen.load[0].resource : null
  if (task.effect === 'Unload') return null
  if (task.effect !== 'Resources') return null
  return task.outputs.find((output) => carried.has(output.resource))?.resource ?? null
}
