import type { Catalog, Citizen, VillageState } from '../api'
import type { FeedEntry } from '../hooks'
import { useI18n } from '../i18n'
import { TABS, tabsFor, type MenuTab } from './menuTabs'
import { NextAction } from './NextAction'
import { ProjectBanner } from './ProjectPanel'
import { ResourceIcon } from './TileIcon'
import { HUD_RESOURCES } from './texts'

/** `governor`: no stock numbers in the top bar. */
export function TopBar({
  village,
  governor,
  night,
  onNight,
}: {
  village: VillageState
  governor: boolean
  night: boolean
  onNight: () => void
}) {
  const i18n = useI18n()
  const { t } = i18n
  return (
    <div className="hud-top">
      <div className="hud-title">
        <span className="brand">{t('brand')}</span>
        <span className="village-name">{village.name}</span>
        {village.isLive && <span className="badge live">{t('live')}</span>}
        {governor && <span className="badge">{t('hud.governor')}</span>}
      </div>
      <ProjectBanner village={village} />
      <div className="hud-resources">
        {!governor && HUD_RESOURCES.map((kind) => {
          const resource = village.resources.find((r) => r.kind === kind)
          return (
            <span className="resource-chip" key={kind} title={i18n.resource(kind)}>
              <ResourceIcon kind={kind} scale={1.5} />
              {i18n.amount(Math.floor(resource?.amount ?? 0))}
            </span>
          )
        })}
        <span className="resource-chip" title={t('hud.moraleTitle')}>
          {t('hud.morale', { value: Math.round(village.morale * 100) })}
        </span>
        <button className="night-toggle" onClick={onNight} title={t('hud.dayNightTitle')} aria-pressed={night}>
          {night ? t('hud.day') : t('hud.night')}
        </button>
      </div>
    </div>
  )
}

export function MiniFeed({ feed }: { feed: FeedEntry[] }) {
  const i18n = useI18n()
  if (feed.length === 0) return null
  return (
    <ul className="mini-feed">
      {feed.slice(0, 4).map((entry) => (
        <li key={entry.key}>
          {entry.kind === 'cycle'
            ? i18n.describeReport(entry.report)
            : i18n.t('feed.built', { project: i18n.project(entry.completion.projectId).name })}
        </li>
      ))}
    </ul>
  )
}

export function BottomBar({
  citizen,
  catalog,
  now,
  phone,
  governor,
  hasResult,
  poll,
  onMenu,
  onInventory,
  onShowResult,
  pending,
  onCancelTask,
}: {
  citizen: Citizen | null | undefined
  catalog: Catalog
  now: number
  phone: boolean
  governor: boolean
  hasResult: boolean
  /** "Vote open" while a hidden vote runs, "Call a vote" for the Governor; null hides the button. */
  poll: { label: string; onClick: () => void } | null
  onMenu: (tab: MenuTab) => void
  onInventory: () => void
  onShowResult: () => void
  pending: boolean
  onCancelTask: () => void
}) {
  const { t } = useI18n()
  return (
    <div className="hud-bottom">
      <div className="hud-status">
        {governor ? (
          <button className="primary" onClick={() => onMenu('build')}>
            {t('hud.build')}
          </button>
        ) : citizen ? (
          <>
            <NextAction citizen={citizen} catalog={catalog} now={now} compact pending={pending} onCancel={onCancelTask} />
            {hasResult && citizen.state === 'AwaitingRenewal' && (
              <button className="primary" onClick={onShowResult}>
                {t('hud.results')}
              </button>
            )}
          </>
        ) : (
          <button className="primary" onClick={() => onMenu('character')}>
            {t('hud.join')}
          </button>
        )}
        {poll && (
          <button className="primary" onClick={poll.onClick}>
            {poll.label}
          </button>
        )}
      </div>
      <div className="hud-buttons">
        {phone ? (
          // Settings stay reachable from the tab row inside the menu, keeping the phone bar short.
          tabsFor(governor).filter((tab) => tab !== 'settings').map((tab) => (
            <button key={tab} onClick={() => onMenu(tab)}>
              {t(TABS[tab])}
            </button>
          ))
        ) : (
          <button onClick={() => onMenu('village')}>
            {t('hud.menu')} <kbd>Esc</kbd>
          </button>
        )}
        <button onClick={onInventory}>
          {phone ? t('hud.storage') : t('hud.inventory')} {!phone && <kbd>I</kbd>}
        </button>
      </div>
    </div>
  )
}
