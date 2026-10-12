import type { Catalog, Citizen } from '../api'
import { useI18n } from '../i18n'
import { CharacterPortrait } from './CharacterPortrait'

export function CharacterSheet({ citizen, catalog }: { citizen: Citizen; catalog: Catalog }) {
  const i18n = useI18n()
  const { t } = i18n
  return (
    <section className="card">
      <h3 className="character-title">
        {citizen.appearance && <CharacterPortrait appearance={citizen.appearance} scale={3} />}
        <span>
          {citizen.name} <span className="muted">— {i18n.role(citizen.titleRoleId)}</span>
        </span>
      </h3>
      <div className="character-summary">
        <div>
          <span className="muted small">{t('character.energy')}</span>
          <div className="bar energy">
            <div style={{ width: `${(citizen.energy / citizen.maximumEnergy) * 100}%` }} />
          </div>
          <span className="small">
            {Math.floor(citizen.energy)} / {citizen.maximumEnergy}
          </span>
        </div>
        <div>
          <span className="muted small">{t('character.renown')}</span>
          <strong>{Math.floor(citizen.renown)}</strong>
        </div>
        <div>
          <span className="muted small">{t('character.streak')}</span>
          <strong>{citizen.streak}</strong>
        </div>
        <div>
          <span className="muted small">{t('character.talent')}</span>
          <strong>
            {citizen.statTotal} / {catalog.rules.talentCap}
          </strong>
        </div>
        <div>
          <span className="muted small">{t('character.sleeps')}</span>
          <strong>{citizen.shelterQuality < 0.6 ? t('character.underTrees') : t('character.inShelter')}</strong>
          <span className="small muted">{t('character.rest', { percent: Math.round(citizen.shelterQuality * 100) })}</span>
        </div>
      </div>
      {citizen.isHungry ? (
        <p className="warning small">{t('character.hungry')}</p>
      ) : (
        <p className="small muted">{t('character.fed')}</p>
      )}
      <p className="small muted">{t('character.eating', { amount: i18n.amount(catalog.rules.foodPerCycle) })}</p>
      <table className="stats">
        <tbody>
          {citizen.stats.map((stat) => (
            <tr key={stat.stat} title={i18n.statHint(stat.stat)}>
              <td>{i18n.stat(stat.stat)}</td>
              <td className="stat-value">{stat.level}</td>
              <td className="stat-progress">
                {stat.experienceToNext > 0 ? (
                  <>
                    <div className="bar">
                      <div style={{ width: `${(stat.experience / stat.experienceToNext) * 100}%` }} />
                    </div>
                    <span className="muted small">
                      {Math.floor(stat.experience)} / {stat.experienceToNext}
                    </span>
                  </>
                ) : (
                  <span className="muted small">{t('character.maximum')}</span>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {citizen.cycleTired && <p className="warning small">{t('character.tired')}</p>}
    </section>
  )
}
