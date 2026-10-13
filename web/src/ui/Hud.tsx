import type { ReactNode } from 'react'
import { Link } from 'react-router'
import type { Catalog, Citizen, CitizenSummary, VillageState } from '../api'
import type { FeedEntry } from '../hooks'
import { useI18n } from '../i18n'
import { TABS, tabsFor, type MenuTab } from './menuTabs'
import { NextAction } from './NextAction'
import { ProjectBanner } from './ProjectPanel'
import { ResourceIcon } from './TileIcon'
import { HUD_RESOURCES } from './texts'
import { WeatherChip } from './WeatherChip'

export function TopBar({
  village,
  governor,
  night,
  onNight,
  citizen,
  citizens,
  onForecast,
}: {
  village: VillageState
  governor: boolean
  night: boolean
  onNight: () => void
  citizen: Citizen | null | undefined
  citizens: Map<string, CitizenSummary>
  onForecast: (rain: boolean) => Promise<void>
}) {
  const i18n = useI18n()
  const { t } = i18n
  return (
    <div className="hud-top">
      <div className="hud-title">
        <Link to="/" className="brand" title={t('toHome')}>
          {t('brand')}
        </Link>
        <span className="village-name">{village.name}</span>
        {governor && <span className="badge">{t('hud.governor')}</span>}
      </div>
      <ProjectBanner village={village} />
      <div className="hud-resources">
        {HUD_RESOURCES.map((kind) => {
          const resource = village.resources.find((r) => r.kind === kind)
          return (
            <span className="resource-chip" key={kind} title={i18n.resource(kind)}>
              <ResourceIcon kind={kind} scale={1.5} />
              {i18n.amount(resource?.amount ?? 0)}
            </span>
          )
        })}
        <span className="resource-chip" title={t('hud.moraleTitle')}>
          {t('hud.morale', { value: Math.round(village.morale * 100) })}
        </span>
        <WeatherChip weather={village.weather} citizen={citizen} citizens={citizens} onForecast={onForecast} />
        <button className="night-toggle" onClick={onNight} title={t('hud.dayNightTitle')} aria-pressed={night}>
          {night ? t('hud.day') : t('hud.night')}
        </button>
      </div>
    </div>
  )
}

/** Header of a HUD card that folds it down to this one line. */
export function FoldHead({ collapsed, onToggle, children }: { collapsed: boolean; onToggle: () => void; children: ReactNode }) {
  const { t } = useI18n()
  return (
    <button
      className="hud-card-head"
      onClick={onToggle}
      aria-expanded={!collapsed}
      title={t(collapsed ? 'hud.expand' : 'hud.collapse')}
    >
      {children}
      <span className="hud-card-fold" aria-hidden>
        {collapsed ? '▸' : '▾'}
      </span>
    </button>
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
  drinks,
  onDrink,
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
  /** Vigor drinks in the citizen's hands and in village storage. */
  drinks: number
  onDrink: () => void
}) {
  const { t } = useI18n()
  return (
    <div className="hud-bottom">
      <div className="hud-left">
        {!governor && citizen && (
          <CitizenNeeds citizen={citizen} catalog={catalog} now={now} drinks={drinks} pending={pending} onDrink={onDrink} />
        )}
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

/** Food and sleep of the player's citizen, with the numbers behind them. */
function CitizenNeeds({
  citizen,
  catalog,
  now,
  drinks,
  pending,
  onDrink,
}: {
  citizen: Citizen
  catalog: Catalog
  now: number
  drinks: number
  pending: boolean
  onDrink: () => void
}) {
  const i18n = useI18n()
  const { t } = i18n
  const meals = catalog.rules.cyclesPerMeal
  const hoursSinceFetch = Math.max(0, now - Date.parse(citizen.serverNow)) / 3_600_000
  const energy = Math.min(citizen.maximumEnergy, citizen.energy + citizen.energyPerHour * hoursSinceFetch)
  const drinkReadyAt = citizen.drinkReadyAt ? Date.parse(citizen.drinkReadyAt) : 0
  const cooling = now < drinkReadyAt
  const full = energy >= citizen.maximumEnergy
  const drinkTitle = cooling
    ? t('needs.drinkCooldown', { minutes: Math.ceil((drinkReadyAt - now) / 60_000) })
    : full
      ? t('needs.drinkFull')
      : t('needs.drinkTitle', { energy: catalog.rules.vigorDrinkEnergy })
  const place = citizen.shelterQuality < 0.6 ? t('needs.underTrees') : t('needs.inShelter')
  const percent = Math.round(citizen.regenerationMultiplier * 100)
  return (
    <div className="hud-needs">
      <span
        className={`resource-chip${citizen.isHungry ? ' needs-warning' : ''}`}
        title={`${citizen.isHungry ? `${t('character.hungry')} ` : ''}${t('character.eating', { cycles: meals })}`}
      >
        <ResourceIcon kind="Food" scale={1.5} />
        {citizen.isHungry
          ? t('needs.hungry')
          : t('needs.fed', { done: Math.min(citizen.cyclesSinceMeal, meals), total: meals })}
      </span>
      <span
        className={`resource-chip${citizen.cycleTired ? ' needs-warning' : ''}`}
        title={t('needs.sleepTitle', { percent })}
      >
        <span className="sleep-icon" aria-hidden>
          z<small>z</small>
        </span>
        {t('needs.sleep', {
          place,
          energy: Math.floor(energy),
          maximum: citizen.maximumEnergy,
          perHour: i18n.amount(citizen.energyPerHour),
          percent,
        })}
      </span>
      {drinks > 0 && (
        <button className="drink-button" disabled={pending || cooling || full} onClick={onDrink} title={drinkTitle}>
          <ResourceIcon kind="VigorDrink" scale={1.5} />
          {t('needs.drink', { energy: catalog.rules.vigorDrinkEnergy, count: drinks })}
        </button>
      )}
    </div>
  )
}
