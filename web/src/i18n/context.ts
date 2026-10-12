import { createContext, useContext } from 'react'
import type { Language, Translator } from './translator'

export interface I18nContextValue extends Translator {
  setLanguage: (language: Language) => void
}

export const I18nContext = createContext<I18nContextValue | null>(null)

export function useI18n() {
  const context = useContext(I18nContext)
  if (!context) throw new Error('useI18n must be used inside I18nProvider')
  return context
}
