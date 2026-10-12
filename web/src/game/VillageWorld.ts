import { Application, CanvasSource, ColorMatrixFilter, Container, Culler, Graphics, Sprite, Text, Texture, type Ticker } from 'pixi.js'
import { Minimap, type MinimapMarkers } from './Minimap'
import { isRipe, plotPosition, type AnimalKind, type BuildingDefinition, type Catalog, type CitizenSummary, type FieldBed, type Site, type Storage, type StorageDefinition, type TileArea, type TilePoint, type VillageState } from '../api'
import { WildlifeLayer } from './animals'
import { CharacterPainter, appearanceKey } from './appearance'
import { TILE, TileSet, battle, dungeon, town, type TileRef } from './sheets'
import {
  BUILDING_TILES,
  CAMPFIRE,
  HEAP_PLOT,
  MAP_HEIGHT,
  MAP_LEFT,
  MONASTERY,
  MAP_TOP,
  MAP_WIDTH,
  PLOT_HEIGHT,
  PLOT_WIDTH,
  PRIEST_SPOT,
  PRIEST_SPRITE,
  PROJECT_BUILDINGS,
  PROJECT_SITES,
  RESOURCE_ICONS,
  RIVER_LEFT,
  RIVER_RIGHT,
  ROAD_TOP,
  ROAMING_SPOTS,
  VILLAGER_SPRITES,
  WILDS,
  WORKPLACES,
  MONUMENT,
  PALISADE,
  PIER,
  VEIN_OUTCROP,
  PROJECT_AREAS,
  PROJECT_YARDS,
  Terrain,
  buildLayout,
  buildingFootprints,
  canBuildAt,
  cropTile,
  footprint,
  seedFrom,
  taskIcon,
  tileBuiltOver,
  treeAt,
  WET_BED,
  type BuildingCell,
  type NatureKind,
  type PlacedTile,
  type WorldTarget,
} from './worldMap'

const WORLD_WIDTH = MAP_WIDTH * TILE
const WORLD_HEIGHT = MAP_HEIGHT * TILE
const WALK_SPEED = 26
/** Hunters run after their prey; near the end of the chase they go faster if they must to reach it in time. */
const RUN_SPEED = 44
const HUNTER_HOLD_DISTANCE = TILE * 0.9
const HUNTER_POUNCE_SECONDS = 2
const BUBBLE_SECONDS = 5
/**
 * Seconds between two depth re-sorts of moving figures. Any depth change makes PixiJS sort thousands of trees and
 * rebuild the whole scene, so it is not done every frame.
 */
const DEPTH_SORT_SECONDS = 0.25
/** Nugget spots on the gold vein outcrop, in tiles from its top-left corner. */
const VEIN_NUGGETS: Array<[number, number]> = [
  [0.1, 0.2],
  [1.1, -0.1],
  [2.0, 0.3],
  [0.6, 0.9],
  [1.6, 1.0],
]
const LABEL_FONT = 'system-ui, "Segoe UI", sans-serif'
/** Closest zoom: one tile is 4 × 16 = 64 screen pixels. */
const MAXIMUM_SCALE = 4
/** Tiles across the screen width at the start. */
const DEFAULT_VIEW_TILES = 34
const MINIMUM_DEFAULT_TILE_PIXELS = 28
/** How close to an animal, in world pixels, a click still picks it. */
const ANIMAL_REACH = TILE * 0.8
/** How close to the fire, in tiles, a click picks the campfire. */
const CAMPFIRE_REACH = 1.2

const NATURE: Partial<Record<number, NatureKind>> = {
  [Terrain.Tree]: 'tree',
  [Terrain.Bush]: 'bush',
  [Terrain.Stone]: 'stone',
  [Terrain.Reeds]: 'reeds',
  [Terrain.Water]: 'water',
}

export const PRIEST_ID = 'priest'

interface Walker {
  id: string
  sprite: Sprite
  icon: Container
  label: Text
  subtitle: Text
  x: number
  y: number
  targetX: number
  targetY: number
  working: boolean
  /** Roaming the land: keeps walking from one random spot to the next while the task runs. */
  roaming: boolean
  /** Sent somewhere by the player ("come here"): stands there instead of wandering around the camp. */
  staying: boolean
  wanderAt: number
  phase: number
  seed: number
  /** Hunting: chases an animal and reaches it at `catchAt` (wall clock, milliseconds), then carries it home. */
  hunt: { prey: AnimalKind; catchAt: number } | null
}

interface Bubble {
  ownerId: string
  view: Container
  expiresAt: number
}

function hash(text: string): number {
  let value = 2166136261
  for (let index = 0; index < text.length; index++) {
    value ^= text.charCodeAt(index)
    value = Math.imul(value, 16777619)
  }
  return value >>> 0
}

function pseudo(seed: number, salt: number): number {
  return (hash(`${seed}:${salt}`) % 10000) / 10000
}

export class VillageWorld {
  private readonly app: Application
  private readonly tiles: TileSet
  private readonly painter: CharacterPainter
  private readonly lookTextures = new Map<string, Texture>()
  private readonly world = new Container()
  private readonly objects = new Container()
  private readonly wildlife = new WildlifeLayer(this.objects)
  private readonly projectLayer = new Container()
  private readonly night = new Graphics()
  private readonly lights = new Graphics()
  private readonly flames = new Graphics()
  private readonly screen = new Container()
  private readonly walkers = new Map<string, Walker>()
  private readonly bubbles: Bubble[] = []
  private depthSortedAt = Number.NEGATIVE_INFINITY
  /**
   * Trees, bushes and reeds out of view are skipped when rendering; the map has thousands of them. They never move,
   * so they are culled again only when the camera moves or scenery is added.
   */
  private cullDirty = true
  private highlightId: string | null = null
  private nightAmount = 0
  private nightTarget = 0
  private elapsed = 0
  /** Screen pixels per world pixel. */
  private scale = 1
  /** Position of the view inside the map: 0 is the left or top edge, 1 the right or bottom edge. */
  private panX = 0.5
  private panY = 0.5
  private readonly groundLayer = new Container()
  private terrainSeed: number | null = null
  private minimap: Minimap | null = null
  private minimapUpdatedAt = 0
  /** Until the player pans or zooms, a resize keeps showing the camp at the default zoom. */
  private cameraMoved = false
  private viewCenter = { x: CAMPFIRE.x, y: CAMPFIRE.y }
  private dragging: { x: number; y: number; panX: number; panY: number; moved: boolean } | null = null
  private lastProjectKey = ''
  private activeProjectId: string | null = null
  private lastBuildingKey = ''
  private buildingViews: Container[] = []
  /** Names above buildings and sites, kept at screen size; `x` and `y` are the top centre of the plot in world pixels. */
  private buildingLabels: Array<{ text: Text; x: number; y: number }> = []
  private buildingName: ((kind: string, storage: boolean) => string) | null = null
  private storageDefinitions = new Map<string, StorageDefinition>()
  private buildingDefinitions = new Map<string, BuildingDefinition>()
  private plots: Record<string, TilePoint> = { heap: HEAP_PLOT }
  private catalog: Catalog | null = null
  private village: VillageState | null = null
  private citizens: CitizenSummary[] = []
  /** Trees and bushes by tile, hidden while a building stands on them. */
  private scenery: Array<{ view: Container; x: number; y: number }> = []
  /** Standing trees of the forest area by "x,y"; they come and go with the village state. */
  private readonly trees = new Map<string, Container>()
  /** What covers each tile, shared with the minimap and the wildlife; trees of the forest area change it. */
  private terrain: Uint8Array | null = null
  private readonly placementLayer = new Container()
  private readonly ghost = new Graphics()
  private onPick: ((x: number, y: number) => void) | null = null
  private onCitizenPick: ((citizenId: string) => void) | null = null
  private onWorldPick: ((target: WorldTarget | null, screen: { x: number; y: number }, point: TilePoint) => void) | null = null
  /** Outline around what the player clicked while they choose what to do there. */
  private readonly selection = new Graphics()

  private constructor(app: Application, tiles: TileSet, painter: CharacterPainter, host: HTMLElement, minimap: boolean) {
    this.app = app
    this.tiles = tiles
    this.painter = painter

    this.objects.sortableChildren = true
    this.projectLayer.sortableChildren = true
    this.objects.addChild(this.campfireView(), this.projectLayer)

    this.night.rect(MAP_LEFT * TILE, MAP_TOP * TILE, WORLD_WIDTH, WORLD_HEIGHT).fill({ color: 0x0b1640 })
    this.night.alpha = 0
    this.lights.blendMode = 'add'
    this.lights.alpha = 0
    this.lights.circle(CAMPFIRE.x * TILE, CAMPFIRE.y * TILE, 3.2 * TILE).fill({ color: 0xff9a3c, alpha: 0.12 })
    this.lights.circle(CAMPFIRE.x * TILE, CAMPFIRE.y * TILE, 1.6 * TILE).fill({ color: 0xffb347, alpha: 0.25 })

    this.placementLayer.addChild(this.ghost)
    this.placementLayer.visible = false
    this.selection.visible = false
    this.world.addChild(this.groundLayer, this.objects, this.placementLayer, this.selection, this.night, this.lights)
    app.stage.addChild(this.world, this.screen)

    this.addWalker(PRIEST_ID, '', this.tiles.get(dungeon(PRIEST_SPRITE)), PRIEST_SPOT.x * TILE, PRIEST_SPOT.y * TILE)

    if (minimap) {
      this.minimap = new Minimap(host, (x, y) => {
        this.cameraMoved = true
        this.centerOn(x, y)
      })
    }
    this.layoutCamera()
    app.renderer.on('resize', () => {
      if (!this.cameraMoved) return this.showCamp()
      const center = this.viewCenter
      this.layoutCamera()
      this.centerOn(center.x, center.y)
    })
    app.ticker.add((ticker) => this.update(ticker))
    this.bindPointer()
  }

  static async create(host: HTMLElement, options: { transparent?: boolean; minimap?: boolean } = {}): Promise<VillageWorld> {
    const [tiles, painter, app] = await Promise.all([TileSet.load(), CharacterPainter.load(), Promise.resolve(new Application())])
    await app.init({
      resizeTo: host,
      background: '#5a8f3c',
      backgroundAlpha: options.transparent ? 0 : 1,
      antialias: false,
      roundPixels: true,
      autoDensity: true,
      resolution: Math.min(window.devicePixelRatio || 1, 2),
    })
    host.appendChild(app.canvas)
    return new VillageWorld(app, tiles, painter, host, options.minimap ?? true)
  }

  destroy() {
    this.minimap?.destroy()
    this.app.canvas.remove()
    this.app.destroy(true, { children: true })
    for (const texture of this.lookTextures.values()) texture.destroy(true)
  }

  /** The land is generated from the village id, so it is built once the village is known. */
  private buildTerrain(seed: number) {
    const layout = buildLayout(seed)
    const ground = new Container()
    layout.ground.forEach((row, y) =>
      row.forEach((tile, x) => {
        const sprite = new Sprite(this.tiles.get(tile))
        sprite.position.set((x + MAP_LEFT) * TILE, (y + MAP_TOP) * TILE)
        ground.addChild(sprite)
      }),
    )
    const water = new Graphics()
    for (const pond of layout.ponds) {
      water
        .ellipse(pond.x * TILE, pond.y * TILE, (pond.radiusX + 0.35) * TILE, (pond.radiusY + 0.35) * TILE)
        .fill({ color: 0xd8f4ff })
        .ellipse(pond.x * TILE, pond.y * TILE, pond.radiusX * TILE, pond.radiusY * TILE)
        .fill({ color: 0x5ac8f0 })
        .ellipse(pond.x * TILE - pond.radiusX * 3, pond.y * TILE - pond.radiusY * 3, pond.radiusX * 4, pond.radiusY * 2)
        .fill({ color: 0x8adcf8, alpha: 0.6 })
    }
    // Trampled earth around the campfire.
    water
      .ellipse(CAMPFIRE.x * TILE, CAMPFIRE.y * TILE, 2.6 * TILE, 1.9 * TILE)
      .fill({ color: 0x8a6a3f, alpha: 0.55 })
      .ellipse(CAMPFIRE.x * TILE, CAMPFIRE.y * TILE, 1.6 * TILE, 1.1 * TILE)
      .fill({ color: 0x7a5a32, alpha: 0.5 })
    ground.addChild(water)
    ground.cacheAsTexture(true)
    this.groundLayer.addChild(ground)

    const cull = <T extends Container>(view: T) => {
      view.cullable = true
      return view
    }
    const addScenery = (view: Container, x: number, y: number) => {
      this.objects.addChild(cull(view))
      this.scenery.push({ view, x: Math.floor(x), y: Math.floor(y) })
    }
    for (const placed of layout.decor) addScenery(this.decorView(placed), placed.x, placed.y)
    for (const bush of layout.bushes) addScenery(this.bushView(bush.x, bush.y), bush.x, bush.y)
    for (const reed of layout.reeds) this.objects.addChild(cull(this.reedView(reed.x, reed.y)))
    const church = MONASTERY.church
    this.objects.addChild(this.buildingView(church.tiles, church.x, church.y, 'blue'))
    for (const cell of MONASTERY.cells) this.objects.addChild(this.buildingView(MONASTERY.cell, cell.x, cell.y))
    this.terrain = layout.terrain
    this.minimap?.setTerrain(layout.terrain)
    this.wildlife.setTerrain(layout.terrain)
    this.cullDirty = true
  }

  /** Adds trees that grew back and removes felled ones. */
  private drawTrees(trees: VillageState['trees']) {
    const terrain = this.terrain
    if (!terrain) return
    const tileIndex = (x: number, y: number) => (y - MAP_TOP) * MAP_WIDTH + (x - MAP_LEFT)
    const standing = new Set(trees.map((tree) => `${tree.x},${tree.y}`))
    let changed = false
    for (const [key, view] of this.trees) {
      if (standing.has(key)) continue
      view.destroy({ children: true })
      this.trees.delete(key)
      const [x, y] = key.split(',').map(Number)
      terrain[tileIndex(x, y)] = Terrain.Grass
      changed = true
    }
    for (const tree of trees) {
      const key = `${tree.x},${tree.y}`
      if (this.trees.has(key)) continue
      const view = this.decorView(treeAt(tree.x, tree.y))
      view.cullable = true
      this.objects.addChild(view)
      this.trees.set(key, view)
      terrain[tileIndex(tree.x, tree.y)] = Terrain.Tree
      changed = true
      this.cullDirty = true
    }
    if (changed) this.minimap?.setTerrain(terrain)
  }

  setPriestName(name: string) {
    const priest = this.walkers.get(PRIEST_ID)
    if (priest) priest.label.text = name
  }

  /** How buildings are named on the map, in the player's language. */
  setBuildingNames(name: (kind: string, storage: boolean) => string) {
    this.buildingName = name
    this.lastBuildingKey = ''
    this.drawBuildings()
  }

  setNight(isNight: boolean) {
    this.nightTarget = isNight ? 1 : 0
  }

  setCatalog(catalog: Catalog | null) {
    if (!catalog) return
    this.catalog = catalog
    this.storageDefinitions = new Map(catalog.storages.map((definition) => [definition.kind, definition]))
    this.buildingDefinitions = new Map(catalog.buildings.map((definition) => [definition.kind, definition]))
    this.plots = catalog.plots
    this.lastBuildingKey = ''
    this.drawBuildings()
    this.setCitizens(this.citizens, this.highlightId)
  }

  private plot(plotId: string) {
    return plotPosition(this.plots, plotId)
  }

  /**
   * Starts placing a building anywhere on free land: an outline follows the pointer, and a click (not a drag)
   * on free land calls `onPick` with the top-left tile. Null ends placement.
   */
  setPlacement(onPick: ((x: number, y: number) => void) | null) {
    this.onPick = onPick
    this.app.canvas.style.cursor = ''
    this.ghost.clear()
    this.placementLayer.visible = false
  }

  /** A click (not a drag) on a citizen calls `onPick` with their id, unless a building is being placed. Null turns it off. */
  setCitizenPick(onPick: ((citizenId: string) => void) | null) {
    this.onCitizenPick = onPick
    if (!onPick) this.app.canvas.style.cursor = ''
  }

  /** The front-most citizen under a screen point; the name above the head counts too. */
  private citizenAt(point: { x: number; y: number }): string | null {
    const padding = 4
    let found: Walker | null = null
    for (const walker of this.walkers.values()) {
      if (walker.id === PRIEST_ID) continue
      const hit = [walker.sprite, walker.label, walker.subtitle].some((view) => {
        if (!view.visible) return false
        const bounds = view.getBounds()
        return (
          point.x >= bounds.minX - padding &&
          point.x <= bounds.maxX + padding &&
          point.y >= bounds.minY - padding &&
          point.y <= bounds.maxY + padding
        )
      })
      if (hit && (!found || walker.y > found.y)) found = walker
    }
    return found?.id ?? null
  }

  /**
   * A click (not a drag) on the map calls `onPick` with what a citizen can work at there (null for nothing),
   * the screen point and the map point in tiles, unless a building is being placed or a citizen was clicked.
   * Null turns it off.
   */
  setWorldPick(onPick: ((target: WorldTarget | null, screen: { x: number; y: number }, point: TilePoint) => void) | null) {
    this.onWorldPick = onPick
    if (!onPick) this.app.canvas.style.cursor = ''
  }

  /** Outlines tiles of the map; null removes the outline. */
  setSelection(area: TileArea | null) {
    this.selection.clear()
    this.selection.visible = area !== null
    if (!area) return
    this.selection
      .roundRect(area.x * TILE - 1, area.y * TILE - 1, area.width * TILE + 2, area.height * TILE + 2, 3)
      .fill({ color: 0xffe066, alpha: 0.18 })
      .stroke({ color: 0xffe066, width: 1.5 })
  }

  /** What a citizen could work at under a point of the world, in world pixels. */
  private targetAt(worldX: number, worldY: number): WorldTarget | null {
    const village = this.village
    const terrain = this.terrain
    if (!village || !terrain) return null
    const x = Math.floor(worldX / TILE)
    const y = Math.floor(worldY / TILE)
    const inside = (area: TileArea) => x >= area.x && x < area.x + area.width && y >= area.y && y < area.y + area.height
    const tile = (tileX: number, tileY: number): TileArea => ({ x: tileX, y: tileY, width: 1, height: 1 })

    const prey = this.wildlife.animalAt(worldX, worldY, ANIMAL_REACH)
    if (prey) return { kind: 'animal', prey, area: null }

    for (const site of village.sites) {
      const plot = this.plot(site.plotId)
      const area = plot && footprint(plot.x, plot.y)
      if (area && inside(area)) return { kind: 'site', plotId: site.plotId, building: site.kind, area }
    }
    const buildings = [
      ...village.structures.map((structure) => ({ plotId: structure.plotId, building: structure.kind, storage: false })),
      ...village.storages.map((storage) => ({
        plotId: storage.plotId,
        building: this.catalog?.buildings.find((definition) => definition.storage === storage.kind)?.kind ?? storage.kind,
        storage: true,
      })),
    ]
    for (const building of buildings) {
      const plot = this.plot(building.plotId)
      const area = plot && footprint(plot.x, plot.y)
      if (area && inside(area)) return { kind: 'building', ...building, area }
    }

    const project = village.project
    const projectArea = project ? PROJECT_AREAS[project.definitionId]?.find(inside) : undefined
    if (project && projectArea) return { kind: 'project', projectId: project.definitionId, area: projectArea }

    if (Math.hypot(worldX / TILE - CAMPFIRE.x, worldY / TILE - CAMPFIRE.y) < CAMPFIRE_REACH) {
      return { kind: 'campfire', area: tile(Math.floor(CAMPFIRE.x), Math.floor(CAMPFIRE.y)) }
    }
    if (inside(VEIN_OUTCROP)) return { kind: 'vein', spot: { x: x + 0.5, y: y + 0.5 }, area: VEIN_OUTCROP }

    const natureAt = (tileX: number, tileY: number) => {
      if (tileX < MAP_LEFT || tileY < MAP_TOP || tileX >= MAP_LEFT + MAP_WIDTH || tileY >= MAP_TOP + MAP_HEIGHT) return undefined
      return NATURE[terrain[(tileY - MAP_TOP) * MAP_WIDTH + (tileX - MAP_LEFT)]]
    }
    const kind = natureAt(x, y)
    if (kind) return { kind, spot: { x: x + 0.5, y: y + 0.5 }, area: tile(x, y) }
    // Tall trees reach into the tile above their trunk.
    if (natureAt(x, y + 1) === 'tree' && !(this.catalog && tileBuiltOver(this.catalog, village, x, y + 1))) return { kind: 'tree', spot: { x: x + 0.5, y: y + 1.5 }, area: tile(x, y + 1) }
    return null
  }

  /** Top-left tile of a building centred under a point of the world, in world pixels. */
  private placementAt(worldX: number, worldY: number) {
    const x = Math.floor(worldX / TILE) - Math.floor(PLOT_WIDTH / 2)
    const y = Math.floor(worldY / TILE) - 1
    const free = this.catalog !== null && this.village !== null && canBuildAt(this.catalog, this.village, x, y)
    return { x, y, free }
  }

  private showGhost(x: number, y: number, free: boolean) {
    const color = free ? 0xffe066 : 0xe05050
    this.ghost
      .clear()
      .rect(x * TILE, y * TILE, PLOT_WIDTH * TILE, PLOT_HEIGHT * TILE)
      .fill({ color, alpha: 0.3 })
      .stroke({ color, width: 2 })
    if (free) {
      this.ghost
        .rect((x + 1.5) * TILE - 2, (y + 2) * TILE - 8, 4, 16)
        .rect((x + 1.5) * TILE - 8, (y + 2) * TILE - 2, 16, 4)
        .fill({ color: 0xfff8e7 })
    }
    this.placementLayer.visible = true
  }

  setCitizens(citizens: CitizenSummary[], highlightId: string | null) {
    this.citizens = citizens
    this.highlightId = highlightId
    const seen = new Set<string>([PRIEST_ID])
    for (const citizen of citizens) {
      seen.add(citizen.id)
      let walker = this.walkers.get(citizen.id)
      const texture = this.citizenTexture(citizen)
      if (!walker) {
        const seed = hash(citizen.id)
        const start = WORKPLACES.camp
        walker = this.addWalker(
          citizen.id,
          citizen.name,
          texture,
          (start.x + (pseudo(seed, 1) - 0.5) * start.spread * 2) * TILE,
          (start.y + (pseudo(seed, 2) - 0.5)) * TILE,
        )
      }
      walker.sprite.texture = texture
      walker.label.text = citizen.name
      walker.label.style.fill = citizen.id === highlightId ? 0xffe066 : 0xffffff
      walker.subtitle.text = citizen.subtitle ?? ''
      walker.subtitle.visible = citizen.subtitle !== null

      const working = citizen.state === 'Working' && (citizen.workplace !== null || citizen.siteId !== null)
      const carried = citizen.carrying?.[0]
      const stayAt = working ? null : citizen.idleSpot
      const waitAt = !working && !stayAt && carried ? this.storageYardFor(carried) : null
      walker.working = working || waitAt !== null
      walker.staying = stayAt !== null
      const roaming = working && citizen.workplace === WILDS
      if (roaming && !walker.roaming) walker.wanderAt = 0
      walker.roaming = roaming
      const hunt = this.huntOf(citizen)
      if (!hunt && walker.hunt) this.wildlife.endHunt(walker.id)
      walker.hunt = hunt
      walker.icon.removeChildren().forEach((child) => child.destroy())
      if (hunt && citizen.taskId) {
        walker.icon.addChild(...this.iconBubble(taskIcon(citizen.taskId)))
      } else if (roaming && citizen.taskId) {
        walker.icon.addChild(...this.iconBubble(taskIcon(citizen.taskId)))
      } else if (working && citizen.taskId) {
        const plot = citizen.siteId ? this.plot(citizen.siteId) : null
        // A picked tree, bush or stone: stand just in front of it.
        const spot = plot
          ? { x: plot.x + PLOT_WIDTH / 2, y: plot.y + PLOT_HEIGHT - 0.4, spread: PLOT_WIDTH / 2 }
          : citizen.spot
            ? { x: citizen.spot.x, y: citizen.spot.y + 0.55, spread: 0.3 }
            : citizen.workplace === 'project' && this.activeProjectId && PROJECT_YARDS[this.activeProjectId]
            ? PROJECT_YARDS[this.activeProjectId]
            : (this.buildingYard(citizen.taskId) ?? WORKPLACES[citizen.workplace ?? 'camp'] ?? WORKPLACES.camp)
        walker.targetX = (spot.x + (pseudo(walker.seed, 3) - 0.5) * spot.spread * 2) * TILE
        walker.targetY = (spot.y + (pseudo(walker.seed, 4) - 0.5) * Math.min(spot.spread, 1)) * TILE
        walker.icon.addChild(...this.iconBubble(taskIcon(citizen.taskId)))
      } else if (stayAt) {
        walker.targetX = stayAt.x * TILE
        walker.targetY = stayAt.y * TILE
      } else if (waitAt && carried) {
        // Waiting at a full storage building with the load in hands: the crowd is the Governor's only hint.
        walker.targetX = (waitAt.x + (pseudo(walker.seed, 5) - 0.5) * waitAt.spread * 2) * TILE
        walker.targetY = (waitAt.y + (pseudo(walker.seed, 6) - 0.5) * 0.6) * TILE
        const icon = RESOURCE_ICONS[carried]
        if (icon) walker.icon.addChild(...this.iconBubble(icon))
      } else {
        walker.wanderAt = 0
      }
    }

    for (const [id, walker] of this.walkers) {
      if (!seen.has(id)) {
        this.wildlife.endHunt(id)
        walker.sprite.destroy()
        walker.icon.destroy({ children: true })
        walker.label.destroy()
        walker.subtitle.destroy()
        this.walkers.delete(id)
      }
    }
  }

  private iconBubble(icon: TileRef): [Graphics, Sprite] {
    const bubble = new Graphics().roundRect(-6, -6, 12, 12, 3).fill({ color: 0xfff5dc }).stroke({ color: 0x3a2a1a, width: 1 })
    const sprite = new Sprite(this.tiles.get(icon))
    sprite.anchor.set(0.5)
    sprite.scale.set(0.625)
    return [bubble, sprite]
  }

  /** The chase of a citizen on a hunting task: it lasts the task's working time, the rest of the cycle is the walk home. */
  private huntOf(citizen: CitizenSummary): Walker['hunt'] {
    if (citizen.state !== 'Working' || !citizen.taskId || !citizen.cycleEndsAt || !this.catalog) return null
    const task = this.catalog.tasks.find((candidate) => candidate.id === citizen.taskId)
    const prey = task?.hunt?.prey
    if (!task || !prey) return null
    const chaseMs = task.workSeconds * 1000
    const endsAt = Date.parse(citizen.cycleEndsAt)
    const startedAt = citizen.cycleStartedAt ? Date.parse(citizen.cycleStartedAt) : endsAt - chaseMs
    return { prey, catchAt: Math.min(endsAt, startedAt + chaseMs) }
  }

  /** Where workers of a task stand: in front of the first building the task needs, the same one the server walks from. */
  private buildingYard(taskId: string) {
    const kind = this.catalog?.tasks.find((task) => task.id === taskId)?.requiredBuilding
    const building = kind ? this.village?.structures.find((structure) => structure.kind === kind) : null
    const plot = building ? this.plot(building.plotId) : null
    return plot ? { x: plot.x + PLOT_WIDTH / 2, y: plot.y + PLOT_HEIGHT - 0.4, spread: PLOT_WIDTH / 2 } : null
  }

  /** Yard of the storage a carrier walks to: a specialised one if built, otherwise one that takes anything. */
  private storageYardFor(resource: string) {
    const storages = this.village?.storages ?? []
    const accepts = (storage: Storage, specialised: boolean) => {
      const definition = this.storageDefinitions.get(storage.kind)
      if (!definition) return false
      return specialised ? definition.accepts.includes(resource) : definition.accepts.length === 0
    }
    const storage = storages.find((s) => accepts(s, true)) ?? storages.find((s) => accepts(s, false))
    const plot = storage ? this.plot(storage.plotId) : null
    if (!storage || !plot) return null
    return { x: plot.x + PLOT_WIDTH / 2, y: plot.y + 4.1, spread: PLOT_WIDTH / 2 }
  }

  private drawBuildings() {
    const village = this.village
    if (!village) return
    const key = JSON.stringify([
      this.buildingDefinitions.size,
      village.storages.map((s) => [s.kind, s.plotId, s.slots.map((slot) => [slot.kind, Math.ceil(slot.fill * 4)])]),
      village.structures.map((s) => [s.kind, s.plotId]),
      village.sites.map((s) => [s.kind, s.plotId, s.stage, Math.floor(s.progress * 24)]),
      village.fields.map((bed) => [bed.plotId, bed.crop, cropTile(bed.crop ?? '', bed.growth, isRipe(bed))?.index, this.isWet(bed)]),
    ])
    if (key === this.lastBuildingKey) return
    this.lastBuildingKey = key

    for (const view of this.buildingViews) view.destroy({ children: true })
    this.buildingViews = []
    for (const label of this.buildingLabels) label.text.destroy()
    this.buildingLabels = []
    const add = (view: Container) => {
      this.buildingViews.push(view)
      this.objects.addChild(view)
    }
    const name = (kind: string, storage: boolean, plot: TilePoint, alpha = 1) => {
      if (!this.buildingName) return
      const text = new Text({
        text: this.buildingName(kind, storage),
        style: {
          fontFamily: LABEL_FONT,
          fontSize: 11,
          fontWeight: '700',
          fill: 0xffe9b0,
          stroke: { color: 0x1b1206, width: 3 },
        },
      })
      text.anchor.set(0.5, 1)
      text.alpha = alpha
      this.screen.addChildAt(text, 0)
      this.buildingLabels.push({ text, x: (plot.x + PLOT_WIDTH / 2) * TILE, y: plot.y * TILE + 2 })
    }

    for (const structure of village.structures) {
      const plot = this.plot(structure.plotId)
      if (!plot) continue
      add(this.finishedView(structure.kind, plot.x, plot.y))
      name(structure.kind, false, plot)
      const beds = village.fields.filter((bed) => bed.plotId === structure.plotId)
      if (beds.length > 0) add(this.bedsView(beds, plot.x, plot.y))
    }

    for (const storage of village.storages) {
      const plot = this.plot(storage.plotId)
      const definition = this.storageDefinitions.get(storage.kind)
      if (!plot || !definition) continue
      add(this.finishedView(storage.kind, plot.x, plot.y))
      name(storage.kind, true, plot)
      add(this.goodsYard(storage, definition, plot.x, plot.y))
    }

    for (const site of village.sites) {
      const plot = this.plot(site.plotId)
      if (!plot) continue
      add(this.siteView(site, plot.x, plot.y))
      name(site.kind, false, plot, 0.7)
    }

    const covered = new Set<string>()
    for (const area of buildingFootprints(this.plots, village)) {
      for (let y = area.y; y < area.y + area.height; y++) {
        for (let x = area.x; x < area.x + area.width; x++) covered.add(`${x},${y}`)
      }
    }
    for (const item of this.scenery) item.view.visible = !covered.has(`${item.x},${item.y}`)
  }

  /** A finished building in the 3×3 area of a plot. Storage kinds share names with their building kinds. */
  private finishedView(kind: string, x: number, y: number): Container {
    const tiles = BUILDING_TILES[kind]
    if (tiles) return this.buildingView(tiles, x, y)
    const view = this.wildView(kind)
    view.position.set(x * TILE, y * TILE)
    view.zIndex = (y + 3) * TILE
    return view
  }

  private isWet(bed: FieldBed) {
    return bed.wateredUntil !== null && Date.parse(bed.wateredUntil) > Date.now()
  }

  /** Farm beds, one per row of the field: darker soil while wet, and a plant on every tile showing how far it grew. */
  private bedsView(beds: FieldBed[], x: number, y: number): Container {
    const view = new Container()
    for (const bed of beds) {
      if (this.isWet(bed)) WET_BED.forEach((tile, column) => view.addChild(this.localSprite(tile, column, bed.index)))
      const plant = bed.crop ? cropTile(bed.crop, bed.growth, isRipe(bed)) : undefined
      if (!plant) continue
      for (let column = 0; column < PLOT_WIDTH; column++) view.addChild(this.localSprite(plant, column, bed.index - 0.15))
    }
    view.position.set(x * TILE, y * TILE)
    view.zIndex = (y + 3) * TILE + 1
    return view
  }

  /** Buildings without walls (first-age shelters and racks, the stage), drawn as shapes and loose tiles within a 3×3 tile area. */
  private wildView(kind: string): Container {
    const view = new Container()
    const g = new Graphics()
    const T = TILE
    const pole = 0x6b4423
    const reed = 0xc9b458
    switch (kind) {
      case 'Heap':
        g.ellipse(1.5 * T, 2.4 * T, 1.4 * T, 0.55 * T).fill({ color: 0x8a6a3f, alpha: 0.7 })
        for (const [px, py] of [[0.2, 1.7], [2.6, 1.7], [0.4, 2.6], [2.4, 2.6]]) {
          view.addChild(this.localSprite(town(47), px - 0.5, py - 0.9))
        }
        break
      case 'LeanTo':
        g.ellipse(1.5 * T, 2.85 * T, 1.4 * T, 0.25 * T).fill({ color: 0x3a2a1a, alpha: 0.35 })
        g.poly([0.2 * T, 2.9 * T, 2.2 * T, 0.6 * T, 2.8 * T, 0.9 * T, 2.8 * T, 2.9 * T]).fill({ color: reed })
        for (let line = 0; line < 6; line++) {
          const t = line / 6
          g.moveTo((0.3 + t * 2) * T, (2.85 - t * 2.2) * T).lineTo((0.3 + t * 2 + 0.5) * T, 2.9 * T).stroke({ color: 0x9c8a3a, width: 1 })
        }
        g.moveTo(0.1 * T, 2.95 * T).lineTo(2.3 * T, 0.45 * T).stroke({ color: pole, width: 3 })
        g.moveTo(2.8 * T, 2.95 * T).lineTo(2.8 * T, 0.8 * T).stroke({ color: pole, width: 3 })
        break
      case 'WoodPile':
        for (let row = 0; row < 3; row++) {
          for (let column = 0; column < 3 - row; column++) {
            view.addChild(this.localSprite(town(106), 0.1 + column * 0.95 + row * 0.47, 1.9 - row * 0.55))
          }
        }
        break
      case 'StonePile':
        for (const [px, py] of [[0.2, 1.9], [1, 2.05], [1.8, 1.9], [0.6, 1.4], [1.4, 1.4], [1, 0.9], [2.1, 1.35]]) {
          view.addChild(this.localSprite(RESOURCE_ICONS.Stone, px, py))
        }
        break
      case 'Stage':
        for (let column = 0; column < 3; column++) view.addChild(this.localSprite(town(73), column, 1.9))
        for (const px of [0, 2]) {
          view.addChild(this.localSprite(town(47), px, 1))
          view.addChild(this.localSprite(battle(70), px, 0.15))
        }
        break
      case 'DryingRack':
        g.moveTo(0.4 * T, 2.9 * T).lineTo(0.4 * T, 1 * T).stroke({ color: pole, width: 3 })
        g.moveTo(2.6 * T, 2.9 * T).lineTo(2.6 * T, 1 * T).stroke({ color: pole, width: 3 })
        g.moveTo(0.2 * T, 1.1 * T).lineTo(2.8 * T, 1.1 * T).stroke({ color: pole, width: 2 })
        g.poly([0.3 * T, 0.95 * T, 1.5 * T, 0.5 * T, 2.7 * T, 0.95 * T]).fill({ color: reed })
        for (const [hx, color] of [[0.8, 0xa0522d], [1.3, 0xd2b48c], [1.8, 0xa0522d], [2.2, 0xc04040]] as const) {
          g.rect(hx * T - 3, 1.15 * T, 6, 0.7 * T).fill({ color })
        }
        break
      default:
        view.addChild(this.localSprite(town(83), 1, 1.8))
    }
    view.addChildAt(g, 0)
    return view
  }

  private localSprite(tile: TileRef, x: number, y: number): Sprite {
    const sprite = new Sprite(this.tiles.get(tile))
    sprite.position.set(x * TILE, y * TILE)
    return sprite
  }

  /** Construction site: a faint blueprint, the building rising from the ground, corner pegs and the delivered material. */
  private siteView(site: Site, x: number, y: number): Container {
    const view = new Container()
    view.position.set(x * TILE, y * TILE)
    view.zIndex = (y + 3) * TILE

    view.addChild(new Graphics().rect(0, 0, PLOT_WIDTH * TILE, 3 * TILE).fill({ color: 0x7a5a32, alpha: 0.35 }))
    const ghost = this.finishedView(site.kind, 0, 0)
    ghost.alpha = 0.22
    view.addChild(ghost)

    const fraction = Math.min(1, site.progress)
    if (fraction > 0) {
      const rising = this.finishedView(site.kind, 0, 0)
      const height = 3 * TILE * fraction
      const mask = new Graphics().rect(-TILE, 3 * TILE - height, (PLOT_WIDTH + 2) * TILE, height + 2).fill({ color: 0xffffff })
      rising.mask = mask
      view.addChild(rising, mask)
    }

    for (const [px, py] of [[-0.35, -0.3], [2.35, -0.3], [-0.35, 2.3], [2.35, 2.3]]) {
      view.addChild(this.localSprite(town(47), px, py))
    }

    // Material waiting in the yard for the current stage.
    const stage = site.stages[site.stage]
    const definition = this.buildingDefinitions.get(site.kind)
    const stageDefinition = definition?.stages[site.stage]
    let slot = 0
    for (const job of stage?.jobs ?? []) {
      const jobDefinition = stageDefinition?.jobs.find((j) => j.jobId === job.jobId)
      if (!jobDefinition || job.required <= 0) continue
      const icon = this.haulIcon(job.jobId)
      if (!icon) continue
      const count = Math.min(4, Math.ceil((job.done / job.required) * 4))
      for (let item = 0; item < count; item++) {
        const sprite = this.localSprite(icon, 0.1 + slot * 0.95 + (item % 2) * 0.35, 3.05 - Math.floor(item / 2) * 0.3)
        sprite.scale.set(0.6)
        view.addChild(sprite)
      }
      slot++
    }
    return view
  }

  private haulIcon(jobId: string): TileRef | null {
    const icons: Record<string, string> = {
      'bring-sticks': 'Wood',
      'bring-logs': 'Wood',
      'bring-stones': 'Stone',
      'bring-reeds': 'Reeds',
      'bring-hides': 'Leather',
    }
    const resource = icons[jobId]
    return resource ? RESOURCE_ICONS[resource] : null
  }

  /** Goods yard: one pallet per slot, with up to four stacked goods showing how full the slot is. */
  private goodsYard(storage: Storage, definition: StorageDefinition, x: number, y: number): Container {
    const yard = new Container()
    const spacing = PLOT_WIDTH / definition.slots
    const floorY = (y + 3.85) * TILE
    for (let index = 0; index < definition.slots; index++) {
      const centerX = (x + spacing * (index + 0.5)) * TILE
      yard.addChild(new Graphics().rect(centerX - spacing * TILE * 0.4, floorY - 2, spacing * TILE * 0.8, 3).fill({ color: 0x7a5230 }))
      const slot = storage.slots[index]
      const icon = slot ? RESOURCE_ICONS[slot.kind] : undefined
      if (!slot || !icon) continue
      const count = Math.max(1, Math.ceil(slot.fill * 4))
      for (let item = 0; item < count; item++) {
        const sprite = new Sprite(this.tiles.get(icon))
        sprite.anchor.set(0.5, 1)
        sprite.scale.set(Math.min(0.6, (spacing * TILE * 0.9) / TILE))
        sprite.position.set(centerX, floorY - 1 - item * 4)
        yard.addChild(sprite)
      }
    }
    yard.zIndex = floorY
    return yard
  }

  setVillage(state: VillageState) {
    if (this.terrainSeed === null) {
      this.terrainSeed = seedFrom(state.id)
      this.buildTerrain(this.terrainSeed)
      this.showCamp()
    }
    this.village = state
    this.drawTrees(state.trees)
    this.drawBuildings()
    this.wildlife.setHerds(state.wildlife)
    this.activeProjectId = state.project?.definitionId ?? null
    this.setCitizens(this.citizens, this.highlightId)

    const project = state.project
    const key = JSON.stringify([
      state.completedProjects,
      project?.definitionId,
      project?.phase,
      project ? Math.round(project.surveyDone) : 0,
      project ? Math.floor((project.buildDone / Math.max(project.buildRequired, 1)) * 16) : 0,
      project?.materials.map((m) => Math.floor((m.delivered / m.required) * 4)),
      Math.ceil((state.goldVeinRemaining / VEIN_OUTCROP.size) * VEIN_NUGGETS.length),
    ])
    if (key === this.lastProjectKey) return
    this.lastProjectKey = key

    this.projectLayer.removeChildren().forEach((child) => child.destroy({ children: true }))
    const add = (child: Container) => this.projectLayer.addChild(child)
    const completed = new Set(state.completedProjects)

    // Tsar bridge.
    const bridgeDone = completed.has('tsar-bridge')
    const bridgeActive = project?.definitionId === 'tsar-bridge'
    let builtColumns = 0
    if (bridgeDone) {
      builtColumns = RIVER_RIGHT - RIVER_LEFT + 1
    } else if (bridgeActive && project.phase === 'Construction') {
      builtColumns = Math.floor((project.buildDone / project.buildRequired) * (RIVER_RIGHT - RIVER_LEFT + 1))
    }
    for (let column = 0; column <= RIVER_RIGHT - RIVER_LEFT; column++) {
      const x = RIVER_LEFT + column
      if (column < builtColumns) {
        for (const y of [ROAD_TOP, ROAD_TOP + 1]) {
          const deck = this.tileSprite(town(73), x, y)
          deck.zIndex = 0
          add(deck)
        }
        add(this.tileSprite(town(column === 0 ? 80 : column === RIVER_RIGHT - RIVER_LEFT ? 82 : 81), x, ROAD_TOP - 0.55))
        add(this.tileSprite(town(column === 0 ? 80 : column === RIVER_RIGHT - RIVER_LEFT ? 82 : 81), x, ROAD_TOP + 1.5))
      } else if (bridgeActive) {
        const surveyed = project.surveyDone / Math.max(project.surveyRequired, 1)
        const showPost = project.phase === 'Construction' || column < Math.ceil(surveyed * (RIVER_RIGHT - RIVER_LEFT + 1))
        if (showPost) {
          add(this.tileSprite(town(47), x, ROAD_TOP - 0.3))
          add(this.tileSprite(town(47), x, ROAD_TOP + 1.2))
        }
      }
    }
    if (bridgeActive) {
      add(this.tileSprite(town(83), RIVER_LEFT - 1, ROAD_TOP - 1.2))
    }

    // Tsar's monument: pegs, then the pedestal, then the statue rising on it.
    const monumentActive = project?.definitionId === MONUMENT.id
    if (completed.has(MONUMENT.id) || monumentActive) {
      const fraction = completed.has(MONUMENT.id) ? 1 : project!.buildDone / Math.max(project!.buildRequired, 1)
      const base = MONUMENT.y + 1
      if (fraction > 0) add(this.tileSprite(MONUMENT.pedestal, MONUMENT.x, base))
      if (fraction >= 1) {
        const statue = this.tileSprite(MONUMENT.statue, MONUMENT.x, MONUMENT.y + 0.15)
        const stone = new ColorMatrixFilter()
        stone.desaturate()
        statue.filters = [stone]
        statue.tint = MONUMENT.stone
        add(statue)
      } else if (fraction >= 0.5) {
        add(this.tileSprite(MONUMENT.rough, MONUMENT.x, MONUMENT.y + 0.2))
      }
      if (monumentActive) {
        for (const dx of [-0.7, 0.7]) add(this.tileSprite(town(47), MONUMENT.x + dx, base + 0.4))
        add(this.tileSprite(town(83), MONUMENT.x + 1.4, base))
      }
    }

    // Fishing pier: poles first, then planks laid from the bank outwards.
    const pierActive = project?.definitionId === PIER.id
    if (completed.has(PIER.id) || pierActive) {
      const fraction = completed.has(PIER.id) ? 1 : project!.buildDone / Math.max(project!.buildRequired, 1)
      const planks = Math.floor(fraction * PIER.length + 1e-6)
      for (let step = 0; step < PIER.length; step++) {
        const x = PIER.x + step
        if (step < planks) {
          const deck = this.tileSprite(town(73), x, PIER.y)
          deck.zIndex = 0
          add(deck)
        } else if (pierActive) {
          add(this.tileSprite(town(47), x, PIER.y - 0.2))
        }
      }
      if (pierActive) add(this.tileSprite(town(83), PIER.x - 1, PIER.y - 1))
    }

    // Palisade: stakes go up piece by piece; pegs every few tiles mark the line still to build.
    const palisadeActive = project?.definitionId === PALISADE.id
    if (completed.has(PALISADE.id) || palisadeActive) {
      const fraction = completed.has(PALISADE.id) ? 1 : project!.buildDone / Math.max(project!.buildRequired, 1)
      const standing = Math.floor(fraction * PALISADE.pieces.length + 1e-6)
      PALISADE.pieces.forEach((piece, index) => {
        if (index < standing) add(this.tileSprite(piece.tile, piece.x, piece.y))
        else if (palisadeActive && index % 4 === 0) add(this.tileSprite(town(47), piece.x, piece.y))
      })
      if (palisadeActive) add(this.tileSprite(town(83), PALISADE.yard.x - 1, PALISADE.yard.y - 1))
    }

    // Gold vein: nuggets in the outcrop thin out as it is dug; an empty vein is bare grey rock.
    const nuggets = Math.ceil((state.goldVeinRemaining / VEIN_OUTCROP.size) * VEIN_NUGGETS.length)
    VEIN_NUGGETS.forEach(([dx, dy], index) => {
      const nugget = this.tileSprite(RESOURCE_ICONS.Gold, VEIN_OUTCROP.x + dx, VEIN_OUTCROP.y + dy)
      nugget.scale.set(0.6)
      if (index >= nuggets) {
        const rock = new ColorMatrixFilter()
        rock.desaturate()
        nugget.filters = [rock]
        nugget.tint = 0xc4c8cf
      }
      add(nugget)
    })

    // Other projects: finished buildings, or rising walls on their site.
    for (const [id, site] of Object.entries(PROJECT_SITES)) {
      const tiles = PROJECT_BUILDINGS[id]
      const dome = id === 'bell-tower' ? 'gold' : undefined
      if (completed.has(id)) {
        add(this.buildingView(tiles, site.x, site.y, dome))
      } else if (project?.definitionId === id) {
        const fraction = project.phase === 'Construction' ? project.buildDone / project.buildRequired : 0
        const rows = Math.floor(fraction * tiles.length)
        const partial = tiles.slice(tiles.length - rows)
        if (partial.length > 0) add(this.buildingView(partial, site.x, site.y + tiles.length - rows))
        const width = tiles[0].length
        for (const [dx, dy] of [[-0.5, -0.5], [width - 0.5, -0.5], [-0.5, tiles.length - 0.5], [width - 0.5, tiles.length - 0.5]]) {
          add(this.tileSprite(town(47), site.x + dx, site.y + dy))
        }
        add(this.tileSprite(town(83), site.x - 1, site.y + tiles.length - 1))
      }
    }

    // Material piles in the yard next to the active project.
    if (project) {
      const yard =
        project.definitionId === 'tsar-bridge'
          ? { x: 26.5, y: 9.4 }
          : project.definitionId === MONUMENT.id
            ? { x: MONUMENT.x, y: MONUMENT.y }
            : project.definitionId === PIER.id
              ? { x: PIER.x, y: PIER.y }
              : project.definitionId === PALISADE.id
                ? PALISADE.yard
                : PROJECT_SITES[project.definitionId]
      project.materials.forEach((material, index) => {
        const count = Math.min(4, Math.ceil((material.delivered / material.required) * 4))
        const tile = RESOURCE_ICONS[material.kind] ?? town(92)
        const baseX = project.definitionId === 'tsar-bridge' ? yard.x + (index % 2) * 1.4 : yard.x - 2.5 + (index % 2) * 1.2
        const baseY = project.definitionId === 'tsar-bridge' ? yard.y + Math.floor(index / 2) * 1.2 : yard.y + 1 + Math.floor(index / 2) * 1.2
        for (let item = 0; item < count; item++) {
          const sprite = this.tileSprite(tile, baseX + (item % 2) * 0.45, baseY - Math.floor(item / 2) * 0.35)
          sprite.scale.set(0.75)
          add(sprite)
        }
      })
    }
  }

  /** Shows a speech bubble above a citizen (or the elder). */
  say(ownerId: string, text: string) {
    const owner = this.walkers.get(ownerId)
    if (!owner) return
    for (const bubble of this.bubbles.filter((b) => b.ownerId === ownerId)) {
      bubble.expiresAt = 0
    }
    const label = new Text({
      text,
      style: { fontFamily: LABEL_FONT, fontSize: 13, fill: 0x2b1d0e, fontWeight: '600', wordWrap: true, wordWrapWidth: 190 },
    })
    const padding = 6
    const width = label.width + padding * 2
    const height = label.height + padding * 2
    const background = new Graphics()
      .roundRect(-width / 2, -height - 6, width, height, 6)
      .fill({ color: 0xfff8e7 })
      .stroke({ color: 0x3a2a1a, width: 2 })
      .poly([-5, -7, 5, -7, 0, 0])
      .fill({ color: 0xfff8e7 })
    label.position.set(-width / 2 + padding, -height - 6 + padding)
    const view = new Container()
    view.addChild(background, label)
    this.screen.addChild(view)
    this.bubbles.push({ ownerId, view, expiresAt: this.elapsed + BUBBLE_SECONDS })
  }

  /** Citizens from before character creation keep a sprite picked from their id. */
  private citizenTexture(citizen: CitizenSummary): Texture {
    if (!citizen.appearance) return this.tiles.get(dungeon(VILLAGER_SPRITES[hash(citizen.id) % VILLAGER_SPRITES.length]))
    const key = appearanceKey(citizen.appearance)
    let texture = this.lookTextures.get(key)
    if (!texture) {
      texture = new Texture({ source: new CanvasSource({ resource: this.painter.paint(citizen.appearance), scaleMode: 'nearest' }) })
      this.lookTextures.set(key, texture)
    }
    return texture
  }

  private addWalker(id: string, name: string, texture: Texture, x: number, y: number): Walker {
    const sprite = new Sprite(texture)
    sprite.anchor.set(0.5, 1)
    const icon = new Container()
    const label = new Text({
      text: name,
      style: {
        fontFamily: LABEL_FONT,
        fontSize: 12,
        fontWeight: '700',
        fill: 0xffffff,
        stroke: { color: 0x1b1206, width: 3 },
      },
    })
    label.anchor.set(0.5, 1)
    const subtitle = new Text({
      text: '',
      style: {
        fontFamily: LABEL_FONT,
        fontSize: 10,
        fontStyle: 'italic',
        fontWeight: '600',
        fill: 0xffe9b0,
        stroke: { color: 0x1b1206, width: 3 },
      },
    })
    subtitle.anchor.set(0.5, 1)
    subtitle.visible = false
    sprite.zIndex = y + 0.5
    icon.zIndex = 100000
    this.objects.addChild(sprite, icon)
    this.screen.addChild(label, subtitle)
    const seed = hash(id)
    const walker: Walker = {
      id,
      sprite,
      icon,
      label,
      subtitle,
      x,
      y,
      targetX: x,
      targetY: y,
      working: false,
      roaming: false,
      staying: false,
      wanderAt: 0,
      phase: pseudo(seed, 9) * Math.PI * 2,
      seed,
      hunt: null,
    }
    this.walkers.set(id, walker)
    return walker
  }

  private tileSprite(tile: TileRef, x: number, y: number): Sprite {
    const sprite = new Sprite(this.tiles.get(tile))
    sprite.position.set(x * TILE, y * TILE)
    sprite.zIndex = (y + 1) * TILE
    return sprite
  }

  private decorView(placed: PlacedTile): Container {
    const sprite = this.tileSprite(placed.tile, placed.x, placed.y)
    if (!placed.top) return sprite
    const view = new Container()
    view.zIndex = sprite.zIndex
    view.addChild(this.tileSprite(placed.top, placed.x, placed.y - 1), sprite)
    return view
  }

  private bushView(x: number, y: number): Graphics {
    const g = new Graphics()
      .ellipse(0, 0, 6, 4.5)
      .fill({ color: 0x2f6b2a })
      .ellipse(-2, -1.5, 3.5, 2.5)
      .fill({ color: 0x3f8a36 })
    for (const [bx, by] of [[-3, 0], [2, -1], [0, 2], [4, 1.5]]) {
      g.circle(bx, by, 1.2).fill({ color: 0xc8283c })
    }
    g.position.set(x * TILE, y * TILE)
    g.zIndex = y * TILE + 4
    return g
  }

  private reedView(x: number, y: number): Graphics {
    const g = new Graphics()
    for (const [dx, height] of [[-3, 10], [0, 13], [3, 9], [5, 11]]) {
      g.moveTo(dx, 0).lineTo(dx + 0.5, -height).stroke({ color: 0x6b8f3a, width: 1.2 })
      g.rect(dx - 0.5, -height - 1, 2, 4).fill({ color: 0x7a4a24 })
    }
    g.position.set(x * TILE, y * TILE)
    g.zIndex = y * TILE
    return g
  }

  private campfireView(): Container {
    const view = new Container()
    const stones = new Graphics()
    for (let index = 0; index < 8; index++) {
      const angle = (index / 8) * Math.PI * 2
      stones.ellipse(Math.cos(angle) * 8, Math.sin(angle) * 4.5, 2.6, 2).fill({ color: index % 2 ? 0x8d8d8d : 0x6f6f6f })
    }
    const logs = new Graphics()
      .moveTo(-6, 2)
      .lineTo(6, -2)
      .stroke({ color: 0x5a3a1c, width: 3 })
      .moveTo(-6, -2)
      .lineTo(6, 2)
      .stroke({ color: 0x6b4423, width: 3 })
    view.addChild(stones, logs, this.flames)
    view.position.set(CAMPFIRE.x * TILE, CAMPFIRE.y * TILE)
    view.zIndex = CAMPFIRE.y * TILE + 5
    return view
  }

  private drawFlames() {
    const flicker = (salt: number) => Math.sin(this.elapsed * (7 + salt) + salt * 1.7) * 1.5
    this.flames
      .clear()
      .poly([-5, 0, -1 + flicker(1), -11 + flicker(2), 5, 0])
      .fill({ color: 0xe8541e })
      .poly([-3, 0, 1 + flicker(3), -8 + flicker(4), 3, 0])
      .fill({ color: 0xffa62b })
      .poly([-1.5, 0, flicker(5) * 0.5, -4.5, 1.5, 0])
      .fill({ color: 0xfff1a8 })
  }

  private buildingView(tiles: BuildingCell[][], x: number, y: number, dome?: 'gold' | 'blue'): Container {
    const view = new Container()
    tiles.forEach((row, rowIndex) =>
      row.forEach((cell, column) => {
        for (const tile of Array.isArray(cell) ? cell : [cell]) {
          const sprite = new Sprite(this.tiles.get(tile))
          sprite.position.set(column * TILE, rowIndex * TILE)
          view.addChild(sprite)
        }
      }),
    )
    if (dome) {
      view.addChild(this.domeView(dome, (tiles[0].length * TILE) / 2))
    }
    view.position.set(x * TILE, y * TILE)
    view.zIndex = (y + tiles.length) * TILE
    return view
  }

  /** Onion dome with a cross, drawn as 2-pixel blocks so it matches the 16-pixel tiles. */
  private domeView(kind: 'gold' | 'blue', centerX: number): Graphics {
    const shape = [
      '...c...',
      '..ccc..',
      '...c...',
      '...o...',
      '..ooo..',
      '.ooooo.',
      'ooohooo',
      'ooohooo',
      '.ooooo.',
      '..ddd..',
      '..dwd..',
      '..ddd..',
    ]
    const colors: Record<string, number> = {
      c: 0xf4d35e,
      o: kind === 'gold' ? 0xe9b949 : 0x3d6fb6,
      h: kind === 'gold' ? 0xfff1a8 : 0x8fb8f0,
      d: 0xd9d4c7,
      w: 0x3a2a1a,
    }
    const block = 3
    const graphics = new Graphics()
    shape.forEach((line, row) =>
      [...line].forEach((cell, column) => {
        if (cell === '.') return
        graphics.rect(centerX + (column - 3.5) * block, (row - shape.length) * block + 3, block, block).fill({ color: colors[cell] })
      }),
    )
    return graphics
  }

  /** The furthest zoom still fills the screen with map, so nothing beyond its edges is ever shown. */
  private scaleLimits() {
    const { width, height } = this.app.screen
    const minimum = Math.max(width / WORLD_WIDTH, height / WORLD_HEIGHT)
    return { minimum, maximum: Math.max(minimum, MAXIMUM_SCALE) }
  }

  /** About 34 tiles across a wide screen; on narrow phones tiles stay at least 28 pixels so citizens remain readable. */
  private defaultScale() {
    return Math.max(this.app.screen.width / (DEFAULT_VIEW_TILES * TILE), MINIMUM_DEFAULT_TILE_PIXELS / TILE)
  }

  private layoutCamera() {
    const { width, height } = this.app.screen
    const limits = this.scaleLimits()
    this.scale = Math.min(limits.maximum, Math.max(limits.minimum, this.scale))
    this.world.scale.set(this.scale)
    const overflowX = Math.max(0, WORLD_WIDTH * this.scale - width)
    const overflowY = Math.max(0, WORLD_HEIGHT * this.scale - height)
    this.panX = Math.min(1, Math.max(0, this.panX))
    this.panY = Math.min(1, Math.max(0, this.panY))
    this.world.position.set(
      Math.round(-MAP_LEFT * TILE * this.scale - overflowX * this.panX),
      Math.round(-MAP_TOP * TILE * this.scale - overflowY * this.panY),
    )
    const view = this.visibleArea()
    this.viewCenter = { x: view.x + view.width / 2, y: view.y + view.height / 2 }
    this.minimap?.setView(view)
    this.cullDirty = true
  }

  private showCamp() {
    this.scale = this.defaultScale()
    // Slightly east of the fire, so the plots and the river share the view with the camp.
    const visibleTiles = this.app.screen.width / this.scale / TILE
    this.centerOn(CAMPFIRE.x + visibleTiles * 0.12, CAMPFIRE.y - 1)
  }

  /** Moves the view so that world pixel (`worldX`, `worldY`) appears at screen point (`screenX`, `screenY`). */
  private placeAt(worldX: number, worldY: number, screenX: number, screenY: number) {
    const { width, height } = this.app.screen
    const overflowX = WORLD_WIDTH * this.scale - width
    const overflowY = WORLD_HEIGHT * this.scale - height
    if (overflowX > 0) this.panX = ((worldX - MAP_LEFT * TILE) * this.scale - screenX) / overflowX
    if (overflowY > 0) this.panY = ((worldY - MAP_TOP * TILE) * this.scale - screenY) / overflowY
    this.layoutCamera()
  }

  /** Centres the view on a tile, as far as the map edges allow. */
  centerOn(tileX: number, tileY: number) {
    const { width, height } = this.app.screen
    this.placeAt(tileX * TILE, tileY * TILE, width / 2, height / 2)
  }

  /** Visible part of the map in tiles. */
  private visibleArea() {
    const topLeft = this.world.toLocal({ x: 0, y: 0 })
    const { width, height } = this.app.screen
    return { x: topLeft.x / TILE, y: topLeft.y / TILE, width: width / this.scale / TILE, height: height / this.scale / TILE }
  }

  /** Zooms by `factor`, keeping the map point under the given screen point in place (the screen centre by default). */
  zoomBy(factor: number, screenX?: number, screenY?: number) {
    const { width, height } = this.app.screen
    const anchorX = screenX ?? width / 2
    const anchorY = screenY ?? height / 2
    const anchor = this.world.toLocal({ x: anchorX, y: anchorY })
    this.cameraMoved = true
    const limits = this.scaleLimits()
    this.scale = Math.min(limits.maximum, Math.max(limits.minimum, this.scale * factor))
    this.placeAt(anchor.x, anchor.y, anchorX, anchorY)
  }

  private bindPointer() {
    const canvas = this.app.canvas
    canvas.style.touchAction = 'none'
    const pointers = new Map<number, { x: number; y: number }>()
    let pinchDistance = 0
    const local = (event: PointerEvent) => {
      const bounds = canvas.getBoundingClientRect()
      return { x: event.clientX - bounds.left, y: event.clientY - bounds.top }
    }
    const pinch = () => {
      const [first, second] = [...pointers.values()]
      return {
        distance: Math.hypot(first.x - second.x, first.y - second.y),
        x: (first.x + second.x) / 2,
        y: (first.y + second.y) / 2,
      }
    }

    canvas.addEventListener('pointerdown', (event) => {
      pointers.set(event.pointerId, local(event))
      canvas.setPointerCapture(event.pointerId)
      if (pointers.size === 2) {
        // Two fingers: pinch zoom takes over, and the gesture never counts as a click on a plot.
        pinchDistance = pinch().distance
        if (this.dragging) this.dragging.moved = true
        return
      }
      this.dragging = { x: event.clientX, y: event.clientY, panX: this.panX, panY: this.panY, moved: false }
    })
    canvas.addEventListener('pointermove', (event) => {
      if (!pointers.has(event.pointerId)) return
      pointers.set(event.pointerId, local(event))
      if (pointers.size < 2) return
      const current = pinch()
      if (pinchDistance > 0) this.zoomBy(current.distance / pinchDistance, current.x, current.y)
      pinchDistance = current.distance
    })
    const release = (event: PointerEvent) => {
      pointers.delete(event.pointerId)
      pinchDistance = 0
      if (pointers.size === 1 && this.dragging) {
        // One finger stays down after a pinch: keep panning from where it is now.
        const [rest] = [...pointers.values()]
        const bounds = canvas.getBoundingClientRect()
        this.dragging = { x: rest.x + bounds.left, y: rest.y + bounds.top, panX: this.panX, panY: this.panY, moved: true }
      }
    }
    canvas.addEventListener('pointerup', release)
    canvas.addEventListener('pointercancel', release)

    const placementUnder = (event: PointerEvent) => {
      const point = this.world.toLocal(local(event))
      return this.placementAt(point.x, point.y)
    }
    canvas.addEventListener('pointerup', (event) => {
      if (!this.dragging || this.dragging.moved) return
      if (!this.onPick) {
        const screen = local(event)
        const citizenId = this.onCitizenPick ? this.citizenAt(screen) : null
        if (citizenId) {
          this.onCitizenPick?.(citizenId)
          return
        }
        const point = this.world.toLocal(screen)
        this.onWorldPick?.(this.targetAt(point.x, point.y), screen, { x: point.x / TILE, y: point.y / TILE })
        return
      }
      const { x, y, free } = placementUnder(event)
      // Touch has no hover: a tap on taken ground shows the red outline there instead.
      if (free) this.onPick(x, y)
      else this.showGhost(x, y, free)
    })
    canvas.addEventListener('pointermove', (event) => {
      if (this.onPick || event.pointerType === 'touch' || this.dragging?.moved) return
      if (!this.onCitizenPick && !this.onWorldPick) return
      const screen = local(event)
      const point = this.world.toLocal(screen)
      const pickable =
        (this.onCitizenPick !== null && this.citizenAt(screen) !== null) ||
        (this.onWorldPick !== null && this.targetAt(point.x, point.y) !== null)
      canvas.style.cursor = pickable ? 'pointer' : ''
    })
    canvas.addEventListener('pointermove', (event) => {
      if (!this.onPick || pointers.size > 1 || event.pointerType === 'touch') return
      const { x, y, free } = placementUnder(event)
      this.showGhost(x, y, free)
    })
    canvas.addEventListener('pointermove', (event) => {
      if (!this.dragging || pointers.size > 1) return
      if (Math.hypot(event.clientX - this.dragging.x, event.clientY - this.dragging.y) > 6) this.dragging.moved = true
      if (this.dragging.moved) this.cameraMoved = true
      const { width, height } = this.app.screen
      const scale = this.world.scale.x
      const overflowX = Math.max(1, WORLD_WIDTH * scale - width)
      const overflowY = Math.max(1, WORLD_HEIGHT * scale - height)
      this.panX = this.dragging.panX - (event.clientX - this.dragging.x) / overflowX
      this.panY = this.dragging.panY - (event.clientY - this.dragging.y) / overflowY
      this.layoutCamera()
    })
    const stop = () => {
      if (pointers.size === 0) this.dragging = null
    }
    canvas.addEventListener('pointerup', stop)
    canvas.addEventListener('pointercancel', stop)
    canvas.addEventListener(
      'wheel',
      (event) => {
        event.preventDefault()
        const bounds = canvas.getBoundingClientRect()
        // Trackpads send many small deltas; mouse wheels send large ones. Both map to a smooth factor.
        const factor = Math.exp(-Math.max(-100, Math.min(100, event.deltaY)) * 0.0015)
        this.zoomBy(factor, event.clientX - bounds.left, event.clientY - bounds.top)
      },
      { passive: false },
    )
  }

  private update(ticker: Ticker) {
    const seconds = Math.min(ticker.deltaMS / 1000, 0.1)
    this.elapsed += seconds

    this.drawFlames()
    this.placementLayer.alpha = 0.65 + Math.sin(this.elapsed * 4) * 0.35
    this.nightAmount += (this.nightTarget - this.nightAmount) * Math.min(1, seconds * 2)
    this.night.alpha = this.nightAmount * 0.55
    this.lights.alpha = this.nightAmount * (0.9 + Math.sin(this.elapsed * 9) * 0.1)

    const now = Date.now()
    const hunters = new Set<string>()
    const sortDepth = this.elapsed - this.depthSortedAt >= DEPTH_SORT_SECONDS
    if (sortDepth) this.depthSortedAt = this.elapsed
    for (const walker of this.walkers.values()) {
      let speed = WALK_SPEED
      walker.icon.visible = true
      if (walker.hunt) {
        hunters.add(walker.id)
        const secondsLeft = (walker.hunt.catchAt - now) / 1000
        const chase = this.wildlife.chase(walker.id, walker.hunt.prey, walker.x, walker.y, secondsLeft)
        // The prey held over the head takes the place of the task bubble.
        walker.icon.visible = !chase.caught
        if (chase.caught) {
          const home = this.storageYardFor('Food') ?? WORKPLACES.camp
          walker.targetX = (home.x + (pseudo(walker.seed, 7) - 0.5) * home.spread * 2) * TILE
          walker.targetY = (home.y + (pseudo(walker.seed, 8) - 0.5) * 0.6) * TILE
        } else {
          const gap = Math.hypot(chase.x - walker.x, chase.y - walker.y)
          // Right behind the prey: wait for it to bolt again, and pounce only when the chase is about to end.
          const pounce = secondsLeft <= HUNTER_POUNCE_SECONDS
          walker.targetX = gap < HUNTER_HOLD_DISTANCE && !pounce ? walker.x : chase.x
          walker.targetY = gap < HUNTER_HOLD_DISTANCE && !pounce ? walker.y : chase.y
          speed = Math.max(RUN_SPEED, gap / Math.max(secondsLeft, seconds))
        }
      } else if (walker.roaming && this.elapsed >= walker.wanderAt) {
        const spot = ROAMING_SPOTS[Math.floor(Math.random() * ROAMING_SPOTS.length)]
        walker.targetX = (spot.x + (Math.random() - 0.5) * (spot.spread + 2) * 2) * TILE
        walker.targetY = (spot.y + (Math.random() - 0.5) * (spot.spread + 2) * 2) * TILE
        walker.wanderAt = this.elapsed + 8 + Math.random() * 6
      } else if (!walker.working && !walker.staying && this.elapsed >= walker.wanderAt) {
        const home = walker.id === PRIEST_ID ? { x: PRIEST_SPOT.x, y: PRIEST_SPOT.y, spread: 1.5 } : WORKPLACES.camp
        walker.targetX = (home.x + (Math.random() - 0.5) * home.spread * 2) * TILE
        walker.targetY = (home.y + (Math.random() - 0.5) * (walker.id === PRIEST_ID ? 0.6 : 1.2)) * TILE
        walker.wanderAt = this.elapsed + 4 + Math.random() * 6
      }

      const dx = walker.targetX - walker.x
      const dy = walker.targetY - walker.y
      const distance = Math.hypot(dx, dy)
      const moving = distance > 0.5
      if (moving) {
        const step = Math.min(distance, speed * seconds)
        walker.x += (dx / distance) * step
        walker.y += (dy / distance) * step
        if (Math.abs(dx) > 0.5) walker.sprite.scale.x = dx < 0 ? -1 : 1
      }

      walker.phase += seconds * (moving ? 12 : walker.working ? 6 : 1.5)
      const bob = moving ? Math.abs(Math.sin(walker.phase)) * -1.5 : walker.working ? Math.max(0, Math.sin(walker.phase)) * -1.5 : 0
      walker.sprite.position.set(Math.round(walker.x), Math.round(walker.y + bob))
      if (sortDepth) walker.sprite.zIndex = walker.y + 0.5
      walker.icon.position.set(Math.round(walker.x), Math.round(walker.y - 22))

      const head = this.world.toGlobal({ x: walker.x, y: walker.y - (walker.icon.children.length > 0 ? 29 : 16) })
      const subtitleHeight = walker.subtitle.visible ? walker.subtitle.height - 2 : 0
      walker.subtitle.position.set(Math.round(head.x), Math.round(head.y))
      walker.label.position.set(Math.round(head.x), Math.round(head.y - subtitleHeight))
      walker.label.alpha = walker.id === this.highlightId || walker.id === PRIEST_ID ? 1 : 0.9
      walker.subtitle.alpha = walker.label.alpha
    }
    this.wildlife.update(seconds, this.elapsed, [...this.walkers.values()], hunters, sortDepth)

    for (const label of this.buildingLabels) {
      const top = this.world.toGlobal({ x: label.x, y: label.y })
      label.text.position.set(Math.round(top.x), Math.round(top.y))
    }

    for (let index = this.bubbles.length - 1; index >= 0; index--) {
      const bubble = this.bubbles[index]
      const owner = this.walkers.get(bubble.ownerId)
      if (!owner || this.elapsed >= bubble.expiresAt) {
        bubble.view.destroy({ children: true })
        this.bubbles.splice(index, 1)
        continue
      }
      const head = this.world.toGlobal({ x: owner.x, y: owner.y - 32 })
      bubble.view.position.set(Math.round(head.x), Math.round(head.y - 14))
      bubble.view.alpha = Math.min(1, (bubble.expiresAt - this.elapsed) * 2)
    }

    if (this.minimap && this.elapsed - this.minimapUpdatedAt >= 0.25) {
      this.minimapUpdatedAt = this.elapsed
      this.minimap.setMarkers(this.minimapMarkers())
    }

    if (this.cullDirty) {
      this.cullDirty = false
      Culler.shared.cull(this.objects, this.app.screen, false)
    }
  }

  private minimapMarkers(): MinimapMarkers {
    const church = MONASTERY.church
    const buildings: MinimapMarkers['buildings'] = [
      { x: church.x, y: church.y, width: 3, height: 4, finished: true },
      ...MONASTERY.cells.map((cell) => ({ x: cell.x, y: cell.y, width: 3, height: 3, finished: true })),
    ]
    const village = this.village
    if (village) {
      const finished = [...village.structures, ...village.storages].map((building) => building.plotId)
      for (const [plotIds, isFinished] of [[finished, true], [village.sites.map((site) => site.plotId), false]] as const) {
        for (const plotId of plotIds) {
          const plot = this.plot(plotId)
          if (plot) buildings.push({ x: plot.x, y: plot.y, width: PLOT_WIDTH, height: PLOT_HEIGHT, finished: isFinished })
        }
      }
    }
    const citizens = [...this.walkers.values()]
      .filter((walker) => walker.id !== PRIEST_ID)
      .map((walker) => ({ x: walker.x / TILE, y: walker.y / TILE, highlighted: walker.id === this.highlightId }))
    return { buildings, citizens, camp: CAMPFIRE }
  }
}
