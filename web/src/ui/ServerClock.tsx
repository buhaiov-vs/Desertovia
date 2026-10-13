import type { ReactNode } from 'react'
import { useServerNow } from '../hooks'

/** Ticks the server clock for its children only, so the page around a countdown does not re-render with it. */
export function ServerClock({ offsetMs, children }: { offsetMs: number; children: (now: number) => ReactNode }) {
  return children(useServerNow(offsetMs))
}
