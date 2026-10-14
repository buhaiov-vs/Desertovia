import { useEffect, useId, useLayoutEffect, useRef, useState, type CSSProperties, type ReactNode } from 'react'
import { createPortal } from 'react-dom'

const MARGIN = 8

let closeOpenHint: (() => void) | null = null

/**
 * A game term the player can click or tap to read how it works.
 * One hint is open at a time; a click elsewhere or Escape closes it.
 */
export function Hint({ text, children, className }: { text: ReactNode; children: ReactNode; className?: string }) {
  const [open, setOpen] = useState(false)
  const [position, setPosition] = useState<CSSProperties>({ visibility: 'hidden' })
  const termRef = useRef<HTMLButtonElement>(null)
  const popupRef = useRef<HTMLSpanElement>(null)
  const id = useId()

  useEffect(() => {
    if (!open) return
    const close = () => setOpen(false)
    closeOpenHint?.()
    closeOpenHint = close
    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node
      if (!termRef.current?.contains(target) && !popupRef.current?.contains(target)) close()
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      event.stopPropagation()
      close()
    }
    document.addEventListener('pointerdown', onPointerDown, true)
    window.addEventListener('keydown', onKeyDown, true)
    window.addEventListener('resize', close)
    return () => {
      if (closeOpenHint === close) closeOpenHint = null
      document.removeEventListener('pointerdown', onPointerDown, true)
      window.removeEventListener('keydown', onKeyDown, true)
      window.removeEventListener('resize', close)
    }
  }, [open])

  useLayoutEffect(() => {
    if (!open || !termRef.current || !popupRef.current) return
    const term = termRef.current.getBoundingClientRect()
    const popup = popupRef.current.getBoundingClientRect()
    const above = term.top - popup.height - MARGIN >= 0 || term.bottom + popup.height + MARGIN > window.innerHeight
    const left = Math.min(Math.max(MARGIN, term.left + term.width / 2 - popup.width / 2), window.innerWidth - popup.width - MARGIN)
    setPosition({
      left,
      top: above ? Math.max(MARGIN, term.top - popup.height - 6) : term.bottom + 6,
    })
  }, [open])

  const toggle = () => {
    setPosition({ visibility: 'hidden' })
    setOpen((value) => !value)
  }

  return (
    <>
      <button
        ref={termRef}
        type="button"
        className={`hint-term${open ? ' open' : ''}${className ? ` ${className}` : ''}`}
        aria-expanded={open}
        aria-describedby={open ? id : undefined}
        onClick={(event) => {
          event.stopPropagation()
          toggle()
        }}
      >
        {children}
      </button>
      {open &&
        createPortal(
          <span ref={popupRef} id={id} role="tooltip" className="hint-popup" style={position}>
            {text}
          </span>,
          document.body,
        )}
    </>
  )
}
