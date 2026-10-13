import { useState } from 'react'
import { RAIN, type Citizen, type CitizenSummary, type Forecast, type Weather } from '../api'
import { useI18n } from '../i18n'

const percent = (multiplier: number) => Math.round(Math.abs(multiplier - 1) * 100)

/**
 * Today's sky in the top bar. Opens the forecasts, what rain does, and for a Synoptic who has just slept
 * the two announcements; the weather then tends to do the opposite.
 */
export function WeatherChip({
  weather,
  citizen,
  citizens,
  onForecast,
}: {
  weather: Weather
  /** The player's citizen; null for the Governor and watchers. */
  citizen: Citizen | null | undefined
  citizens: Map<string, CitizenSummary>
  onForecast: (rain: boolean) => Promise<void>
}) {
  const i18n = useI18n()
  const { t } = i18n
  const [open, setOpen] = useState(false)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const sky = (rain: boolean) => (rain ? t('weather.rain') : t('weather.sun'))
  const nameOf = (forecast: Forecast) => citizens.get(forecast.citizenId)?.name ?? t('weather.someone')
  const minutesLeft = Math.max(0, Math.ceil((Date.parse(weather.dayEndsAt) - Date.now()) / 60_000))
  const changesIn =
    minutesLeft >= 60 ? t('weather.inHours', { hours: Math.floor(minutesLeft / 60), minutes: minutesLeft % 60 }) : t('weather.inMinutes', { minutes: minutesLeft })

  const synoptic = citizen?.forecastReliability != null ? citizen : null
  const rested =
    synoptic !== null &&
    (synoptic.state === 'Idle' || (synoptic.state === 'AwaitingRenewal' && synoptic.graceEndsAt !== null && Date.now() > Date.parse(synoptic.graceEndsAt)))

  const forecast = async (rain: boolean) => {
    setPending(true)
    setError(null)
    try {
      await onForecast(rain)
    } catch (exception) {
      setError(i18n.error(exception))
    } finally {
      setPending(false)
    }
  }

  return (
    <span className="weather">
      <button
        className={`resource-chip weather-chip${weather.raining ? ' raining' : ''}`}
        onClick={() => setOpen(!open)}
        title={t('weather.title')}
        aria-expanded={open}
      >
        <span className="weather-icon" aria-hidden />
        {sky(weather.raining)}
      </button>
      {open && (
        <div className="weather-panel">
          <div className="weather-row">
            <strong>{t('weather.today', { sky: sky(weather.raining) })}</strong>
            <span className="muted small">{t('weather.changes', { time: changesIn })}</span>
          </div>
          {weather.today && (
            <p className="small">
              {t(weather.today.saysRain ? 'weather.promisedRain' : 'weather.promisedSun', { name: nameOf(weather.today) })}{' '}
              {weather.today.saysRain === weather.raining ? t('weather.cameTrue') : t('weather.cameWrong')}
            </p>
          )}
          <p className="small">
            {weather.tomorrow
              ? t(weather.tomorrow.saysRain ? 'weather.promisesRain' : 'weather.promisesSun', { name: nameOf(weather.tomorrow) })
              : t('weather.noForecast')}
          </p>

          <p className="small weather-effects-title">{t('weather.effectsTitle')}</p>
          <ul className="weather-effects small">
            <li className="good">{t('weather.effectFields')}</li>
            <li className="good">{t('weather.effectFishing', { percent: percent(RAIN.fishingYield) })}</li>
            <li className="good">{t('weather.effectIndoor', { percent: percent(RAIN.indoorYield) })}</li>
            <li className="good">{t('weather.effectForest', { times: RAIN.forestRegrowth })}</li>
            <li className="bad">{t('weather.effectEnergy', { percent: percent(RAIN.outdoorEnergyCost) })}</li>
            <li className="bad">{t('weather.effectSleep', { percent: percent(RAIN.underTreesRegeneration) })}</li>
            <li className="bad">{t('weather.effectFire', { percent: percent(RAIN.fireMorale) })}</li>
            <li className="bad">{t('weather.effectMud', { percent: percent(RAIN.mudYield) })}</li>
            <li className="bad">{t('weather.effectHunting', { percent: percent(RAIN.huntingYield) })}</li>
          </ul>

          {synoptic && (
            <div className="weather-synoptic">
              <p className="small">
                {t('weather.synoptic', { percent: Math.round((synoptic.forecastReliability ?? 0) * 100) })}
              </p>
              {weather.tomorrow ? null : rested ? (
                <div className="weather-buttons">
                  <button disabled={pending} onClick={() => forecast(false)}>
                    {t('weather.saySun')}
                  </button>
                  <button disabled={pending} onClick={() => forecast(true)}>
                    {t('weather.sayRain')}
                  </button>
                </div>
              ) : (
                <p className="muted small">{t('weather.sleepFirst')}</p>
              )}
              {error && <p className="error small">{error}</p>}
            </div>
          )}
        </div>
      )}
    </span>
  )
}
