import { useI18n } from '../i18n'
import { useNotificationSetting } from '../notifications'

/** `compact`: a single button, shown only while notifications are off. */
export function NotificationToggle({ compact = false }: { compact?: boolean }) {
  const { t } = useI18n()
  const { state, enable, disable } = useNotificationSetting()

  if (compact) {
    if (state !== 'off') return null
    return (
      <button className="link-button small" onClick={() => void enable()}>
        🔔 {t('notify.enableShort')}
      </button>
    )
  }

  return (
    <section className="card">
      <h3>{t('notify.title')}</h3>
      <p className="small muted">{t('notify.hint')}</p>
      {state === 'unsupported' && <p className="small warning">{t('notify.unsupported')}</p>}
      {state === 'denied' && <p className="small warning">{t('notify.denied')}</p>}
      {state === 'off' && (
        <button className="primary" onClick={() => void enable()}>
          {t('notify.enable')}
        </button>
      )}
      {state === 'on' && (
        <div className="row">
          <span className="small highlight">✓ {t('notify.on')}</span>
          <button onClick={disable}>{t('notify.disable')}</button>
        </div>
      )}
    </section>
  )
}
