import { useState } from 'react'
import { api, CITIZEN_SUBTITLE_MAXIMUM, type CitizenSummary } from '../api'
import { useI18n } from '../i18n'

/** The Governor writes a short line shown under a citizen's name on the map. */
export function SubtitleDialog({
  slug,
  citizen,
  onSaved,
  onClose,
}: {
  slug: string
  citizen: CitizenSummary
  onSaved: (citizen: CitizenSummary) => void
  onClose: () => void
}) {
  const i18n = useI18n()
  const { t } = i18n
  const [subtitle, setSubtitle] = useState(citizen.subtitle ?? '')
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const save = async (next: string | null) => {
    setPending(true)
    setError(null)
    try {
      onSaved(await api.setSubtitle(slug, citizen.id, next))
      onClose()
    } catch (exception) {
      setError(i18n.error(exception))
    } finally {
      setPending(false)
    }
  }

  return (
    <div className="modal-backdrop" onPointerDown={(event) => event.target === event.currentTarget && onClose()}>
      <div className="modal" role="dialog" aria-label={t('subtitle.title', { name: citizen.name })}>
        <header className="modal-header">
          <h2>{t('subtitle.title', { name: citizen.name })}</h2>
          <button className="close" onClick={onClose} title={`${t('close')} (Esc)`}>
            ✕
          </button>
        </header>
        <form
          className="modal-body"
          onSubmit={(event) => {
            event.preventDefault()
            void save(subtitle.trim() || null)
          }}
        >
          <p className="small">{t('subtitle.hint')}</p>
          <label className="field">
            {t('subtitle.label')}
            <input
              value={subtitle}
              onChange={(event) => setSubtitle(event.target.value)}
              maxLength={CITIZEN_SUBTITLE_MAXIMUM}
              placeholder={t('subtitle.placeholder')}
              autoFocus
            />
          </label>
          <div className="chips">
            <button type="submit" className="primary" disabled={pending}>
              {t('subtitle.save')}
            </button>
            {citizen.subtitle && (
              <button type="button" disabled={pending} onClick={() => void save(null)}>
                {t('subtitle.remove')}
              </button>
            )}
          </div>
          {error && <p className="error">{error}</p>}
        </form>
      </div>
    </div>
  )
}
