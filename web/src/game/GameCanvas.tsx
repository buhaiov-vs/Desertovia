import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from 'react'
import type { Catalog, CitizenSummary, TileArea, TilePoint, VillageState } from '../api'
import { useI18n } from '../i18n'
import { VillageWorld } from './VillageWorld'
import type { WalkBlock, WorldTarget } from './worldMap'

export interface GameCanvasHandle {
  say(ownerId: string, text: string): void
  /** Above 1 zooms in, below 1 zooms out, around the screen centre. */
  zoom(factor: number): void
  /** What stops a walker from walking to a spot, in tiles; null when the way is open. */
  wayBlock(walkerId: string, spot: TilePoint): WalkBlock | null
}

export interface Placement {
  /** Top-left tile of the building. */
  onPick: (x: number, y: number) => void
}

interface Props {
  village: VillageState
  citizens: CitizenSummary[]
  highlightId: string | null
  night: boolean
  catalog: Catalog | null
  placement?: Placement | null
  minimap?: boolean
  /** Called with the id of the citizen clicked on the map; null turns clicking off. */
  onCitizenPick?: ((citizenId: string) => void) | null
  /** Called with what a citizen could work at where the map was clicked (null for nothing) and the clicked tile point; null turns clicking off. */
  onWorldPick?: ((target: WorldTarget | null, screen: { x: number; y: number }, point: TilePoint) => void) | null
  /** Which clickable targets show the pointer cursor; null means every target. */
  worldPickable?: ((target: WorldTarget) => boolean) | null
  /** Tiles outlined on the map. */
  selection?: TileArea | null
}

export const GameCanvas = forwardRef<GameCanvasHandle, Props>(function GameCanvas(
  {
    village,
    citizens,
    highlightId,
    night,
    catalog,
    placement = null,
    minimap = true,
    onCitizenPick = null,
    onWorldPick = null,
    worldPickable = null,
    selection = null,
  },
  ref,
) {
  const hostRef = useRef<HTMLDivElement>(null)
  const [world, setWorld] = useState<VillageWorld | null>(null)
  const { t, building, storage } = useI18n()
  const priestName = t('priest.name')
  const governorTitle = t('hud.governor')

  useEffect(() => {
    const host = hostRef.current
    if (!host) return
    let disposed = false
    let created: VillageWorld | null = null
    VillageWorld.create(host, { minimap }).then((instance) => {
      if (disposed) {
        instance.destroy()
        return
      }
      created = instance
      setWorld(instance)
      // Development builds only: lets browser checks draw made-up village states.
      if (import.meta.env.DEV) Object.assign(window, { desertodWorld: instance })
    })
    return () => {
      disposed = true
      created?.destroy()
      setWorld(null)
    }
  }, [minimap])

  useEffect(() => world?.setCatalog(catalog), [world, catalog])
  useEffect(() => world?.setVillage(village), [world, village])
  useEffect(() => world?.setCitizens(citizens, highlightId), [world, citizens, highlightId])
  useEffect(() => world?.setNight(night), [world, night])
  useEffect(() => world?.setPriestName(priestName), [world, priestName])
  useEffect(() => world?.setGovernorTitle(governorTitle), [world, governorTitle])
  useEffect(
    () => world?.setBuildingNames((kind, isStorage) => (isStorage ? storage(kind) : building(kind).name)),
    [world, building, storage],
  )
  useEffect(() => world?.setPlacement(placement?.onPick ?? null), [world, placement])
  useEffect(() => world?.setCitizenPick(onCitizenPick), [world, onCitizenPick])
  useEffect(() => world?.setWorldPick(onWorldPick), [world, onWorldPick])
  useEffect(() => world?.setWorldPickable(worldPickable), [world, worldPickable])
  useEffect(() => world?.setSelection(selection), [world, selection])

  useImperativeHandle(
    ref,
    () => ({
      say: (ownerId, text) => world?.say(ownerId, text),
      zoom: (factor) => world?.zoomBy(factor),
      wayBlock: (walkerId, spot) => world?.wayBlock(walkerId, spot) ?? null,
    }),
    [world],
  )

  return <div ref={hostRef} className="game-canvas" />
})
