import type { CSSProperties } from 'react'
import { Assets, Rectangle, Texture, type TextureSource } from 'pixi.js'

export const TILE = 16

export type SheetName = 'town' | 'dungeon' | 'battle' | 'farm' | 'factory' | 'rogue' | 'extras'

const SHEETS: Record<SheetName, { url: string; columns: number; rows: number }> = {
  town: { url: '/assets/kenney/tiny-town.png', columns: 12, rows: 11 },
  dungeon: { url: '/assets/kenney/tiny-dungeon.png', columns: 12, rows: 11 },
  battle: { url: '/assets/kenney/tiny-battle.png', columns: 18, rows: 11 },
  farm: { url: '/assets/kenney/tiny-farm.png', columns: 12, rows: 11 },
  factory: { url: '/assets/kenney/tiny-factory.png', columns: 12, rows: 11 },
  rogue: { url: '/assets/kenney/roguelike-rpg.png', columns: 57, rows: 31 },
  extras: { url: '/assets/extras.png', columns: 2, rows: 1 },
}

export interface TileRef {
  sheet: SheetName
  index: number
}

export const town = (index: number): TileRef => ({ sheet: 'town', index })
export const dungeon = (index: number): TileRef => ({ sheet: 'dungeon', index })
export const battle = (index: number): TileRef => ({ sheet: 'battle', index })
export const farm = (index: number): TileRef => ({ sheet: 'farm', index })
export const factory = (index: number): TileRef => ({ sheet: 'factory', index })
export const rogue = (index: number): TileRef => ({ sheet: 'rogue', index })
const extras = (index: number): TileRef => ({ sheet: 'extras', index })

/** Tiles of `extras.png`, made for this game in the style of the Kenney sheets. */
export const EXTRAS = { coal: extras(0), campfire: extras(1) }

export class TileSet {
  private readonly sources: Record<SheetName, TextureSource>
  private readonly cache = new Map<string, Texture>()

  private constructor(sources: Record<SheetName, TextureSource>) {
    this.sources = sources
  }

  static async load(): Promise<TileSet> {
    const entries = await Promise.all(
      (Object.keys(SHEETS) as SheetName[]).map(async (name) => {
        const texture = await Assets.load<Texture>(SHEETS[name].url)
        texture.source.scaleMode = 'nearest'
        return [name, texture.source] as const
      }),
    )
    return new TileSet(Object.fromEntries(entries) as Record<SheetName, TextureSource>)
  }

  get(tile: TileRef): Texture {
    const key = `${tile.sheet}:${tile.index}`
    let texture = this.cache.get(key)
    if (!texture) {
      const { columns } = SHEETS[tile.sheet]
      const frame = new Rectangle((tile.index % columns) * TILE, Math.floor(tile.index / columns) * TILE, TILE, TILE)
      texture = new Texture({ source: this.sources[tile.sheet], frame })
      this.cache.set(key, texture)
    }
    return texture
  }
}

/** CSS for showing one tile as an icon in the React interface. */
export function tileStyle(tile: TileRef, scale: number): CSSProperties {
  const { url, columns, rows } = SHEETS[tile.sheet]
  const sheetWidth = columns * TILE
  const sheetHeight = rows * TILE
  return {
    width: TILE * scale,
    height: TILE * scale,
    backgroundImage: `url(${url})`,
    backgroundSize: `${sheetWidth * scale}px ${sheetHeight * scale}px`,
    backgroundPosition: `${-(tile.index % columns) * TILE * scale}px ${-Math.floor(tile.index / columns) * TILE * scale}px`,
    imageRendering: 'pixelated',
    display: 'inline-block',
    flexShrink: 0,
  }
}
