import { useState } from 'react'
import {
  blockedByLoad,
  builtKinds,
  isStoryQuest,
  projectWants,
  storageBuildingsFor,
  veinEmpty,
  type Catalog,
  type Citizen,
  type Quest,
  type QuestObjective,
  type StoryQuestKind,
  type Task,
  type VillageState,
} from '../api'
import { useI18n } from '../i18n'

type Translator = ReturnType<typeof useI18n>

/** Tasks a citizen can start right from a story quest. */
const STORY_TASKS: Partial<Record<StoryQuestKind, string[]>> = {
  Fire: ['tend-fire'],
  Shelter: ['chop-wood', 'cut-reeds'],
  Stores: ['forage', 'hunt'],
  Monument: ['deliver-stone', 'deliver-wood', 'build'],
  FishingSpot: ['deliver-wood', 'deliver-reeds', 'build', 'fish'],
  Palisade: ['chop-wood', 'deliver-wood', 'build'],
  TsarBridge: ['measure-river', 'survey', 'supervise', 'deliver-wood', 'deliver-stone', 'deliver-reeds', 'build'],
  MonasteryBell: ['survey', 'supervise', 'deliver-stone', 'deliver-wood', 'deliver-gold', 'build'],
  MonksGold: ['dig-gold'],
  GreatGranary: ['survey', 'supervise', 'deliver-wood', 'deliver-stone', 'deliver-reeds', 'build'],
}

function objectiveText(i18n: Translator, quest: Quest, objective: QuestObjective, governor: boolean) {
  return i18n.objective(objective.id, governor, {
    resource: quest.resource ? i18n.resource(quest.resource).toLowerCase() : '',
    current: i18n.amount(Math.floor(objective.current ?? 0)),
    required: i18n.amount(objective.required ?? 0),
  })
}

/** A ready-made chat line; the game itself never passes messages from citizens to the Governor. */
function tellLine(i18n: Translator, quest: Quest, village: VillageState, catalog: Catalog) {
  if (quest.kind === 'Cold') {
    const beds = quest.objectives.find((o) => o.id === 'build-shelter')
    return i18n.t('tell.Cold', { beds: beds?.current ?? 0, citizens: beds?.required ?? 0 })
  }
  if (quest.kind === 'Storage' && quest.resource) {
    const stock = village.resources.find((r) => r.kind === quest.resource)
    const building = storageBuildingsFor(catalog, quest.resource)[0]
    return i18n.t(building ? 'tell.build' : 'tell.more', {
      resource: i18n.resource(quest.resource),
      amount: Math.floor(stock?.amount ?? 0),
      capacity: Math.floor(stock?.capacity ?? 0),
      building: building ? i18n.building(building.kind).name : '',
    })
  }
  return i18n.t('tell.Hunger')
}

export function QuestPanel({
  village,
  catalog,
  governor,
  citizen,
  disabled,
  onStart,
  onPlan,
  onHelpBuild,
}: {
  village: VillageState
  catalog: Catalog
  /** Sees the needs, never the numbers. */
  governor: boolean
  citizen: Citizen | null | undefined
  disabled: boolean
  onStart: (task: Task) => void
  onPlan: () => void
  onHelpBuild: () => void
}) {
  const i18n = useI18n()
  const { t } = i18n
  const [copied, setCopied] = useState<string | null>(null)
  const built = builtKinds(village, catalog)
  const foodTasks = catalog.tasks.filter(
    (task) =>
      task.roleId === 'laborer' &&
      task.effect === 'Resources' &&
      task.outputs.some((output) => output.resource === 'Food') &&
      (task.requiredBuilding === null || built.has(task.requiredBuilding)),
  )

  const tell = async (quest: Quest) => {
    await navigator.clipboard.writeText(tellLine(i18n, quest, village, catalog))
    setCopied(quest.id)
    window.setTimeout(() => setCopied(null), 4000)
  }

  const story = village.quests.filter((quest) => isStoryQuest(quest.kind))
  const needs = village.quests.filter((quest) => !isStoryQuest(quest.kind))

  const level = (stat: string) => citizen?.stats.find((s) => s.stat === stat)?.level ?? 0
  const qualified = (task: Task) =>
    catalog.roles.find((role) => role.id === task.roleId)?.requirements.every((r) => level(r.stat) >= r.minimum) ?? false

  const storyTasks = (quest: Quest) =>
    isStoryQuest(quest.kind)
      ? (STORY_TASKS[quest.kind] ?? [])
          .map((id) => catalog.tasks.find((task) => task.id === id))
          .filter((task): task is Task => task !== undefined && qualified(task) && projectWants(village.project, task))
      : []

  const taskButton = (task: Task) =>
    citizen && (
      <button
        key={task.id}
        className="primary"
        disabled={disabled || blockedByLoad(task, citizen) !== null || veinEmpty(village, task)}
        onClick={() => onStart(task)}
      >
        {i18n.task(task.id)}
      </button>
    )

  const renderQuest = (quest: Quest) => {
    const text = i18n.quest(quest)
    const reward = catalog.questRewards[quest.kind]
    const governorPending = quest.objectives.some((o) => o.audience === 'Governor' && !o.done)
    return (
      <article key={quest.id} className={`quest quest-${quest.kind.toLowerCase()}`}>
        <h4>{text.title}</h4>
        <p className="small">{text.story}</p>
        <ul className="quest-objectives">
          {quest.objectives.map((objective) => (
            <li key={objective.id} className={objective.done ? 'done' : ''}>
              <span className="quest-check">{objective.done ? '✓' : '○'}</span>
              {objectiveText(i18n, quest, objective, governor)}
            </li>
          ))}
        </ul>
        {reward && (
          <p className="small muted">
            {t('quest.reward', { renown: reward.renownPool, morale: i18n.amount(reward.morale * 100) })}
          </p>
        )}
        <div className="row">
          {governor && governorPending && (
            <button className="primary" onClick={onPlan}>
              {t('quest.plan')}
            </button>
          )}
          {quest.kind === 'Hunger' && foodTasks.map(taskButton)}
          {storyTasks(quest).map(taskButton)}
          {citizen && village.sites.length > 0 && <button onClick={onHelpBuild}>{t('quest.helpBuild')}</button>}
          {citizen && governorPending && <button onClick={() => tell(quest)}>{t('quest.tell')}</button>}
        </div>
        {copied === quest.id && <p className="small highlight">{t('quest.copied')}</p>}
      </article>
    )
  }

  return (
    <>
      {story.length > 0 && (
        <section className="card quests quests-story">
          <h3>{t('quest.storyTitle')}</h3>
          {story.map(renderQuest)}
        </section>
      )}
      <section className="card quests">
        <h3>{t('quest.title')}</h3>
        {governor && <p className="small muted">{t('quest.governorHint')}</p>}
        {needs.length === 0 && <p className="muted small">{t('quest.none')}</p>}
        {needs.map(renderQuest)}
      </section>
    </>
  )
}

/** Compact list of open needs on the game screen; opens the full list on click. */
export function QuestTracker({ village, governor, onOpen }: { village: VillageState; governor: boolean; onOpen: () => void }) {
  const i18n = useI18n()
  const { t } = i18n
  if (village.quests.length === 0) return null
  const shown = village.quests.slice(0, 3)
  return (
    <button className="quest-tracker" onClick={onOpen} title={t('quest.title')}>
      <strong>{t('quest.title')}</strong>
      <ul>
        {shown.map((quest) => {
          const next = quest.objectives.find((o) => !o.done && (governor ? o.audience === 'Governor' : true))
          return (
            <li key={quest.id}>
              <span>{i18n.quest(quest).title}</span>
              {next && <span className="quest-next">{objectiveText(i18n, quest, next, governor)}</span>}
            </li>
          )
        })}
      </ul>
      {village.quests.length > shown.length && (
        <span className="small">{t('quest.more', { count: village.quests.length - shown.length })}</span>
      )}
    </button>
  )
}
