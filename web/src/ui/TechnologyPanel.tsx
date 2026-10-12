import {
  builtKinds,
  ERAS,
  type Catalog,
  type Citizen,
  type TechnologyDefinition,
  type TechnologyState,
  type VillageState,
} from '../api'
import { useI18n } from '../i18n'
import { BUILDING_SIGNS, taskIcon } from '../game/worldMap'
import { TileIcon } from './TileIcon'

/**
 * Discoveries grouped by era. The Governor calls a vote on the next one and plans the buildings the research needs;
 * citizens research the chosen one at its building.
 */
export function TechnologyPanel({
  catalog,
  village,
  citizen,
  governor,
  disabled,
  onPoll,
  onResearch,
}: {
  catalog: Catalog
  village: VillageState
  citizen: Citizen | null | undefined
  governor: boolean
  disabled: boolean
  /** Opens the open vote, or lets the Governor call one. */
  onPoll: () => void
  onResearch: () => void
}) {
  const i18n = useI18n()
  const { t } = i18n
  const built = builtKinds(village, catalog)
  const states = new Map(village.technologies.map((state) => [state.id, state]))
  const current = village.currentResearch
  const pollOpen = village.poll?.closedAt === null

  return (
    <>
      <section className="card">
        <h3>{t('tech.era', { era: i18n.era(village.era) })}</h3>
        <p className="small">{governor ? t('tech.governorHint') : t('tech.intro')}</p>
        <p className="small">
          {current ? t('tech.current', { technology: i18n.technology(current).name }) : t('tech.none')}
        </p>
        {(pollOpen || governor) && (
          <button className="primary" onClick={onPoll}>
            {pollOpen ? t('poll.open') : t('poll.call')}
          </button>
        )}
      </section>

      {ERAS.map((era) => {
        const technologies = catalog.technologies.filter((technology) => technology.era === era)
        if (technologies.length === 0) return null
        return (
          <section key={era} className="card">
            <h3>{i18n.era(era)}</h3>
            <div className="task-grid">
              {technologies.map((technology) => (
                <TechnologyCard
                  key={technology.id}
                  technology={technology}
                  state={states.get(technology.id)}
                  catalog={catalog}
                  citizen={governor ? null : citizen}
                  current={technology.id === current}
                  hasBuilding={built.has(technology.researchedAt)}
                  disabled={disabled}
                  onResearch={onResearch}
                />
              ))}
            </div>
          </section>
        )
      })}
    </>
  )
}

function TechnologyCard({
  technology,
  state,
  catalog,
  citizen,
  current,
  hasBuilding,
  disabled,
  onResearch,
}: {
  technology: TechnologyDefinition
  state: TechnologyState | undefined
  catalog: Catalog
  citizen: Citizen | null | undefined
  current: boolean
  hasBuilding: boolean
  disabled: boolean
  onResearch: () => void
}) {
  const i18n = useI18n()
  const { t } = i18n
  const { name, description } = i18n.technology(technology.id)
  const discovered = state?.discovered ?? false
  const available = state?.available ?? false
  const building = i18n.building(technology.researchedAt).name
  const level = (stat: string) => citizen?.stats.find((s) => s.stat === stat)?.level ?? 0
  const qualified = technology.researcher.every((r) => level(r.stat) >= r.minimum)
  const researching = citizen?.technologyId === technology.id && citizen.state !== 'Idle'
  const unlocks = [
    ...catalog.buildings.filter((b) => b.technology === technology.id).map((b) => i18n.building(b.kind).name),
    ...catalog.roles.filter((r) => r.technology === technology.id).map((r) => i18n.role(r.id)),
    ...catalog.tasks.filter((task) => task.requiredTechnology === technology.id).map((task) => i18n.task(task.id)),
    ...technology.bonuses.map((bonus) =>
      t('tech.bonus', { task: i18n.task(bonus.taskId), multiplier: i18n.amount(bonus.multiplier) }),
    ),
  ]

  return (
    <div className={`task-card ${current ? 'current' : ''}`}>
      <TileIcon tile={BUILDING_SIGNS[technology.researchedAt] ?? taskIcon('research-technology')} scale={2} />
      <span className="task-text">
        <span className="task-name">
          {name}
          {discovered && <span className="muted small"> · {t('tech.discovered')}</span>}
        </span>
        <span className="task-meta">{description}</span>
        <span className="task-meta">{t('tech.at', { building })}</span>
        <span className="task-meta">{t('tech.researchers', { requirements: i18n.requirements(technology.researcher) })}</span>
        {unlocks.length > 0 && <span className="task-meta">{t('tech.unlocks', { items: unlocks.join(', ') })}</span>}
        {!discovered && !available && technology.requires.length > 0 && (
          <span className="task-lock">
            {t('tech.needs', { technologies: technology.requires.map((id) => i18n.technology(id).name).join(', ') })}
          </span>
        )}
        {!discovered && state && state.done > 0 && (
          <>
            <span className="task-meta" title={t('tech.crewHint', { needed: state.crewNeeded })}>
              {t('tech.progress', { done: i18n.amount(state.done), required: i18n.amount(state.required) })} ·{' '}
              {t('tech.hands', { hands: state.hands, needed: state.crewNeeded })}
            </span>
            <div className="bar gold">
              <div style={{ width: `${Math.min(100, (state.done / state.required) * 100)}%` }} />
            </div>
          </>
        )}
        {available && current && !hasBuilding && (
          <span className="task-lock">{t('tech.needsBuilding', { building })}</span>
        )}
        {citizen && available && current && (
          <span className="task-actions">
            <button
              className="small-button primary"
              disabled={disabled || researching || !qualified || !hasBuilding}
              onClick={onResearch}
            >
              {t('tech.research')}
            </button>
          </span>
        )}
        {citizen && available && current && !qualified && (
          <span className="task-lock">{t('tech.skill', { requirements: i18n.requirements(technology.researcher) })}</span>
        )}
      </span>
    </div>
  )
}
