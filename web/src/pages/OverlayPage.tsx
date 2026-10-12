import { useEffect, useMemo, useRef, useState } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { api, type Catalog } from '../api'
import { GameCanvas, type GameCanvasHandle } from '../game/GameCanvas'
import { PRIEST_ID } from '../game/VillageWorld'
import { useLiveVillage, useLocalNight, usePriest } from '../hooks'
import { useI18n } from '../i18n'
import { MiniFeed } from '../ui/Hud'
import { ProjectBanner } from '../ui/ProjectPanel'

/**
 * Page meant to be added as a browser source in streaming software.
 * `?lang=ru` picks the language, `?night=1` forces night.
 */
export function OverlayPage() {
  const { slug = '' } = useParams()
  const [search] = useSearchParams()
  const i18n = useI18n()
  const canvas = useRef<GameCanvasHandle>(null)
  const localNight = useLocalNight()
  const night = search.has('night') ? search.get('night') === '1' : localNight

  const [catalog, setCatalog] = useState<Catalog | null>(null)
  useEffect(() => {
    api.catalog().then(setCatalog)
  }, [])

  // The streamer watches the overlay too, so it never voices storage shortages.
  const live = useLiveVillage(slug, {
    onCycleCompleted: (report) => canvas.current?.say(report.citizenId, i18n.speechFor(report, true)),
    onProjectCompleted: (completion) =>
      canvas.current?.say(PRIEST_ID, i18n.t('priest.completed', { project: i18n.project(completion.projectId).name })),
  })
  usePriest(canvas, live.state, 15_000)
  const citizens = useMemo(() => [...live.citizens.values()], [live.citizens])

  if (live.error) return <div className="splash">{i18n.error(live.error)}</div>
  if (!live.state) return <div className="splash">{i18n.t('loading')}</div>

  return (
    <div className="game-root overlay">
      <GameCanvas ref={canvas} village={live.state} citizens={citizens} highlightId={null} night={night} catalog={catalog} minimap={false} />
      <div className="hud">
        <div className="overlay-top">
          <ProjectBanner village={live.state} />
        </div>
        <div className="overlay-join">
          {i18n.t('overlay.join', { address: `${window.location.host}/v/${slug}`, count: citizens.length })}
        </div>
        <MiniFeed feed={live.feed} />
      </div>
    </div>
  )
}
