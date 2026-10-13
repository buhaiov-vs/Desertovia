import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useCallback, useEffect, useRef, useState, type RefObject } from 'react'
import {
  api,
  loadVillage,
  type CitizenSummary,
  type CycleReport,
  type ProjectCompletion,
  type QuestCompletion,
  type VillageEvents,
  type VillageState,
} from './api'
import type { GameCanvasHandle } from './game/GameCanvas'
import { PRIEST_ID } from './game/VillageWorld'
import { useI18n } from './i18n'

export type FeedEntry =
  | { key: number; kind: 'cycle'; report: CycleReport }
  | { key: number; kind: 'project'; completion: ProjectCompletion }

const FEED_LENGTH = 8

/** Wait after the weather day ends before asking for the new sky, and between retries; covers clock drift. */
const WEATHER_RETRY_MS = 5_000

export interface LiveVillageHandlers {
  onCitizenUpdated?: (citizen: CitizenSummary) => void
  onCycleCompleted?: (report: CycleReport) => void
  onProjectCompleted?: (completion: ProjectCompletion) => void
  onQuestCompleted?: (completion: QuestCompletion) => void
}

/** Loads a village and keeps it live through the SignalR hub. */
export function useLiveVillage(slug: string, handlers: LiveVillageHandlers = {}) {
  const [state, setState] = useState<VillageState | null>(null)
  const [citizens, setCitizens] = useState<Map<string, CitizenSummary>>(new Map())
  const [isOwner, setIsOwner] = useState(false)
  const [inviteCode, setInviteCode] = useState<string | null>(null)
  const [feed, setFeed] = useState<FeedEntry[]>([])
  const [error, setError] = useState<unknown>(null)
  const callbacks = useRef(handlers)
  useEffect(() => {
    callbacks.current = handlers
  })

  useEffect(() => {
    let cancelled = false
    let feedKey = 0
    const push = (entry: { kind: 'cycle'; report: CycleReport } | { kind: 'project'; completion: ProjectCompletion }) =>
      setFeed((previous) => [{ ...entry, key: feedKey++ }, ...previous].slice(0, FEED_LENGTH))

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/village')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('villageUpdated', (next: VillageState) => setState(next))
    connection.on('villageEvents', (events: VillageEvents) => {
      for (const report of events.cycles) {
        push({ kind: 'cycle', report })
        callbacks.current.onCycleCompleted?.(report)
      }
      for (const completion of events.projects) {
        push({ kind: 'project', completion })
        callbacks.current.onProjectCompleted?.(completion)
      }
      for (const completion of events.quests) callbacks.current.onQuestCompleted?.(completion)
      if (events.citizens.length > 0) {
        setCitizens((previous) => {
          const next = new Map(previous)
          for (const citizen of events.citizens) next.set(citizen.id, citizen)
          return next
        })
        for (const citizen of events.citizens) callbacks.current.onCitizenUpdated?.(citizen)
      }
    })
    connection.onreconnected(() => connection.invoke('JoinVillage', slug))

    const load = async () => {
      try {
        const village = await loadVillage(slug)
        if (cancelled) return
        setState(village.state)
        setIsOwner(village.isOwner)
        setInviteCode(village.inviteCode)
        setCitizens(new Map(village.citizens.map((c) => [c.id, c])))
        await connection.start()
        if (cancelled) return
        await connection.invoke('JoinVillage', slug)
      } catch (exception) {
        if (!cancelled) setError(exception)
      }
    }

    void load()
    return () => {
      cancelled = true
      void connection.stop()
    }
  }, [slug])

  // The sky changes when the weather day ends, and the server sends nothing at that moment.
  const dayEndsAt = state?.weather.dayEndsAt
  useEffect(() => {
    if (!dayEndsAt) return
    let cancelled = false
    let id = 0
    const refresh = async () => {
      try {
        const next = (await api.village(slug)).state
        if (cancelled) return
        if (next.weather.dayEndsAt === dayEndsAt) id = window.setTimeout(refresh, WEATHER_RETRY_MS)
        else setState(next)
      } catch {
        if (!cancelled) id = window.setTimeout(refresh, WEATHER_RETRY_MS)
      }
    }
    id = window.setTimeout(refresh, Math.max(0, Date.parse(dayEndsAt) - Date.now()) + WEATHER_RETRY_MS)
    return () => {
      cancelled = true
      window.clearTimeout(id)
    }
  }, [slug, dayEndsAt])

  const updateCitizen = useCallback(
    (citizen: CitizenSummary) => setCitizens((previous) => new Map(previous).set(citizen.id, citizen)),
    [],
  )

  return { state, setState, citizens, updateCitizen, isOwner, inviteCode, setInviteCode, feed, error }
}

/** Current time in milliseconds, adjusted to the server clock, refreshed every `intervalMs`. */
export function useServerNow(serverOffsetMs: number, intervalMs = 250) {
  const [localNow, setLocalNow] = useState(() => Date.now())

  useEffect(() => {
    const id = window.setInterval(() => setLocalNow(Date.now()), intervalMs)
    return () => window.clearInterval(id)
  }, [intervalMs])

  return localNow + serverOffsetMs
}

/** The elder comments on village life every `intervalMs`, now and then on the work in progress. */
export function usePriest(canvas: RefObject<GameCanvasHandle | null>, village: VillageState | null, intervalMs = 22_000) {
  const i18n = useI18n()
  const latest = useRef({ village, i18n })
  useEffect(() => {
    latest.current = { village, i18n }
  })

  useEffect(() => {
    let index = 0
    const speak = () => {
      const { village: current, i18n: translator } = latest.current
      const lines = translator.priestLines
      const work = current ? currentWork(current, translator) : null
      const line =
        index % 3 === 1 && work ? translator.t('priest.progress', work) : lines[index % lines.length]
      canvas.current?.say(PRIEST_ID, line)
      index++
    }
    const first = window.setTimeout(speak, 3000)
    const id = window.setInterval(speak, intervalMs)
    return () => {
      window.clearTimeout(first)
      window.clearInterval(id)
    }
  }, [canvas, intervalMs])
}

function currentWork(village: VillageState, i18n: ReturnType<typeof useI18n>) {
  if (village.project) {
    return { project: i18n.project(village.project.definitionId).name, percent: Math.floor(village.project.progress * 100) }
  }
  const site = village.sites[0]
  return site ? { project: i18n.building(site.kind).name, percent: Math.floor(site.progress * 100) } : null
}

/** True between 21:00 and 06:00 local time, re-checked every minute. */
export function useLocalNight() {
  const check = () => {
    const hour = new Date().getHours()
    return hour >= 21 || hour < 6
  }
  const [night, setNight] = useState(check)
  useEffect(() => {
    const id = window.setInterval(() => setNight(check()), 60_000)
    return () => window.clearInterval(id)
  }, [])
  return night
}

/** Mirrors a CSS media query. */
export function useMediaQuery(query: string) {
  const [matches, setMatches] = useState(() => window.matchMedia(query).matches)
  useEffect(() => {
    const media = window.matchMedia(query)
    const listener = () => setMatches(media.matches)
    media.addEventListener('change', listener)
    return () => media.removeEventListener('change', listener)
  }, [query])
  return matches
}

/** A folded HUD card stays folded in this browser; until the player chooses, `initial` decides. */
export function useCollapsed(id: string, initial: boolean) {
  const key = `desertod.collapsed.${id}`
  const [saved, setSaved] = useState(() => window.localStorage.getItem(key))
  const collapsed = saved === null ? initial : saved === '1'
  const toggle = () => {
    const next = collapsed ? '0' : '1'
    window.localStorage.setItem(key, next)
    setSaved(next)
  }
  return [collapsed, toggle] as const
}
