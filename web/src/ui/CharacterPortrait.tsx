import { useEffect, useRef } from 'react'
import type { Appearance } from '../api'
import { CharacterPainter } from '../game/appearance'
import { TILE } from '../game/sheets'

/** The citizen's sprite drawn `scale` times larger with crisp pixels. */
export function CharacterPortrait({ appearance, scale }: { appearance: Appearance; scale: number }) {
  const canvas = useRef<HTMLCanvasElement>(null)

  useEffect(() => {
    let cancelled = false
    CharacterPainter.load().then((painter) => {
      const target = canvas.current
      if (cancelled || !target) return
      const context = target.getContext('2d')!
      context.imageSmoothingEnabled = false
      context.clearRect(0, 0, target.width, target.height)
      context.drawImage(painter.paint(appearance), 0, 0, target.width, target.height)
    })
    return () => {
      cancelled = true
    }
  }, [appearance])

  return <canvas ref={canvas} className="portrait" width={TILE * scale} height={TILE * scale} />
}
