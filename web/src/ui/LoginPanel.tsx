import { useEffect, useRef, useState } from 'react'
import { api, type LoginProviders } from '../api'
import { useI18n } from '../i18n'
import type { UiKey } from '../i18n/en'

function TelegramLoginButton({ botUsername, returnUrl, language }: { botUsername: string; returnUrl: string; language: string }) {
  const container = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const element = container.current
    if (!element) return
    const script = document.createElement('script')
    script.src = 'https://telegram.org/js/telegram-widget.js?22'
    script.async = true
    script.setAttribute('data-telegram-login', botUsername)
    script.setAttribute('data-size', 'large')
    script.setAttribute('data-lang', language)
    script.setAttribute(
      'data-auth-url',
      `${window.location.origin}/auth/telegram/callback?returnUrl=${encodeURIComponent(returnUrl)}`,
    )
    element.appendChild(script)
    return () => {
      element.innerHTML = ''
    }
  }, [botUsername, returnUrl, language])

  return <div ref={container} />
}

export function LoginPanel({ onLoggedIn, title = 'login.title' }: { onLoggedIn: () => void; title?: UiKey }) {
  const i18n = useI18n()
  const { t } = i18n
  const [providers, setProviders] = useState<LoginProviders | null>(null)
  const [developmentName, setDevelopmentName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const returnUrl = window.location.pathname + window.location.search

  useEffect(() => {
    api.providers().then(setProviders, (exception: unknown) => setError(i18n.error(exception)))
  }, [i18n])

  if (!providers) return <section className="card">{error ?? t('loading')}</section>

  const loginUrl = (provider: string) => `/auth/login/${provider}?returnUrl=${encodeURIComponent(returnUrl)}`
  const nothingConfigured =
    !providers.twitch && !providers.google && !providers.telegramBotUsername && !providers.developmentLogin

  return (
    <section className="card login">
      <h3>{t(title)}</h3>
      <div className="login-buttons">
        {providers.twitch && (
          <a className="button twitch" href={loginUrl('twitch')}>
            {t('login.twitch')}
          </a>
        )}
        {providers.google && (
          <a className="button youtube" href={loginUrl('google')}>
            {t('login.google')}
          </a>
        )}
        {providers.telegramBotUsername && (
          <TelegramLoginButton botUsername={providers.telegramBotUsername} returnUrl={returnUrl} language={i18n.language} />
        )}
      </div>

      {providers.developmentLogin && (
        <form
          className="inline-form"
          onSubmit={async (event) => {
            event.preventDefault()
            try {
              await api.developmentLogin(developmentName)
              onLoggedIn()
            } catch (exception) {
              setError(i18n.error(exception))
            }
          }}
        >
          <span className="muted small">{t('login.development')}</span>
          <input
            value={developmentName}
            onChange={(event) => setDevelopmentName(event.target.value)}
            placeholder={t('login.anyName')}
            maxLength={32}
          />
          <button type="submit" disabled={!developmentName.trim()}>
            {t('login.submit')}
          </button>
        </form>
      )}

      {nothingConfigured && <p className="error">{t('login.none')}</p>}
      {error && <p className="error">{error}</p>}
    </section>
  )
}
