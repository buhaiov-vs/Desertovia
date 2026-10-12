import { api, type Me, type VillageState } from '../api'
import { LanguageSwitcher, useI18n } from '../i18n'
import { InviteCard } from './InviteCard'
import { LoginPanel } from './LoginPanel'
import { NotificationToggle } from './NotificationToggle'

interface Props {
  slug: string
  village: VillageState
  me: Me | null
  isOwner: boolean
  inviteCode: string | null
  onToggleLive: () => void
  onNewInviteCode: () => Promise<void>
  /** Reloads the account after signing in or out. */
  onAccountChanged: () => void
}

/** Things that are not part of the game: language, notifications, account and stream controls. */
export function SettingsPanel({ slug, village, me, isOwner, inviteCode, onToggleLive, onNewInviteCode, onAccountChanged }: Props) {
  const i18n = useI18n()
  const { t } = i18n

  return (
    <>
      <section className="card">
        <h3>{t('settings.language')}</h3>
        <LanguageSwitcher />
      </section>

      <NotificationToggle />

      {me ? (
        <section className="card">
          <h3>{t('settings.account')}</h3>
          <div className="row">
            <span>
              {t('home.signedInAs')} <strong>{me.displayName}</strong>
            </span>
            <button
              onClick={async () => {
                await api.logout()
                onAccountChanged()
              }}
            >
              {t('home.signOut')}
            </button>
          </div>
        </section>
      ) : (
        <LoginPanel onLoggedIn={onAccountChanged} />
      )}

      {isOwner && (
        <section className="card">
          <h3>{t('settings.stream')}</h3>
          <div className="row">
            <button onClick={onToggleLive}>{village.isLive ? t('menu.endLive') : t('menu.goLive')}</button>
            <a className="button" href={`/v/${slug}/overlay?lang=${i18n.language}`} target="_blank" rel="noreferrer">
              {t('menu.overlay')}
            </a>
          </div>
        </section>
      )}

      {isOwner && <InviteCard village={village} inviteCode={inviteCode} onNewCode={onNewInviteCode} />}
    </>
  )
}
