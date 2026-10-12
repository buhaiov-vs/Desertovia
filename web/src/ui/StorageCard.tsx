import type { Catalog, Citizen, Storage } from '../api'
import { useI18n } from '../i18n'
import { ResourceIcon } from './TileIcon'

/** Lets the player's citizen take goods out of the building into their hands. */
export interface StorageTake {
  citizen: Citizen
  disabled: boolean
  onTake: (resource: string) => void
}

/** One storage building: what it takes, how many slots are used and what lies inside. */
export function StorageCard({ storage, catalog, take }: { storage: Storage; catalog: Catalog; take?: StorageTake }) {
  const i18n = useI18n()
  const { t } = i18n
  const info = catalog.storages.find((s) => s.kind === storage.kind)
  const limit = catalog.rules.takeLimit
  const held = (resource: string) => take?.citizen.load.find((good) => good.resource === resource)?.amount ?? 0

  return (
    <div className="storage-card">
      {info && (
        <p className="muted small">
          {t('inventory.slots', { used: storage.slots.length, total: info.slots, size: info.slotSize })}
          {' · '}
          {info.accepts.length === 0
            ? t('storage.acceptsAll')
            : t('storage.accepts', { goods: info.accepts.map((kind) => i18n.resource(kind)).join(', ') })}
        </p>
      )}
      {storage.goods.length === 0 && <span className="muted small">{t('inventory.emptyBuilding')}</span>}
      {take ? (
        <ul className="storage-take">
          {storage.goods.map((good) => (
            <li key={good.resource}>
              <ResourceIcon kind={good.resource} scale={1} />
              <span>{i18n.resource(good.resource)}</span>
              <span className="muted">{i18n.amount(good.amount)}</span>
              <button
                disabled={take.disabled || held(good.resource) >= limit}
                title={t('storage.takeTitle', { amount: i18n.amount(limit) })}
                onClick={() => take.onTake(good.resource)}
              >
                {t('storage.take')}
              </button>
            </li>
          ))}
        </ul>
      ) : (
        <div className="storage-goods">
          {storage.goods.map((good) => (
            <span key={good.resource} className="cost" title={i18n.resource(good.resource)}>
              <ResourceIcon kind={good.resource} scale={1} /> {i18n.amount(good.amount)}
            </span>
          ))}
        </div>
      )}
      {take && storage.goods.length > 0 && <p className="small muted">{t('storage.takeHint', { amount: i18n.amount(limit) })}</p>}
    </div>
  )
}
