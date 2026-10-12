import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useParams } from 'react-router'
import {
  api,
  CARRY_TO_STORAGE,
  isStoryQuest,
  takesSpot,
  type BuildingDefinition,
  type Catalog,
  type Citizen,
  type CycleReport,
  type Me,
  type Task,
  type TilePoint,
  type VillageState,
} from '../api'
import type { JobChoice } from '../ui/SitesPanel'
import { inForestArea, tileBuiltOver, type WorldTarget } from '../game/worldMap'
import { TargetPopup } from '../ui/TargetPopup'
import { flagTitle, notify, useTitleReset } from '../notifications'
import { GameCanvas, type GameCanvasHandle, type Placement } from '../game/GameCanvas'
import { HandsCard } from '../ui/HandsCard'
import { PRIEST_ID } from '../game/VillageWorld'
import { useLiveVillage, useLocalNight, useMediaQuery, usePriest, useServerNow } from '../hooks'
import { GameMenu } from '../ui/GameMenu'
import type { MenuTab } from '../ui/menuTabs'
import { BottomBar, MiniFeed, TopBar } from '../ui/Hud'
import { InventoryPanel } from '../ui/InventoryPanel'
import { useI18n } from '../i18n'
import { TaskDonePanel } from '../ui/TaskDonePanel'
import { Guide } from '../ui/Guide'
import { QuestTracker } from '../ui/QuestPanel'
import { SubtitleDialog } from '../ui/SubtitleDialog'
import { PollPanel, PollSetup } from '../ui/PollPanel'

type Panel = 'none' | 'menu' | 'inventory' | 'done'

const ZOOM_STEP = 1.25
/** Results of a closed vote pop up for players who come within this time. */
const POLL_RESULTS_MS = 10 * 60 * 1000
const seenPollKey = (slug: string) => `desertod.poll-seen.${slug}`

export function GamePage() {
  const { slug = '' } = useParams()
  const i18n = useI18n()
  const [me, setMe] = useState<Me | null | undefined>(undefined)
  const [catalog, setCatalog] = useState<Catalog | null>(null)
  const [ownCitizen, setCitizen] = useState<Citizen | null | undefined>(undefined)
  const [placing, setPlacing] = useState<{ definition: BuildingDefinition } | null>(null)
  /** Citizen whose subtitle the Governor is writing. */
  const [subtitleFor, setSubtitleFor] = useState<string | null>(null)
  /** Place on the map the player clicked to send their citizen there. */
  const [picked, setPicked] = useState<{ target: WorldTarget; screen: { x: number; y: number } } | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [serverOffset, setServerOffset] = useState(0)
  const [panel, setPanel] = useState<Panel>('none')
  const [tab, setTab] = useState<MenuTab>('village')
  const [report, setReport] = useState<CycleReport | null>(null)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)
  /** The Governor is ticking discoveries for a new vote. */
  const [pollSetup, setPollSetup] = useState(false)
  /** Open vote the player hid; the next one pops up again. */
  const [hiddenPoll, setHiddenPoll] = useState<string | null>(null)
  /** Closed vote whose results the player has seen. */
  const [seenPoll, setSeenPoll] = useState(() => localStorage.getItem(seenPollKey(slug)))
  const [pollError, setPollError] = useState<string | null>(null)
  const localNight = useLocalNight()
  const [nightOverride, setNightOverride] = useState<boolean | null>(null)
  const night = nightOverride ?? localNight
  const phone = useMediaQuery('(max-width: 720px)')
  const now = useServerNow(serverOffset)
  const canvas = useRef<GameCanvasHandle>(null)
  const citizenId = useRef<string | null>(null)
  const governorRef = useRef(false)

  const applyCitizen = useCallback((next: Citizen | null) => {
    setCitizen(next)
    if (next) setServerOffset(Date.parse(next.serverNow) - Date.now())
  }, [])

  const loadCitizen = useCallback(async () => applyCitizen(await api.myCitizen(slug)), [slug, applyCitizen])

  const applyAccount = useCallback(
    async (account: Me | null) => {
      setMe(account)
      if (account) await loadCitizen()
      else setCitizen(null)
    },
    [loadCitizen],
  )
  const loadMe = useCallback(async () => applyAccount(await api.me()), [applyAccount])

  useEffect(() => {
    api.me().then(applyAccount)
    api.catalog().then(setCatalog)
  }, [applyAccount])

  const titleRef = useRef('Desertod')
  const catalogRef = useRef<Catalog | null>(null)

  const live = useLiveVillage(slug, {
    onCitizenUpdated: (summary) => {
      if (summary.id === citizenId.current) void loadCitizen()
    },
    onCycleCompleted: (finished) => {
      canvas.current?.say(finished.citizenId, i18n.speechFor(finished, governorRef.current))
      if (finished.buildingCompleted) {
        const building = i18n.building(finished.buildingCompleted).name
        notify(i18n.t('notify.builtTitle', { building }), i18n.t('notify.builtBody'), `built-${finished.siteId ?? building}`)
        flagTitle('★', titleRef.current)
      }
      if (finished.technologyDiscovered && finished.technologyId) {
        const technology = i18n.technology(finished.technologyId).name
        notify(
          i18n.t('notify.discoveredTitle', { technology }),
          i18n.t('notify.discoveredBody'),
          `discovered-${finished.technologyId}`,
        )
        flagTitle('★', titleRef.current)
      }
      if (finished.citizenId === citizenId.current) {
        setReport(finished)
        setError(null)
        setPanel((current) => (current === 'none' ? 'done' : current))
        if (!finished.buildingCompleted && !finished.technologyDiscovered) {
          notify(
            i18n.t('notify.doneTitle', { task: i18n.task(finished.taskId) }),
            i18n.t('notify.doneBody', { seconds: catalogRef.current?.rules.graceSeconds ?? 60 }),
            'cycle-done',
          )
        }
        flagTitle('✓', titleRef.current)
      }
    },
    onProjectCompleted: (completion) => {
      const project = i18n.project(completion.projectId).name
      canvas.current?.say(PRIEST_ID, i18n.t('priest.completed', { project }))
      notify(i18n.t('notify.builtTitle', { building: project }), i18n.t('notify.builtBody'), `project-${completion.projectId}`)
      flagTitle('★', titleRef.current)
    },
    onQuestCompleted: (completion) => {
      const done = i18n.t(isStoryQuest(completion.kind) ? 'quest.storyDone' : 'quest.done', {
        quest: i18n.quest(completion).title,
      })
      const renown = citizenId.current ? completion.renownByCitizen[citizenId.current] : undefined
      setNotice(renown ? `${done} ${i18n.t('quest.renown', { renown: i18n.amount(renown) })}` : done)
      window.setTimeout(() => setNotice(null), 6000)
      notify(done, i18n.t('notify.questBody'), `quest-${completion.questId}`)
      flagTitle('★', titleRef.current)
    },
  })

  const brand = i18n.t('brand')
  const title = live.state ? `${live.state.name} — ${brand}` : brand
  useEffect(() => {
    titleRef.current = title
    catalogRef.current = catalog
  }, [title, catalog])
  useTitleReset(title)

  usePriest(canvas, live.state)

  // The village owner is the Governor: plans and builds, never works, and sees no storage numbers.
  const governor = live.isOwner
  const citizen = governor ? null : ownCitizen
  const placingRef = useRef(placing)
  const subtitleForRef = useRef(subtitleFor)
  const pickedRef = useRef(picked)
  /** Closes the vote screen when it is shown. */
  const closePollRef = useRef<(() => void) | null>(null)
  useEffect(() => {
    governorRef.current = governor
    citizenId.current = citizen?.id ?? null
    placingRef.current = placing
    subtitleForRef.current = subtitleFor
    pickedRef.current = picked
  })

  // Calls everyone to the vote when it opens, and shares the result when it closes.
  const lastPoll = useRef<{ id: string; open: boolean } | null | undefined>(undefined)
  const poll = live.state?.poll ?? null
  useEffect(() => {
    if (!live.state) return
    const previous = lastPoll.current
    lastPoll.current = poll ? { id: poll.id, open: poll.closedAt === null } : null
    if (previous === undefined || !poll) return
    if (poll.closedAt === null && previous?.id !== poll.id) {
      notify(i18n.t('notify.pollTitle'), i18n.t('notify.pollBody'), `poll-${poll.id}`)
      flagTitle('?', titleRef.current)
    } else if (poll.winner && previous?.id === poll.id && previous.open) {
      notify(
        i18n.t('notify.pollClosedTitle', { technology: i18n.technology(poll.winner).name }),
        i18n.t('notify.pollClosedBody'),
        `poll-${poll.id}`,
      )
      flagTitle('★', titleRef.current)
    }
  }, [live.state, poll, i18n])

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && subtitleForRef.current) {
        event.preventDefault()
        setSubtitleFor(null)
        return
      }
      if (event.key === 'Escape' && closePollRef.current) {
        event.preventDefault()
        closePollRef.current()
        return
      }
      const target = event.target as HTMLElement | null
      if (target && ['INPUT', 'TEXTAREA', 'SELECT'].includes(target.tagName)) {
        if (event.key === 'Escape') target.blur()
        return
      }
      if (event.key === 'Escape') {
        event.preventDefault()
        if (placingRef.current) {
          setPlacing(null)
          return
        }
        if (pickedRef.current) {
          setPicked(null)
          return
        }
        setPanel((current) => (current === 'none' ? 'menu' : 'none'))
      } else if (event.code === 'KeyI') {
        event.preventDefault()
        setPanel((current) => (current === 'inventory' ? 'none' : 'inventory'))
      } else if (['Equal', 'NumpadAdd'].includes(event.code)) {
        event.preventDefault()
        canvas.current?.zoom(ZOOM_STEP)
      } else if (['Minus', 'NumpadSubtract'].includes(event.code)) {
        event.preventDefault()
        canvas.current?.zoom(1 / ZOOM_STEP)
      }
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  const run = async (action: () => Promise<Citizen>) => {
    setPending(true)
    setError(null)
    try {
      applyCitizen(await action())
      setPanel('none')
      setPicked(null)
    } catch (exception) {
      setError(i18n.error(exception))
    } finally {
      setPending(false)
    }
  }

  const start = (task: Task, spot: TilePoint | null = null) => run(() => api.startTask(slug, task.id, spot))
  const startJob = (choice: JobChoice) => run(() => api.startJob(slug, choice.plotId, choice.stage, choice.jobId))
  const carry = () => run(() => api.startTask(slug, CARRY_TO_STORAGE))
  const research = () => run(() => api.research(slug))

  const renew = (finished: CycleReport) => {
    if (finished.technologyId) {
      if (finished.technologyDiscovered) openMenu('technologies')
      else void research()
      return
    }
    if (finished.siteId) {
      const site = live.state?.sites.find((s) => s.plotId === finished.siteId)
      const stage = citizen?.siteStage ?? site?.stage ?? 0
      void startJob({ plotId: finished.siteId, stage, jobId: finished.taskId })
      return
    }
    const task = catalog?.tasks.find((t) => t.id === finished.taskId)
    if (task) void start(task, againAt(task))
  }

  /** The same spot again, unless the picked camp tree has fallen; then the nearest tree is chopped. */
  const againAt = (task: Task) => {
    const spot = citizen?.spot
    if (!spot || !takesSpot(task)) return null
    const [x, y] = [Math.floor(spot.x), Math.floor(spot.y)]
    const state = live.state
    const felled =
      task.workplace === 'forest' &&
      inForestArea(x, y) &&
      (!state?.trees.some((tree) => tree.x === x && tree.y === y) || (catalog !== null && state !== null && tileBuiltOver(catalog, state, x, y)))
    return felled ? null : spot
  }

  const openMenu = (next: MenuTab) => {
    setTab(next)
    setPanel('menu')
    setPicked(null)
  }

  const drop = async () => {
    setPending(true)
    setError(null)
    try {
      applyCitizen(await api.dropLoad(slug))
    } catch (exception) {
      setError(i18n.error(exception))
    } finally {
      setPending(false)
    }
  }

  /** Keeps the storage view open, so the player sees the goods move into their hands. */
  const takeFromStorage = async (plotId: string, resource: string) => {
    setPending(true)
    setError(null)
    try {
      applyCitizen(await api.takeFromStorage(slug, plotId, resource))
    } catch (exception) {
      setError(i18n.error(exception))
    } finally {
      setPending(false)
    }
  }

  const cancelTask = async () => {
    setPending(true)
    setError(null)
    try {
      applyCitizen(await api.cancelTask(slug))
    } catch (exception) {
      setNotice(i18n.error(exception))
      window.setTimeout(() => setNotice(null), 5000)
    } finally {
      setPending(false)
    }
  }

  const { setState: setVillageState } = live
  const placement = useMemo<Placement | null>(() => {
    if (!placing) return null
    return {
      onPick: async (x, y) => {
        const building = i18n.building(placing.definition.kind).name
        setPlacing(null)
        try {
          setVillageState(await api.placeBlueprint(slug, placing.definition.kind, x, y))
          setNotice(i18n.t('build.placed', { building }))
        } catch (exception) {
          setNotice(i18n.error(exception))
        }
        window.setTimeout(() => setNotice(null), 5000)
      },
    }
  }, [placing, slug, i18n, setVillageState])

  const pollAction = async (action: () => Promise<VillageState>, after?: () => void) => {
    setPending(true)
    setPollError(null)
    try {
      setVillageState(await action())
      after?.()
    } catch (exception) {
      setPollError(i18n.error(exception))
    } finally {
      setPending(false)
    }
  }

  const pollOpen = poll !== null && poll.closedAt === null
  const pollShown = pollOpen
    ? hiddenPoll !== poll.id
    : poll !== null &&
      poll.winner !== null &&
      seenPoll !== poll.id &&
      poll.closedAt !== null &&
      now - Date.parse(poll.closedAt) < POLL_RESULTS_MS
  const canCallPoll = governor && !pollOpen && (live.state?.technologies.some((state) => state.available) ?? false)

  const showPoll = () => {
    setPanel('none')
    setPollError(null)
    if (pollOpen) setHiddenPoll(null)
    else if (governor) setPollSetup(true)
  }

  const closePoll = () => {
    setPollError(null)
    if (pollSetup) {
      setPollSetup(false)
    } else if (poll && pollOpen) {
      setHiddenPoll(poll.id)
    } else if (poll) {
      localStorage.setItem(seenPollKey(slug), poll.id)
      setSeenPoll(poll.id)
    }
  }
  useEffect(() => {
    closePollRef.current = pollSetup || pollShown ? closePoll : null
  })

  const withdraw = async (plotId: string) => {
    try {
      setVillageState(await api.cancelBlueprint(slug, plotId))
    } catch (exception) {
      setNotice(i18n.error(exception))
      window.setTimeout(() => setNotice(null), 5000)
    }
  }

  const hands = (owner: Citizen) => (
    <HandsCard citizen={owner} village={live.state!} catalog={catalog!} pending={pending} onCarry={carry} onDrop={drop} />
  )

  const citizens = useMemo(() => [...live.citizens.values()], [live.citizens])
  const pickCitizen = useMemo(() => (governor ? (id: string) => setSubtitleFor(id) : null), [governor])
  // Only a citizen can be sent to work; the Governor and visitors just look around.
  const canWork = !governor && !!citizen
  const working = citizen?.state === 'Working'
  const pickWorld = useMemo(
    () =>
      canWork
        ? (target: WorldTarget | null, screen: { x: number; y: number }, point: TilePoint) => {
            setError(null)
            setPicked(target ? { target, screen } : null)
            // Empty ground: "come here". A citizen busy with a task stays at work.
            if (target || working) return
            api.goTo(slug, point).then(applyCitizen, (exception) => {
              setNotice(i18n.error(exception))
              window.setTimeout(() => setNotice(null), 5000)
            })
          }
        : null,
    [canWork, working, slug, applyCitizen, i18n],
  )
  const showPicked = picked !== null && canWork && panel === 'none'
  const subtitleCitizen = subtitleFor ? live.citizens.get(subtitleFor) : undefined

  if (live.error) {
    return (
      <div className="splash">
        <p className="error">{i18n.error(live.error)}</p>
        <Link to="/">{i18n.t('toHome')}</Link>
      </div>
    )
  }

  if (!live.state || !catalog || me === undefined) return <div className="splash">{i18n.t('loading')}</div>

  return (
    <div className="game-root">
      <GameCanvas
        ref={canvas}
        village={live.state}
        citizens={citizens}
        highlightId={citizen?.id ?? null}
        night={night}
        catalog={catalog}
        placement={placement}
        onCitizenPick={pickCitizen}
        onWorldPick={pickWorld}
        selection={showPicked ? picked.target.area : null}
      />

      {showPicked && citizen && (
        <TargetPopup
          key={`${picked.screen.x},${picked.screen.y}`}
          target={picked.target}
          screen={picked.screen}
          phone={phone}
          village={live.state}
          catalog={catalog}
          citizen={citizen}
          now={now}
          pending={pending}
          error={error}
          onStart={start}
          onStartJob={startJob}
          onTake={takeFromStorage}
          onClose={() => setPicked(null)}
        />
      )}

      <div className="hud">
        <TopBar village={live.state} governor={governor} night={night} onNight={() => setNightOverride(!night)} />
        <div className="hud-side">
          <QuestTracker village={live.state} governor={governor} onOpen={() => openMenu('village')} />
          {me !== undefined && (citizen !== undefined || governor) && (
            <Guide
              key={governor ? 'governor' : 'citizen'}
              slug={slug}
              governor={governor}
              inviteCode={live.inviteCode}
              village={live.state}
              citizen={citizen}
              catalog={catalog}
              menuTab={panel === 'menu' ? tab : null}
              onMenu={openMenu}
            />
          )}
        </div>
        {!phone && <MiniFeed feed={live.feed} />}
        <div className="zoom-controls">
          <button onClick={() => canvas.current?.zoom(ZOOM_STEP)} title={`${i18n.t('hud.zoomIn')} (+)`} aria-label={i18n.t('hud.zoomIn')}>
            +
          </button>
          <button onClick={() => canvas.current?.zoom(1 / ZOOM_STEP)} title={`${i18n.t('hud.zoomOut')} (−)`} aria-label={i18n.t('hud.zoomOut')}>
            −
          </button>
        </div>
        {(placing || notice) && (
          <div className="placement-banner">
            {placing ? (
              <>
                {i18n.t('build.placing', { building: i18n.building(placing.definition.kind).name })}
                <button onClick={() => setPlacing(null)}>{i18n.t('build.cancel')}</button>
              </>
            ) : (
              notice
            )}
          </div>
        )}
        <BottomBar
          citizen={citizen}
          catalog={catalog}
          now={now}
          phone={phone}
          governor={governor}
          hasResult={report !== null}
          poll={
            pollOpen && !pollShown
              ? { label: i18n.t('poll.open'), onClick: showPoll }
              : canCallPoll && !pollSetup
                ? { label: i18n.t('poll.call'), onClick: showPoll }
                : null
          }
          onMenu={openMenu}
          onInventory={() => setPanel((current) => (current === 'inventory' ? 'none' : 'inventory'))}
          onShowResult={() => setPanel('done')}
          pending={pending}
          onCancelTask={cancelTask}
        />
      </div>

      {panel === 'menu' && (
        <GameMenu
          slug={slug}
          tab={tab}
          onTab={setTab}
          onClose={() => setPanel('none')}
          village={live.state}
          isOwner={governor}
          inviteCode={live.inviteCode}
          onToggleLive={async () => live.setState(await api.setLive(slug, !live.state!.isLive))}
          onNewInviteCode={async () => live.setInviteCode((await api.newInviteCode(slug)).inviteCode)}
          onPlace={(definition) => {
            setPlacing({ definition })
            setNotice(null)
            setPanel('none')
          }}
          onWithdraw={withdraw}
          catalog={catalog}
          me={me}
          citizen={citizen}
          now={now}
          pending={pending}
          error={error}
          onStart={start}
          onStartJob={startJob}
          onCancelTask={cancelTask}
          onPoll={showPoll}
          onResearch={research}
          onLoggedIn={loadMe}
          onCreated={(created) => {
            applyCitizen(created)
            setTab('tasks')
          }}
          feed={live.feed}
        />
      )}

      {panel === 'inventory' && (
        <InventoryPanel
          village={live.state}
          citizen={citizen}
          catalog={catalog}
          governor={governor}
          now={now}
          pending={pending}
          error={error}
          onCarry={carry}
          onDrop={drop}
          onTake={takeFromStorage}
          onClose={() => setPanel('none')}
        />
      )}

      {governor && pollSetup && !pollOpen ? (
        <PollSetup
          catalog={catalog}
          village={live.state}
          pending={pending}
          error={pollError}
          onStart={(technologyIds) => pollAction(() => api.openPoll(slug, technologyIds), () => setPollSetup(false))}
          onClose={closePoll}
        />
      ) : (
        poll &&
        pollShown && (
          <PollPanel
            poll={poll}
            catalog={catalog}
            citizen={citizen}
            governor={governor}
            pending={pending}
            error={pollError}
            onVote={(technologyId) => pollAction(() => api.vote(slug, technologyId))}
            onCancel={() => pollAction(() => api.cancelPoll(slug))}
            onClose={closePoll}
          />
        )
      )}

      {governor && subtitleCitizen && (
        <SubtitleDialog
          key={subtitleCitizen.id}
          slug={slug}
          citizen={subtitleCitizen}
          onSaved={live.updateCitizen}
          onClose={() => setSubtitleFor(null)}
        />
      )}

      {panel === 'done' && report && citizen && (
        <TaskDonePanel
          hands={hands(citizen)}
          report={report}
          citizen={citizen}
          catalog={catalog}
          now={now}
          pending={pending}
          error={error}
          onRenew={() => renew(report)}
          onChoose={() => openMenu('tasks')}
          onHide={() => setPanel('none')}
        />
      )}
    </div>
  )
}
