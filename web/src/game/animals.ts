import { Container, Graphics, GraphicsContext, Sprite } from 'pixi.js'
import type { AnimalKind, Herd } from '../api'
import { TILE, art, type AnimationName, type ArtName, type TileSet } from './sheets'
import { CAMPFIRE, MAP_HEIGHT, MAP_LEFT, MAP_TOP, MAP_WIDTH, MONASTERY, Terrain } from './worldMap'

interface AnimalLook {
  /** Animals drawn for a full herd; a smaller herd shows its share of them. */
  shown: number
  /** Half the width and height of the area an animal roams around its home spot, in tiles. */
  roam: { x: number; y: number }
  /** Trees around the spot where one appears: deer keep to the woods, rabbits to the meadows. */
  trees: { minimum: number; maximum: number }
  /** World pixels per second. */
  speed: number
  /** Seconds of rest between two moves. */
  rest: [number, number]
  /** Height of the hops while moving, in world pixels. */
  hop: number
  /** Drawn facing right, 16 × 16, feet on the bottom row; the first frame is the animal at rest. */
  frames: AnimationName
  /** Limp, as a hunter carries it home. */
  carried: ArtName
  /** Half the width of the shadow under the feet, in world pixels. */
  shadow: number
}

const LOOKS: Record<AnimalKind, AnimalLook> = {
  Rabbit: {
    shown: 30,
    roam: { x: 4, y: 3 },
    trees: { minimum: 0, maximum: 1 },
    speed: 22,
    rest: [2, 6],
    hop: 2.5,
    frames: 'character-rabbit',
    carried: 'character-rabbit-carried',
    shadow: 4,
  },
  Deer: {
    shown: 12,
    roam: { x: 5, y: 4 },
    trees: { minimum: 2, maximum: 9 },
    speed: 14,
    rest: [4, 10],
    hop: 0.8,
    frames: 'character-deer',
    carried: 'character-deer-carried',
    shadow: 5.5,
  },
  Bird: {
    shown: 24,
    roam: { x: 4, y: 3 },
    trees: { minimum: 1, maximum: 9 },
    speed: 40,
    rest: [1.5, 5],
    hop: 6,
    frames: 'character-bird',
    carried: 'character-bird-carried',
    shadow: 3,
  },
}

/** Animals run from a citizen who comes closer than this, in tiles. */
const FLEE_DISTANCE = 1.5
/** Hunters on duty are noticed from further away. */
const HUNTER_FLEE_DISTANCE = 4
const FLEE_SPEED = 2.5
/** How far a frightened animal runs, in tiles. */
const FLEE_RUN = 3.5
/** A fleeing animal further than this from its home spot, in tiles, runs back toward it. */
const HOME_RANGE = 5
/** Animals keep out of the camp: this many tiles around the campfire. */
const CAMP_CLEARANCE = 10
/** In the last seconds of a chase the prey tires and slows down, so the hunter reaches it in time. */
const TIRING_SECONDS = 20
const MINIMUM_STAMINA = 0.2
/** Where a carried animal rests above a citizen's feet: on the shoulders, in world pixels. */
const CARRY_HEIGHT = 12

const BLOCKING: number[] = [Terrain.Water, Terrain.Reeds, Terrain.Mountain, Terrain.Ravine, Terrain.Thicket, Terrain.Rockfall]

interface Animal {
  kind: AnimalKind
  view: Container
  body: Sprite
  homeX: number
  homeY: number
  x: number
  y: number
  targetX: number
  targetY: number
  moveAt: number
  fleeing: boolean
  phase: number
  /** 1 when rested; a chased animal tires toward the end of the chase. */
  stamina: number
}

export interface WildlifeCitizen {
  id: string
  x: number
  y: number
}

/** Where a hunter runs: the prey, or nothing more to chase once it is caught. */
export type Chase = { caught: false; x: number; y: number } | { caught: true }


/**
 * Game animals living all over the wild land. How many are drawn follows the herds on the server,
 * so the Tsar sees the land empty out without ever seeing a number. Hunters on duty chase one animal
 * and always catch it when their chase time is up.
 */
export class WildlifeLayer {
  private readonly shadows = new Map<AnimalKind, GraphicsContext>()
  private readonly animals: Animal[] = []
  private readonly layer: Container
  private readonly tiles: TileSet
  private terrain: Uint8Array | null = null
  private herds: Herd[] = []
  /** Prey each hunter is after. */
  private readonly chases = new Map<string, Animal>()
  /** Caught animals, carried by their hunter until the cycle ends. */
  private readonly carried = new Map<string, Sprite>()

  /** Whether anyone sees a point of the world, in world pixels; animals out of sight are hidden. */
  private seen: (worldX: number, worldY: number) => boolean = () => true

  constructor(layer: Container, tiles: TileSet) {
    this.layer = layer
    this.tiles = tiles
  }

  setSight(seen: (worldX: number, worldY: number) => boolean) {
    this.seen = seen
  }

  setTerrain(terrain: Uint8Array) {
    this.terrain = terrain
    this.setHerds(this.herds)
  }

  setHerds(herds: Herd[]) {
    this.herds = herds
    if (!this.terrain) return
    const chased = new Set(this.chases.values())
    for (const herd of herds) {
      const look = LOOKS[herd.kind]
      if (!look) continue
      const wanted = herd.count < 1 ? 0 : Math.max(1, Math.round((herd.count / herd.capacity) * look.shown))
      // Animals being chased are the last to go.
      const current = this.animals
        .filter((animal) => animal.kind === herd.kind)
        .sort((a, b) => Number(chased.has(a)) - Number(chased.has(b)))
      for (let index = current.length; index < wanted; index++) this.add(herd.kind, look)
      for (const animal of current.slice(0, Math.max(0, current.length - wanted))) this.remove(animal)
    }
  }

  /**
   * Picks or follows the hunter's prey. `secondsLeft` is the time until the catch; at zero the prey is caught,
   * wherever the hunter is, and the hunter carries it from then on.
   */
  chase(hunterId: string, kind: AnimalKind, x: number, y: number, secondsLeft: number): Chase {
    if (this.carried.has(hunterId)) return { caught: true }

    let prey = this.chases.get(hunterId)
    if (!prey || !this.animals.includes(prey)) {
      // Joining in the middle of the cycle after the catch: the hunter already holds the prey.
      prey = secondsLeft > 0 ? this.pickPrey(kind, x, y) : undefined
      if (!prey) {
        this.carry(hunterId, kind)
        return { caught: true }
      }
      this.chases.set(hunterId, prey)
    }

    if (secondsLeft <= 0) {
      this.chases.delete(hunterId)
      this.animals.splice(this.animals.indexOf(prey), 1)
      prey.view.destroy({ children: true })
      this.carry(hunterId, kind)
      return { caught: true }
    }
    prey.stamina = Math.max(MINIMUM_STAMINA, Math.min(1, secondsLeft / TIRING_SECONDS))
    return { caught: false, x: prey.x, y: prey.y }
  }

  /** The kind of the animal nearest a point of the world (in world pixels), if one is within `reach` pixels. */
  animalAt(worldX: number, worldY: number, reach: number): AnimalKind | null {
    let found: Animal | null = null
    let best = reach
    for (const animal of this.animals) {
      if (!animal.view.visible) continue
      // Animals stand on their feet: the body is drawn above (x, y).
      const distance = Math.hypot(animal.x - worldX, animal.y - 3 - worldY)
      if (distance <= best) {
        best = distance
        found = animal
      }
    }
    return found?.kind ?? null
  }

  /** The hunter's cycle is over: drops the prey they carried or stops the chase. */
  endHunt(hunterId: string) {
    const prey = this.chases.get(hunterId)
    if (prey) prey.stamina = 1
    this.chases.delete(hunterId)
    this.carried.get(hunterId)?.destroy()
    this.carried.delete(hunterId)
  }

  /**
   * `citizens` are positions in world pixels; `hunters` are the ids of citizens hunting right now.
   * Depth order changes only when `sortDepth` is set, because each change re-sorts the whole scene.
   */
  update(seconds: number, elapsed: number, citizens: WildlifeCitizen[], hunters: Set<string>, sortDepth: boolean) {
    for (const animal of this.animals) {
      const look = LOOKS[animal.kind]
      const threat = citizens.find((citizen) => {
        const reach = (hunters.has(citizen.id) ? HUNTER_FLEE_DISTANCE : FLEE_DISTANCE) * TILE
        return Math.hypot(citizen.x - animal.x, citizen.y - animal.y) < reach
      })
      if (threat && !animal.fleeing) {
        animal.fleeing = this.flee(animal, threat)
      } else if (elapsed >= animal.moveAt && !this.moving(animal)) {
        this.wander(animal, look)
        animal.moveAt = elapsed + look.rest[0] + Math.random() * (look.rest[1] - look.rest[0])
      }

      const dx = animal.targetX - animal.x
      const dy = animal.targetY - animal.y
      const distance = Math.hypot(dx, dy)
      if (distance > 0.5) {
        const speed = look.speed * (animal.fleeing ? FLEE_SPEED * animal.stamina : 1)
        const step = Math.min(distance, speed * seconds)
        animal.x += (dx / distance) * step
        animal.y += (dy / distance) * step
        if (Math.abs(dx) > 0.5) animal.view.scale.x = dx < 0 ? -1 : 1
        animal.phase += seconds * (animal.fleeing ? 14 : 9)
      } else if (animal.fleeing) {
        animal.fleeing = false
        animal.phase = 0
        animal.moveAt = elapsed + look.rest[0] + Math.random() * (look.rest[1] - look.rest[0])
      }

      const moving = this.moving(animal)
      const hop = moving ? Math.abs(Math.sin(animal.phase)) * -look.hop : 0
      // One pass through the frames per hop; at rest the animal shows its first frame.
      const frames = this.tiles.animation(look.frames)
      const stride = moving ? Math.floor(((animal.phase % Math.PI) / Math.PI) * frames.length) : 0
      animal.body.texture = frames[Math.min(frames.length - 1, stride)]
      animal.body.y = Math.round(hop)
      animal.view.position.set(Math.round(animal.x), Math.round(animal.y))
      animal.view.visible = this.seen(animal.x, animal.y)
      if (sortDepth) animal.view.zIndex = animal.y
      animal.view.alpha = Math.min(1, animal.view.alpha + seconds)
    }

    for (const [hunterId, view] of this.carried) {
      const hunter = citizens.find((citizen) => citizen.id === hunterId)
      if (!hunter) continue
      view.position.set(Math.round(hunter.x), Math.round(hunter.y - CARRY_HEIGHT))
      if (sortDepth) view.zIndex = hunter.y + 1
    }
  }

  private moving(animal: Animal) {
    return Math.hypot(animal.targetX - animal.x, animal.targetY - animal.y) > 0.5
  }

  /**
   * Runs away from the threat, turning aside when water or the camp is in the way. Far from home it turns
   * back toward it, so a chase circles around the animal's home instead of crossing the whole map.
   */
  private flee(animal: Animal, threat: WildlifeCitizen) {
    const away = Math.atan2(animal.y - threat.y, animal.x - threat.x)
    const farFromHome = Math.hypot(animal.x / TILE - animal.homeX, animal.y / TILE - animal.homeY) > HOME_RANGE
    let best: { x: number; y: number; score: number } | null = null
    for (const turn of [0, 0.5, -0.5, 1, -1, 1.5, -1.5, 2.2, -2.2]) {
      const x = animal.x + Math.cos(away + turn) * FLEE_RUN * TILE
      const y = animal.y + Math.sin(away + turn) * FLEE_RUN * TILE
      if (!this.clearPath(animal.x, animal.y, x, y)) continue
      if (!farFromHome) {
        best = { x, y, score: 0 }
        break
      }
      const score = Math.hypot(x / TILE - animal.homeX, y / TILE - animal.homeY)
      if (!best || score < best.score) best = { x, y, score }
    }
    if (!best) return false
    animal.targetX = best.x
    animal.targetY = best.y
    return true
  }

  private wander(animal: Animal, look: AnimalLook) {
    for (let attempt = 0; attempt < 6; attempt++) {
      const x = (animal.homeX + (Math.random() - 0.5) * 2 * look.roam.x) * TILE
      const y = (animal.homeY + (Math.random() - 0.5) * 2 * look.roam.y) * TILE
      if (this.clearPath(animal.x, animal.y, x, y)) {
        animal.targetX = x
        animal.targetY = y
        return
      }
    }
  }

  /** Nearest animal of the kind no other hunter is after; shares one when every animal is taken. */
  private pickPrey(kind: AnimalKind, x: number, y: number) {
    const taken = new Set(this.chases.values())
    const candidates = this.animals.filter((animal) => animal.kind === kind)
    const free = candidates.filter((animal) => !taken.has(animal))
    const distance = (animal: Animal) => Math.hypot(animal.x - x, animal.y - y)
    return (free.length > 0 ? free : candidates).sort((a, b) => distance(a) - distance(b))[0]
  }

  private carry(hunterId: string, kind: AnimalKind) {
    const view = new Sprite(this.tiles.get(art(LOOKS[kind].carried)))
    view.anchor.set(0.5, 1)
    this.layer.addChild(view)
    this.carried.set(hunterId, view)
  }

  /** Tile in world pixels is dry land outside the camp and the monastery. */
  private walkable(worldX: number, worldY: number) {
    const tileX = Math.floor(worldX / TILE)
    const tileY = Math.floor(worldY / TILE)
    const column = tileX - MAP_LEFT
    const row = tileY - MAP_TOP
    if (!this.terrain || column < 1 || row < 1 || column >= MAP_WIDTH - 1 || row >= MAP_HEIGHT - 1) return false
    if (BLOCKING.includes(this.terrain[row * MAP_WIDTH + column])) return false
    if (Math.hypot(tileX - CAMPFIRE.x, tileY - CAMPFIRE.y) < CAMP_CLEARANCE) return false
    const monastery = MONASTERY.area
    return !(
      tileX >= monastery.x - 1 &&
      tileX <= monastery.x + monastery.width &&
      tileY >= monastery.y - 1 &&
      tileY <= monastery.y + monastery.height
    )
  }

  private clearPath(fromX: number, fromY: number, toX: number, toY: number) {
    const steps = Math.max(1, Math.ceil(Math.hypot(toX - fromX, toY - fromY) / (TILE / 2)))
    for (let step = 1; step <= steps; step++) {
      if (!this.walkable(fromX + ((toX - fromX) * step) / steps, fromY + ((toY - fromY) * step) / steps)) return false
    }
    return true
  }

  private treesAround(tileX: number, tileY: number) {
    let trees = 0
    for (let dy = -1; dy <= 1; dy++) {
      for (let dx = -1; dx <= 1; dx++) {
        const column = tileX + dx - MAP_LEFT
        const row = tileY + dy - MAP_TOP
        if (column >= 0 && row >= 0 && column < MAP_WIDTH && row < MAP_HEIGHT && this.terrain?.[row * MAP_WIDTH + column] === Terrain.Tree) trees++
      }
    }
    return trees
  }

  /** A random spot on dry land, preferring the kind's surroundings. */
  private spawnSpot(look: AnimalLook) {
    let fallback: { x: number; y: number } | null = null
    for (let attempt = 0; attempt < 200; attempt++) {
      const x = (MAP_LEFT + Math.random() * MAP_WIDTH) * TILE
      const y = (MAP_TOP + Math.random() * MAP_HEIGHT) * TILE
      if (!this.walkable(x, y)) continue
      fallback ??= { x, y }
      const trees = this.treesAround(Math.floor(x / TILE), Math.floor(y / TILE))
      if (trees >= look.trees.minimum && trees <= look.trees.maximum) return { x, y }
    }
    return fallback
  }

  private shadow(kind: AnimalKind) {
    let context = this.shadows.get(kind)
    if (!context) {
      context = new GraphicsContext().ellipse(0, 0, LOOKS[kind].shadow, 1.5).fill({ color: 0x1b1206, alpha: 0.25 })
      this.shadows.set(kind, context)
    }
    return context
  }

  private add(kind: AnimalKind, look: AnimalLook) {
    const spot = this.spawnSpot(look)
    if (!spot) return
    // Not cullable: the world is culled only when the camera moves, and animals walk into view on their own.
    const view = new Container()
    const body = new Sprite(this.tiles.animation(look.frames)[0])
    body.anchor.set(0.5, 1)
    view.addChild(new Graphics(this.shadow(kind)), body)
    view.alpha = 0
    view.zIndex = spot.y
    this.layer.addChild(view)
    this.animals.push({
      kind,
      view,
      body,
      homeX: spot.x / TILE,
      homeY: spot.y / TILE,
      x: spot.x,
      y: spot.y,
      targetX: spot.x,
      targetY: spot.y,
      moveAt: Math.random() * look.rest[1],
      fleeing: false,
      phase: 0,
      stamina: 1,
    })
  }

  private remove(animal: Animal) {
    for (const [hunterId, prey] of this.chases) if (prey === animal) this.chases.delete(hunterId)
    this.animals.splice(this.animals.indexOf(animal), 1)
    animal.view.destroy()
  }
}
