import { plotPosition, type AnimalKind, type Catalog, type TileArea, type TilePoint, type VillageState } from '../api'
import { EXTRAS, battle, dungeon, factory, farm, rogue, town, type TileRef } from './sheets'

/**
 * The camp area (tiles 0…43 × 0…25) matches the server's coordinates; the wild land extends around it
 * into negative coordinates, so the whole map spans MAP_LEFT…MAP_RIGHT × MAP_TOP…MAP_BOTTOM.
 */
export const MAP_LEFT = -44
export const MAP_TOP = -32
export const MAP_WIDTH = 120
export const MAP_HEIGHT = 90
export const MAP_RIGHT = MAP_LEFT + MAP_WIDTH
export const MAP_BOTTOM = MAP_TOP + MAP_HEIGHT

const CORE_WIDTH = 44
const CORE_HEIGHT = 26

export const RIVER_LEFT = 30
export const RIVER_RIGHT = 33
export const ROAD_TOP = 12

/** A building takes 3×3 tiles; the row below it is its yard. Mirrors the server's plot size. */
export const PLOT_WIDTH = 3
export const PLOT_HEIGHT = 4

export const CAMPFIRE = { x: 15.5, y: 13.5 }

/** Tiles a building and its yard cover when its top-left tile is (`x`, `y`). */
export const footprint = (x: number, y: number): TileArea => ({ x, y, width: PLOT_WIDTH, height: PLOT_HEIGHT })

const overlaps = (a: TileArea, b: TileArea) =>
  a.x < b.x + b.width && b.x < a.x + a.width && a.y < b.y + b.height && b.y < a.y + a.height

/** Footprints of every building and open site. */
export function buildingFootprints(village: VillageState): TileArea[] {
  return [...village.storages, ...village.structures, ...village.sites].flatMap((building) => {
    const plot = plotPosition(building.plotId)
    return plot ? [footprint(plot.x, plot.y)] : []
  })
}

/**
 * Same check as the server: inside the build area, off reserved ground, clear of other buildings.
 * Trees do not stop a blueprint: its builders fell them first.
 */
export function canBuildAt(catalog: Catalog, village: VillageState, x: number, y: number) {
  const area = footprint(x, y)
  const land = catalog.buildArea
  const inside = x >= land.x && y >= land.y && x + area.width <= land.x + land.width && y + area.height <= land.y + land.height
  return (
    inside &&
    !catalog.reservedAreas.some((reserved) => overlaps(reserved, area)) &&
    !buildingFootprints(village).some((taken) => overlaps(taken, area))
  )
}

/** True when a building or an open site covers the tile; its trees belong to the builders, not to wood gatherers. */
export const tileBuiltOver = (village: VillageState, x: number, y: number) =>
  buildingFootprints(village).some((taken) => overlaps(taken, { x, y, width: 1, height: 1 }))

/** Camp land up to the river whose trees come from the server. Mirrors the server's forest area. */
export const FOREST_AREA: TileArea = { x: 0, y: 0, width: 30, height: 26 }

export const inForestArea = (x: number, y: number) =>
  x >= FOREST_AREA.x && y >= FOREST_AREA.y && x < FOREST_AREA.x + FOREST_AREA.width && y < FOREST_AREA.y + FOREST_AREA.height

const TREE_TILES: Array<Omit<PlacedTile, 'x' | 'y'>> = [
  { tile: town(16), top: town(4) },
  { tile: town(28) },
  { tile: town(16), top: town(4) },
  { tile: town(28) },
  { tile: town(15), top: town(3) },
  { tile: town(27) },
  { tile: town(5) },
]

/** A tree from the server, whose look is picked from its tile so it stays the same between updates. */
export function treeAt(x: number, y: number): PlacedTile {
  let value = Math.imul(x, 0x27d4eb2d) ^ Math.imul(y, 0x165667b1)
  value = Math.imul(value ^ (value >>> 15), 0x85ebca6b)
  return { ...TREE_TILES[((value ^ (value >>> 13)) >>> 0) % TREE_TILES.length], x, y }
}

export interface PlacedTile {
  tile: TileRef
  x: number
  y: number
  /** Upper half of a two-tile sprite, drawn one tile above `tile`. */
  top?: TileRef
}

/** What covers a tile, for the minimap. */
export const Terrain = {
  Grass: 0,
  Tree: 1,
  Water: 2,
  Stone: 3,
  Bush: 4,
  Reeds: 5,
  Clearing: 6,
} as const
export type Terrain = (typeof Terrain)[keyof typeof Terrain]

export interface MapLayout {
  /** Rows from MAP_TOP, columns from MAP_LEFT. */
  ground: TileRef[][]
  decor: PlacedTile[]
  bushes: Array<{ x: number; y: number }>
  reeds: Array<{ x: number; y: number }>
  ponds: Array<{ x: number; y: number; radiusX: number; radiusY: number }>
  /** One entry per tile, row by row from the top-left corner of the map. */
  terrain: Uint8Array
}

/** Spots (in tiles) where citizens stand while working. Places match the server's map. */
export const WORKPLACES: Record<string, { x: number; y: number; spread: number }> = {
  forest: { x: 5, y: 8, spread: 2.5 },
  stones: { x: 10.5, y: 2.5, spread: 1.5 },
  berries: { x: 4.5, y: 18.5, spread: 2 },
  reeds: { x: 28.5, y: 18, spread: 1.2 },
  pier: { x: 30.6, y: 16.2, spread: 0.7 },
  ford: { x: 29, y: 14.5, spread: 0.6 },
  'gold-vein': { x: 40, y: 17.4, spread: 0.8 },
  hunting: { x: 9, y: 23, spread: 2.5 },
  'deer-woods': { x: -3, y: 21.5, spread: 2 },
  'bird-grove': { x: 21, y: 23.5, spread: 2 },
  camp: { x: CAMPFIRE.x, y: CAMPFIRE.y + 1, spread: 2.5 },
  gate: { x: 1.2, y: 13.4, spread: 0.6 },
  project: { x: 28, y: 11.2, spread: 1.4 },
}

/** Workplace of tasks without a fixed spot, like roaming the land. Mirrors the server's map. */
export const WILDS = 'wilds'

/** Workplace of farm work: the beds of the village farms. Mirrors the server's catalog. */
export const FIELDS = 'fields'

/** Where roaming citizens drift between: the natural places around the camp. */
export const ROAMING_SPOTS = ['forest', 'stones', 'berries', 'reeds', 'hunting'].map((place) => WORKPLACES[place])

export const PRIEST_SPOT = { x: 13.6, y: 12.6 }

/** One tile, or several drawn over each other from the first: a gable's eave over the wall that shows through it. */
export type BuildingCell = TileRef | TileRef[]

/**
 * Finished buildings drawn from tiles, each with its own roof, walls and props so the Governor can tell them apart.
 * Shelters and racks of the wild age and the open stage are drawn as shapes instead.
 */
export const BUILDING_TILES: Record<string, BuildingCell[][]> = {
  /** Orange gable, plaster walls. */
  Hut: [
    [rogue(1217), rogue(1222), rogue(1218)],
    [[rogue(873), rogue(1331)], [rogue(873), rogue(1276)], [rogue(873), rogue(1332)]],
    [rogue(869), [rogue(868), rogue(89)], rogue(871)],
  ],
  /** Cream gable with a round window, plaster walls, banners by an arched door. */
  TownHall: [
    [rogue(1210), rogue(1215), rogue(1211)],
    [[rogue(873), rogue(1324)], [rogue(873), rogue(1269), rogue(101)], [rogue(873), rogue(1325)]],
    [[rogue(869), rogue(222)], [rogue(868), rogue(32)], [rogue(871), rogue(222)]],
  ],
  /** Open shed under a long brown roof. */
  Woodyard: [
    [rogue(1231), rogue(1236), rogue(1232)],
    [[rogue(1235), rogue(1288)], [rogue(1236), rogue(1293)], [rogue(1237), rogue(1289)]],
    [[rogue(150), rogue(1117)], rogue(150), [rogue(150), rogue(1117)]],
  ],
  /** Battlemented grey stone wall with a dark gateway. */
  StoneYard: [
    [rogue(819), rogue(820), rogue(821)],
    [rogue(879), rogue(880), rogue(881)],
    [rogue(876), [rogue(875), rogue(150), rogue(994)], rogue(878)],
  ],
  /** Brown gable, timber walls, sacks of grain by the door. */
  Granary: [
    [rogue(1231), rogue(1236), rogue(1232)],
    [[rogue(894), rogue(1345)], [rogue(894), rogue(1290)], [rogue(894), rogue(1346)]],
    [[rogue(890), rogue(139)], [rogue(889), rogue(90)], [rogue(892), rogue(140)]],
  ],
  /** Cream gable, timber walls, a double door and a crate of goods. */
  Storehouse: [
    [rogue(1210), rogue(1215), rogue(1211)],
    [[rogue(894), rogue(1324)], [rogue(894), rogue(1269)], [rogue(894), rogue(1325)]],
    [[rogue(890), rogue(315)], [rogue(889), rogue(316)], [rogue(892), rogue(413)]],
  ],
  /** Long grey roof over blue stone, a barred door between heaps of gold. */
  Treasury: [
    [rogue(1224), rogue(1229), rogue(1225)],
    [[rogue(1228), rogue(1281)], [rogue(1229), rogue(1286)], [rogue(1230), rogue(1282)]],
    [[rogue(883), rogue(613)], [rogue(882), rogue(204)], [rogue(885), rogue(672)]],
  ],
  /** Three dry beds; wet soil and crops are drawn over them from the village's fields. */
  Farm: [
    [farm(60), farm(61), farm(63)],
    [farm(60), farm(61), farm(63)],
    [farm(60), farm(61), farm(63)],
  ],
  /** A dark shaft in the rock, rails coming out of it, ore and a cart beside them. */
  Mine: [
    [rogue(177), rogue(177), rogue(177)],
    [rogue(177), [rogue(150), rogue(994)], rogue(177)],
    [rogue(671), [rogue(150), rogue(1238)], rogue(69)],
  ],
  /** Grey gable over brick, an anvil and a glowing forge. */
  Smithy: [
    [rogue(1224), rogue(1229), rogue(1225)],
    [[rogue(120), rogue(1338)], [rogue(120), rogue(1283)], [rogue(120), rogue(1339)]],
    [[rogue(120), rogue(15)], [rogue(120), rogue(89)], [rogue(120), rogue(511)]],
  ],
  /** Long cream roof over plank walls, a window and a crate of tools. */
  Workshop: [
    [rogue(1210), rogue(1215), rogue(1211)],
    [[rogue(1214), rogue(1267)], [rogue(1215), rogue(1272)], [rogue(1216), rogue(1268)]],
    [[rogue(1003), rogue(331)], [rogue(1004), rogue(90)], [rogue(1005), rogue(414)]],
  ],
  /** Grey gable with a rose window over blue stone, tall arched windows. */
  Library: [
    [rogue(1224), rogue(1229), rogue(1225)],
    [[rogue(887), rogue(1338)], [rogue(887), rogue(1283), rogue(102)], [rogue(887), rogue(1339)]],
    [[rogue(883), rogue(159)], [rogue(882), rogue(32)], [rogue(885), rogue(159)]],
  ],
  /** Long orange roof over timber walls, a lit window and a barrel by the door. */
  Tavern: [
    [rogue(1217), rogue(1222), rogue(1218)],
    [[rogue(1221), rogue(1274)], [rogue(1222), rogue(1279)], [rogue(1223), rogue(1275)]],
    [[rogue(890), rogue(331)], [rogue(889), rogue(90)], [rogue(892), rogue(23)]],
  ],
  /** Orange gable over grey stone, a sack of flour and the oven. */
  Bakery: [
    [rogue(1217), rogue(1222), rogue(1218)],
    [[rogue(880), rogue(1331)], [rogue(880), rogue(1276)], [rogue(880), rogue(1332)]],
    [[rogue(876), rogue(141)], [rogue(875), rogue(89)], [rogue(878), rogue(453)]],
  ],
  /** A striped stall with goods on the counter, crates and sacks in front. */
  Market: [
    [rogue(68), rogue(11), rogue(125)],
    [rogue(296), rogue(240), rogue(353)],
    [rogue(413), rogue(141), rogue(24)],
  ],
  /** Battlemented plaster wall, war banners and an iron gate. */
  Barracks: [
    [rogue(812), rogue(813), rogue(814)],
    [[rogue(872), rogue(50)], rogue(873), [rogue(874), rogue(50)]],
    [rogue(869), [rogue(868), rogue(203)], rogue(871)],
  ],
}

/** A wet farm bed: darker soil over the dry one. */
export const WET_BED: TileRef[] = [farm(48), farm(49), farm(51)]

/** Growth stages of each crop in a farm bed: sprout, young plant, ripe plant. */
export const CROP_STAGES: Record<string, TileRef[]> = {
  Turnip: [farm(16), farm(17), farm(18)],
  Carrot: [farm(4), farm(5), farm(6)],
  Cabbage: [farm(52), farm(53), farm(54)],
  Flax: [farm(64), farm(65), farm(80)],
  Rye: [farm(64), farm(65), farm(66)],
}

/** What the crop looks like once gathered. */
export const CROP_ICONS: Record<string, TileRef> = {
  Turnip: farm(20),
  Carrot: farm(8),
  Cabbage: farm(56),
  Flax: farm(80),
  Rye: farm(68),
}

/** Below this growth a bed shows sprouts. */
const SPROUT_GROWTH = 0.4

/** Tile of a crop at its growth: sprout, young plant, or the ripe plant. */
export const cropTile = (crop: string, growth: number, ripe: boolean) =>
  CROP_STAGES[crop]?.[ripe ? 2 : growth < SPROUT_GROWTH ? 0 : 1]

/** Icon of each building in the interface. */
export const BUILDING_SIGNS: Record<string, TileRef> = {
  Heap: town(92),
  LeanTo: farm(2),
  WoodPile: town(106),
  StonePile: farm(89),
  DryingRack: town(29),
  Hut: town(83),
  TownHall: battle(52),
  Woodyard: town(127),
  StoneYard: town(115),
  Granary: farm(71),
  Storehouse: dungeon(89),
  Treasury: town(93),
  Farm: farm(68),
  Mine: factory(98),
  Smithy: dungeon(74),
  Workshop: factory(123),
  Library: dungeon(63),
  Tavern: dungeon(82),
  Bakery: farm(125),
  Stage: battle(70),
  Market: farm(74),
  Barracks: dungeon(104),
}

/** The old monastery on the far bank: the Tsar Bridge is meant to reach it. Scenery, not part of the village. */
export const MONASTERY = {
  church: {
    x: 37,
    y: 7,
    tiles: [
      [town(48), town(49), town(50)],
      [town(60), town(61), town(62)],
      [town(76), town(77), town(79)],
      [town(88), town(90), town(88)],
    ] as TileRef[][],
  },
  cells: [
    { x: 34, y: 8 },
    { x: 40.5, y: 8 },
  ],
  cell: [
    [town(52), town(53), town(54)],
    [town(64), town(65), town(66)],
    [town(84), town(85), town(84)],
  ] as TileRef[][],
  area: { x: 34, y: 5, width: 10, height: 9 },
}

/** Where each catalog project is built when it is not the bridge. */
/** The Tsar's statue north of the campfire, between plots 1 and 2: a stone figure on a pedestal. */
export const MONUMENT = {
  id: 'tsar-monument',
  x: 16,
  y: 9,
  pedestal: dungeon(54),
  rough: dungeon(65),
  statue: dungeon(86),
  stone: 0xb9bec7,
}

/** The gold vein in the hills on the far bank, reached over the Tsar Bridge. Size mirrors the server's rules. */
export const VEIN_OUTCROP = { x: 39, y: 16, width: 3, height: 2, size: 150 }

/** Planks on poles from the west bank into the river, south of the road. */
export const PIER = { id: 'fishing-pier', x: RIVER_LEFT - 1, y: 16, length: 3 }

/**
 * Stakes around the camp land on the west, north and south; the river is the east wall. The west side is open at
 * the gate. Pieces are listed in building order: from the gate north and along the top, then south and along the bottom.
 */
export const PALISADE = (() => {
  const left = -1
  const top = -1
  const bottom = 26
  const end = 27
  const gate = { from: 12, to: 14 }
  const pieces: PlacedTile[] = []
  const side = (from: number, to: number, step: number, tile: (y: number) => TileRef) => {
    for (let y = from; step > 0 ? y <= to : y >= to; y += step) pieces.push({ tile: tile(y), x: left, y })
  }
  const rail = (y: number, corner: TileRef) => {
    pieces.push({ tile: corner, x: left, y })
    for (let x = left + 1; x <= end; x++) pieces.push({ tile: town(x === end ? 82 : 45), x, y })
  }
  side(gate.from - 1, top + 1, -1, (y) => town(y === gate.from - 1 ? 71 : 56))
  rail(top, town(44))
  side(gate.to + 1, bottom - 1, 1, (y) => town(y === gate.to + 1 ? 47 : 56))
  rail(bottom, town(68))
  return { id: 'palisade', pieces, yard: { x: 2.5, y: 10.5 } }
})()

/** Where builders stand and materials lie for projects drawn outside building plots. */
export const PROJECT_YARDS: Record<string, { x: number; y: number; spread: number }> = {
  [MONUMENT.id]: { x: MONUMENT.x + 0.5, y: MONUMENT.y + 3, spread: 1.5 },
  [PIER.id]: { x: PIER.x - 1, y: PIER.y + 1, spread: 1 },
  [PALISADE.id]: { x: PALISADE.yard.x, y: PALISADE.yard.y + 1, spread: 1.5 },
  'bell-tower': { x: 41.5, y: 5.5, spread: 1 },
  granary: { x: 26.5, y: 21, spread: 1.5 },
}

export const PROJECT_SITES: Record<string, { x: number; y: number }> = {
  granary: { x: 25, y: 22 },
  /** Behind the monastery on the far bank, standing free so the bell is seen from both banks. */
  'bell-tower': { x: 41, y: 1 },
}

/** Ground of each village project; while it is being built, a click there calls citizens to work on it. */
export const PROJECT_AREAS: Record<string, TileArea[]> = {
  'tsar-bridge': [
    { x: 26, y: 9, width: 4, height: 3 },
    { x: RIVER_LEFT, y: ROAD_TOP, width: RIVER_RIGHT - RIVER_LEFT + 1, height: 2 },
  ],
  [MONUMENT.id]: [{ x: MONUMENT.x - 1, y: MONUMENT.y - 1, width: 3, height: 4 }],
  [PIER.id]: [{ x: PIER.x - 2, y: PIER.y - 1, width: PIER.length + 2, height: 3 }],
  [PALISADE.id]: [
    { x: 0, y: 9, width: 4, height: 3 },
    ...PALISADE.pieces.map((piece) => ({ x: piece.x, y: piece.y, width: 1, height: 1 })),
  ],
  granary: [{ x: 25, y: 21, width: 4, height: 4 }],
  'bell-tower': [{ x: 40, y: 0, width: 5, height: 6 }],
}

/** Deck of the Tsar Bridge: the only way across the river once it is finished. */
export const BRIDGE_DECK: TileArea = { x: RIVER_LEFT, y: ROAD_TOP, width: RIVER_RIGHT - RIVER_LEFT + 1, height: 2 }

/** Why a walk cannot be made: the spot is in water, or the river lies between and no bridge crosses it. */
export type WalkBlock = 'water' | 'river'

/**
 * The way on foot between two points, in tiles: the turns to take, ending at `to`. Water stops walkers, except the
 * deck of a finished bridge. A walker already standing in water may still walk out of it.
 */
export function findWay(terrain: Uint8Array, from: TilePoint, to: TilePoint, bridge: boolean): TilePoint[] | WalkBlock {
  const startX = Math.floor(from.x)
  const startY = Math.floor(from.y)
  const goalX = Math.floor(to.x)
  const goalY = Math.floor(to.y)
  const inside = (x: number, y: number) => x >= MAP_LEFT && x < MAP_RIGHT && y >= MAP_TOP && y < MAP_BOTTOM
  const index = (x: number, y: number) => (y - MAP_TOP) * MAP_WIDTH + (x - MAP_LEFT)
  const onDeck = (x: number, y: number) =>
    x >= BRIDGE_DECK.x && x < BRIDGE_DECK.x + BRIDGE_DECK.width && y >= BRIDGE_DECK.y && y < BRIDGE_DECK.y + BRIDGE_DECK.height
  const walkable = (x: number, y: number) =>
    inside(x, y) && (terrain[index(x, y)] !== Terrain.Water || (bridge && onDeck(x, y)) || (x === startX && y === startY))

  if (!walkable(goalX, goalY)) return 'water'
  const clear = (a: TilePoint, b: TilePoint) => {
    const steps = Math.ceil(Math.hypot(b.x - a.x, b.y - a.y) / 0.1)
    for (let step = 1; step <= steps; step++) {
      const share = step / steps
      if (!walkable(Math.floor(a.x + (b.x - a.x) * share), Math.floor(a.y + (b.y - a.y) * share))) return false
    }
    return true
  }
  if (clear(from, to)) return [to]

  // A* over tiles, eight ways; a diagonal step may not cut the corner of water.
  const cost = new Float64Array(MAP_WIDTH * MAP_HEIGHT).fill(Infinity)
  const cameFrom = new Int32Array(MAP_WIDTH * MAP_HEIGHT).fill(-1)
  const closed = new Uint8Array(MAP_WIDTH * MAP_HEIGHT)
  const heuristic = (x: number, y: number) => {
    const dx = Math.abs(x - goalX)
    const dy = Math.abs(y - goalY)
    return Math.max(dx, dy) + (Math.SQRT2 - 1) * Math.min(dx, dy)
  }
  const open: Array<{ node: number; score: number }> = []
  const push = (node: number, score: number) => {
    open.push({ node, score })
    let child = open.length - 1
    while (child > 0) {
      const parent = (child - 1) >> 1
      if (open[parent].score <= open[child].score) break
      ;[open[parent], open[child]] = [open[child], open[parent]]
      child = parent
    }
  }
  const pop = () => {
    const top = open[0]
    const last = open.pop()!
    if (open.length > 0) {
      open[0] = last
      let parent = 0
      for (;;) {
        const left = parent * 2 + 1
        const right = left + 1
        let smallest = parent
        if (left < open.length && open[left].score < open[smallest].score) smallest = left
        if (right < open.length && open[right].score < open[smallest].score) smallest = right
        if (smallest === parent) break
        ;[open[parent], open[smallest]] = [open[smallest], open[parent]]
        parent = smallest
      }
    }
    return top.node
  }

  const start = index(startX, startY)
  const goal = index(goalX, goalY)
  cost[start] = 0
  push(start, heuristic(startX, startY))
  while (open.length > 0 && !closed[goal]) {
    const node = pop()
    if (closed[node]) continue
    closed[node] = 1
    const x = (node % MAP_WIDTH) + MAP_LEFT
    const y = Math.floor(node / MAP_WIDTH) + MAP_TOP
    for (let dy = -1; dy <= 1; dy++) {
      for (let dx = -1; dx <= 1; dx++) {
        if ((dx === 0 && dy === 0) || !walkable(x + dx, y + dy)) continue
        if (dx !== 0 && dy !== 0 && (!walkable(x + dx, y) || !walkable(x, y + dy))) continue
        const next = index(x + dx, y + dy)
        const nextCost = cost[node] + (dx !== 0 && dy !== 0 ? Math.SQRT2 : 1)
        if (closed[next] || nextCost >= cost[next]) continue
        cost[next] = nextCost
        cameFrom[next] = node
        push(next, nextCost + heuristic(x + dx, y + dy))
      }
    }
  }
  if (!closed[goal]) return 'river'

  const centres: TilePoint[] = []
  for (let node = cameFrom[goal]; node !== start && node !== -1; node = cameFrom[node]) {
    centres.unshift({ x: (node % MAP_WIDTH) + MAP_LEFT + 0.5, y: Math.floor(node / MAP_WIDTH) + MAP_TOP + 0.5 })
  }
  const points = [from, ...centres, to]
  // Straightens the tile steps: from each turn, head for the farthest point still in plain sight.
  const way: TilePoint[] = []
  let at = 0
  while (at < points.length - 1) {
    let next = points.length - 1
    while (next > at + 1 && !clear(points[at], points[next])) next--
    way.push(points[next])
    at = next
  }
  return way
}

/** Natural places a click can send a citizen to; `spot` is the centre of the clicked tile. */
export type NatureKind = 'tree' | 'bush' | 'stone' | 'reeds' | 'water' | 'vein'

/** Something on the map a citizen can be sent to work at. `area` is outlined while the choice is open. */
export type WorldTarget = { area: TileArea | null } & (
  | { kind: 'site'; plotId: string; building: string }
  | { kind: 'building'; plotId: string; building: string; storage: boolean }
  | { kind: 'project'; projectId: string }
  | { kind: 'campfire' }
  | { kind: 'animal'; prey: AnimalKind }
  | { kind: NatureKind; spot: TilePoint }
)

export const PROJECT_BUILDINGS: Record<string, TileRef[][]> = {
  granary: [
    [town(52), town(53), town(53), town(54)],
    [town(64), town(65), town(65), town(66)],
    [town(84), town(86), town(87), town(84)],
  ],
  'bell-tower': [
    [town(48), town(49), town(50)],
    [town(60), town(61), town(62)],
    [town(76), town(77), town(79)],
    [town(88), town(90), town(88)],
  ],
}

function random(seed: number) {
  let state = seed >>> 0
  return () => {
    state = (state * 1664525 + 1013904223) >>> 0
    return state / 4294967296
  }
}

export function seedFrom(text: string): number {
  let value = 2166136261
  for (let index = 0; index < text.length; index++) {
    value ^= text.charCodeAt(index)
    value = Math.imul(value, 16777619)
  }
  return value >>> 0
}

/** Smooth value noise in 0…1 with features about `scale` tiles wide. */
function valueNoise(seed: number, scale: number) {
  const corner = (x: number, y: number) => {
    // Integer hash with a full avalanche (MurmurHash3 finaliser), so neighbouring corners are unrelated.
    let value = (seed ^ Math.imul(x, 0x27d4eb2d) ^ Math.imul(y, 0x165667b1)) >>> 0
    value = Math.imul(value ^ (value >>> 16), 0x85ebca6b)
    value = Math.imul(value ^ (value >>> 13), 0xc2b2ae35)
    return ((value ^ (value >>> 16)) >>> 0) / 4294967296
  }
  const smooth = (t: number) => t * t * (3 - 2 * t)
  return (x: number, y: number) => {
    const fx = x / scale
    const fy = y / scale
    const x0 = Math.floor(fx)
    const y0 = Math.floor(fy)
    const tx = smooth(fx - x0)
    const ty = smooth(fy - y0)
    const top = corner(x0, y0) * (1 - tx) + corner(x0 + 1, y0) * tx
    const bottom = corner(x0, y0 + 1) * (1 - tx) + corner(x0 + 1, y0 + 1) * tx
    return top * (1 - ty) + bottom * ty
  }
}

const inCore = (x: number, y: number) => x >= 0 && x < CORE_WIDTH && y >= 0 && y < CORE_HEIGHT

/** Centre column of the river in a row: straight through the camp area, meandering beyond it. */
function riverCenter(y: number, seed: number) {
  const straight = (RIVER_LEFT + RIVER_RIGHT) / 2
  const distance = y < 0 ? -y : y >= CORE_HEIGHT ? y - CORE_HEIGHT + 1 : 0
  if (distance === 0) return straight
  const phase = (seed % 1000) / 160
  const sway = Math.sin(distance * 0.11 + phase) * 7 + Math.sin(distance * 0.27 + phase * 2) * 2
  return straight + sway * Math.min(1, distance / 10)
}

/**
 * The land around the first campfire. The camp area keeps the features the server's workplaces expect
 * (forest west, stones north, berries south-west, reeds by the river); the rest is generated from `seed`,
 * so every village has its own land and every viewer of a village sees the same one.
 */
export function buildLayout(seed: number): MapLayout {
  const next = random(seed)
  const forestNoise = valueNoise(seed + 1, 9)
  const detailNoise = valueNoise(seed + 2, 3)
  const stoneNoise = valueNoise(seed + 3, 7)
  const ground: TileRef[][] = []
  const decor: PlacedTile[] = []
  const bushes: MapLayout['bushes'] = []
  const reeds: MapLayout['reeds'] = []
  const ponds: MapLayout['ponds'] = []
  const terrain = new Uint8Array(MAP_WIDTH * MAP_HEIGHT)
  const index = (x: number, y: number) => (y - MAP_TOP) * MAP_WIDTH + (x - MAP_LEFT)
  const inside = (x: number, y: number) => x >= MAP_LEFT && x < MAP_RIGHT && y >= MAP_TOP && y < MAP_BOTTOM
  const occupied = new Set<number>()
  const mark = (x: number, y: number, width = 1, height = 1, kind?: Terrain) => {
    for (let row = y; row < y + height; row++) {
      for (let column = x; column < x + width; column++) {
        if (!inside(column, row)) continue
        occupied.add(index(column, row))
        if (kind !== undefined) terrain[index(column, row)] = kind
      }
    }
  }
  const setGround = (x: number, y: number, tile: TileRef) => {
    if (inside(x, y)) ground[y - MAP_TOP][x - MAP_LEFT] = tile
  }

  for (let y = MAP_TOP; y < MAP_BOTTOM; y++) {
    const row: TileRef[] = []
    for (let x = MAP_LEFT; x < MAP_RIGHT; x++) {
      const roll = next()
      row.push(town(roll < 0.06 ? 1 : roll < 0.09 ? 2 : 0))
    }
    ground.push(row)
  }

  // The river runs the whole height of the map.
  for (let y = MAP_TOP; y < MAP_BOTTOM; y++) {
    const left = Math.round(riverCenter(y, seed) - 1.5)
    setGround(left, y, battle(36))
    setGround(left + 1, y, battle(37))
    setGround(left + 2, y, battle(37))
    setGround(left + 3, y, battle(38))
    mark(left, y, 4, 1, Terrain.Water)
    // Reeds grow on the west bank wherever the land is low.
    if (!inCore(left, y) && detailNoise(left, y) > 0.55) {
      reeds.push({ x: left - 0.6, y: y + 0.5 })
      mark(left - 1, y, 1, 1, Terrain.Reeds)
    }
  }

  // Ponds out in the wild, never in the camp area.
  for (let attempt = 0; attempt < 40 && ponds.length < 9; attempt++) {
    const x = MAP_LEFT + 4 + Math.floor(next() * (MAP_WIDTH - 8))
    const y = MAP_TOP + 4 + Math.floor(next() * (MAP_HEIGHT - 8))
    const radiusX = 2 + next() * 3.5
    const radiusY = 1.5 + next() * 2.5
    const clear = (cx: number, cy: number) => !inCore(cx, cy) && !occupied.has(index(cx, cy))
    let fits = true
    for (let row = Math.floor(y - radiusY - 1); row <= y + radiusY + 1 && fits; row++) {
      for (let column = Math.floor(x - radiusX - 1); column <= x + radiusX + 1 && fits; column++) {
        if (!inside(column, row) || !clear(column, row)) fits = false
      }
    }
    if (!fits) continue
    ponds.push({ x: x + 0.5, y: y + 0.5, radiusX, radiusY })
    for (let row = Math.floor(y - radiusY); row <= y + radiusY; row++) {
      for (let column = Math.floor(x - radiusX); column <= x + radiusX; column++) {
        if (((column - x) / radiusX) ** 2 + ((row - y) / radiusY) ** 2 <= 1) mark(column, row, 1, 1, Terrain.Water)
      }
    }
    // A ring of reeds around some ponds.
    if (next() < 0.6) {
      for (let angle = 0; angle < Math.PI * 2; angle += 0.5) {
        const rx = x + 0.5 + Math.cos(angle) * (radiusX + 0.6)
        const ry = y + 0.5 + Math.sin(angle) * (radiusY + 0.6)
        if (next() < 0.7) {
          reeds.push({ x: rx, y: ry })
          mark(Math.floor(rx), Math.floor(ry), 1, 1, Terrain.Reeds)
        }
      }
    }
  }

  // Loose stones on the hillside north of the camp.
  for (const [x, y] of [[9, 1], [10, 1], [11, 1], [9, 2], [10, 3], [11, 2], [12, 2], [8, 3], [12, 4]]) {
    setGround(x, y, town(43))
    mark(x, y, 1, 1, Terrain.Stone)
  }

  // Berry bushes in the meadow south-west of the camp.
  for (let y = 16; y <= 20; y++) {
    for (let x = 1; x <= 7; x++) {
      if (next() < 0.45) {
        bushes.push({ x: x + 0.5, y: y + 0.6 })
        mark(x, y, 1, 1, Terrain.Bush)
      }
    }
  }
  mark(1, 16, 7, 5)

  // Reeds along the river bank by the camp.
  for (let y = 14; y <= 22; y++) {
    const pierRow = Math.abs(y - PIER.y) <= 1
    const bank = { x: RIVER_LEFT - 0.6, y: y + 0.5 + (next() - 0.5) * 0.4 }
    if (!pierRow) reeds.push(bank)
    if (next() < 0.6 && !pierRow) reeds.push({ x: RIVER_LEFT - 1.3, y: y + 0.3 })
    mark(RIVER_LEFT - 2, y, 2, 1, Terrain.Reeds)
  }

  // Camp clearing with the bare plot east of it, and the trail to the gate. Mirrors the server's bare ground.
  mark(12, 10, 7, 7, Terrain.Clearing)
  mark(MONUMENT.x - 1, MONUMENT.y - 1, 3, 4, Terrain.Clearing)
  mark(0, 12, 12, 3, Terrain.Clearing)
  mark(17, 12, PLOT_WIDTH, PLOT_HEIGHT, Terrain.Clearing)
  const monastery = MONASTERY.area
  mark(monastery.x, monastery.y, monastery.width, monastery.height, Terrain.Clearing)
  const bellTower = PROJECT_SITES['bell-tower']
  mark(bellTower.x - 3, bellTower.y - 1, 7, 6, Terrain.Clearing)
  mark(RIVER_RIGHT + 1, ROAD_TOP, MAP_RIGHT - RIVER_RIGHT - 1, 2, Terrain.Clearing)
  mark(VEIN_OUTCROP.x - 1, VEIN_OUTCROP.y - 1, VEIN_OUTCROP.width + 2, VEIN_OUTCROP.height + 2, Terrain.Clearing)
  for (let y = VEIN_OUTCROP.y; y < VEIN_OUTCROP.y + VEIN_OUTCROP.height; y++) {
    for (let x = VEIN_OUTCROP.x; x < VEIN_OUTCROP.x + VEIN_OUTCROP.width; x++) {
      setGround(x, y, town(43))
      mark(x, y, 1, 1, Terrain.Stone)
    }
  }

  const plantTree = (x: number, y: number) => {
    const look = TREE_TILES[Math.floor(next() * TREE_TILES.length)]
    // Trees of the forest area come from the server; the roll and the mark still happen so the wild keeps its look.
    if (inForestArea(x, y)) return mark(x, y)
    decor.push({ ...look, x, y })
    mark(x, y, 1, 1, Terrain.Tree)
  }

  // Camp area: forest on the left, hunting woods in the south, groves elsewhere.
  const plant = (x0: number, y0: number, x1: number, y1: number, density: number) => {
    for (let y = y0; y <= y1; y++) {
      for (let x = x0; x <= x1; x++) {
        if (occupied.has(index(x, y)) || next() > density) continue
        plantTree(x, y)
      }
    }
  }
  plant(0, 0, 7, 11, 0.8)
  plant(0, 21, 16, 25, 0.55)
  plant(0, 0, 29, 3, 0.3)
  plant(34, 0, 43, 25, 0.45)
  plant(8, 4, 29, 20, 0.08)
  plant(17, 21, 29, 25, 0.3)

  // The wild: forests and stony ground follow the noise; bushes and lone trees dot the meadows.
  for (let y = MAP_TOP; y < MAP_BOTTOM; y++) {
    for (let x = MAP_LEFT; x < MAP_RIGHT; x++) {
      if (inCore(x, y) || occupied.has(index(x, y))) continue
      const forest = forestNoise(x, y) * 0.75 + detailNoise(x, y) * 0.25
      const stone = stoneNoise(x, y)
      const roll = next()
      if (stone > 0.74 && forest < 0.55) {
        setGround(x, y, town(43))
        mark(x, y, 1, 1, Terrain.Stone)
      } else if (forest > 0.56 ? roll < 0.85 : forest > 0.48 ? roll < 0.3 : roll < 0.03) {
        plantTree(x, y)
      } else if (forest > 0.4 && forest < 0.5 && roll > 0.97) {
        bushes.push({ x: x + 0.5, y: y + 0.6 })
        mark(x, y, 1, 1, Terrain.Bush)
      }
    }
  }

  return { ground, decor, bushes, reeds, ponds, terrain }
}

/** Villager sprites from Tiny Dungeon (the hooded monk, 111, is reserved for the elder). */
export const VILLAGER_SPRITES = [85, 86, 87, 88, 96, 97, 98, 99, 100, 112, 84]
export const PRIEST_SPRITE = 111

const HAMMER = factory(122)
const PICKAXE = town(115)
const STICK = farm(2)
const ROCKS = farm(89)
const REEDS = farm(65)
const POST = town(47)

export const RESOURCE_ICONS: Record<string, TileRef> = {
  Wood: town(106),
  Stone: ROCKS,
  Food: town(29),
  Coal: EXTRAS.coal,
  IronOre: dungeon(41),
  IronIngot: dungeon(102),
  Planks: town(73),
  Leather: dungeon(66),
  Gold: town(93),
  Chain: factory(115),
  Mechanism: factory(114),
  Remedy: dungeon(115),
  ResearchPoints: dungeon(65),
  Reeds: REEDS,
  Feathers: EXTRAS.feather,
  Flax: farm(80),
  Grain: farm(68),
  VigorDrink: dungeon(116),
}

export const TASK_ICONS: Record<string, TileRef> = {
  'chop-wood': town(127),
  'quarry-stone': PICKAXE,
  forage: town(29),
  'cut-reeds': farm(88),
  hunt: town(118),
  'hunt-birds': town(119),
  'tend-fire': EXTRAS.campfire,
  'carry-to-storage': town(92),
  'track-game': town(118),
  'tend-fields': farm(66),
  'sow-turnips': farm(21),
  'sow-carrots': farm(9),
  'sow-cabbage': farm(57),
  'sow-flax': farm(10),
  'sow-rye': farm(69),
  'water-fields': farm(73),
  harvest: farm(11),
  'read-books': dungeon(63),
  'chat-tavern': town(107),
  'deliver-wood': RESOURCE_ICONS.Wood,
  'deliver-stone': RESOURCE_ICONS.Stone,
  'deliver-planks': RESOURCE_ICONS.Planks,
  'deliver-reeds': RESOURCE_ICONS.Reeds,
  'deliver-gold': RESOURCE_ICONS.Gold,
  fish: town(131),
  'measure-river': POST,
  'dig-gold': RESOURCE_ICONS.Gold,
  'deliver-iron': RESOURCE_ICONS.IronIngot,
  build: HAMMER,
  expedition: town(119),
  roam: town(29),
  'tell-stories': EXTRAS.campfire,
  'study-plants': farm(80),
  'explore-land': town(119),
  'research-technology': dungeon(63),
  'mine-coal': RESOURCE_ICONS.Coal,
  'mine-iron-ore': RESOURCE_ICONS.IronOre,
  'smelt-ingot': dungeon(117),
  'saw-planks': dungeon(118),
  'bake-bread': farm(125),
  research: dungeon(63),
  survey: town(83),
  supervise: factory(125),
  trade: farm(74),
  perform: town(95),
  patrol: dungeon(104),
  'mine-gold': RESOURCE_ICONS.Gold,
  'forge-chain': RESOURCE_ICONS.Chain,
  'build-mechanism': RESOURCE_ICONS.Mechanism,
  explore: town(119),
  design: dungeon(65),
  'brew-remedy': dungeon(115),
  'brew-vigor': RESOURCE_ICONS.VigorDrink,
  'lead-crew': factory(120),
  negotiate: battle(196),
  command: battle(70),
  'bring-sticks': STICK,
  'bring-logs': RESOURCE_ICONS.Wood,
  'bring-stones': RESOURCE_ICONS.Stone,
  'bring-reeds': RESOURCE_ICONS.Reeds,
  'bring-hides': RESOURCE_ICONS.Leather,
  'bring-planks': RESOURCE_ICONS.Planks,
  'fell-trees': town(127),
  'clear-ground': farm(86),
  'mark-outline': POST,
  'lean-poles': STICK,
  'lay-cover': REEDS,
  'stack-pile': RESOURCE_ICONS.Wood,
  'lay-ring': farm(77),
  'tie-rack': POST,
  'lay-stones': farm(77),
  'hew-beams': town(127),
  'raise-posts': POST,
  'tie-frame': town(44),
  'mix-clay': farm(124),
  'bundle-thatch': farm(68),
  'thatch-roof': farm(97),
  'plough-field': town(129),
  'dig-shaft': PICKAXE,
  'build-forge': dungeon(74),
  'build-oven': farm(125),
  'lay-floor': RESOURCE_ICONS.Planks,
  'lend-a-hand': HAMMER,
}

/** Icon for a task or a construction job. Labor jobs without their own icon show a hammer. */
export const taskIcon = (id: string) => TASK_ICONS[id] ?? HAMMER
